using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.IntegrationEvents;
using ErpClink.Modules.Finance.Application.Integration;
using ErpClink.Modules.Finance.Domain.Integration;
using ErpClink.Modules.Finance.Infrastructure.Events;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Finance.Infrastructure.Integration;

/// <summary>
/// Registers accounting posting intents with DB-backed idempotency.
/// Does not create journal entries or invent account mappings.
/// </summary>
public sealed class AccountingPostingPort : IAccountingPostingPort
{
    private readonly FinanceDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly IBusinessClock _clock;
    private readonly IValidator<AccountingPostingRequest> _validator;
    private readonly ILogger<AccountingPostingPort> _logger;
    private readonly FinanceIntegrationEventCollector _integrationEvents;

    public AccountingPostingPort(
        FinanceDbContext db,
        IOrganizationContext org,
        IBusinessClock clock,
        IValidator<AccountingPostingRequest> validator,
        ILogger<AccountingPostingPort> logger,
        FinanceIntegrationEventCollector integrationEvents)
    {
        _db = db;
        _org = org;
        _clock = clock;
        _validator = validator;
        _logger = logger;
        _integrationEvents = integrationEvents;
    }

    public async Task<AccountingPostingResult> RequestPostingAsync(
        AccountingPostingRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = request.CorrelationId is { } c && c != Guid.Empty ? c : Guid.NewGuid();

        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage));
            return AccountingPostingResult.Failed(correlationId, "finance.accounting_integration.validation_failed", message);
        }

        if (request.OrganizationId != _org.OrganizationId)
        {
            return AccountingPostingResult.Failed(
                correlationId,
                "finance.accounting_integration.organization_mismatch",
                "Request OrganizationId must match the current organization context.");
        }

        var existing = await FindByNaturalKeyAsync(request, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status == AccountingIntegrationStatus.Failed)
            {
                existing.ResetForRetry(correlationId, _clock.UtcNow);
                await _db.SaveChangesAsync(cancellationToken);
                EmitAccepted(request, existing, correlationId);
                return AccountingPostingResult.Accepted(existing.Id, correlationId, existing.Status.ToString());
            }

            return AccountingPostingResult.AlreadyProcessed(existing.Id, existing.CorrelationId, existing.Status.ToString());
        }

        var entity = AccountingIntegrationRequest.CreatePending(
            organizationId: request.OrganizationId,
            branchId: request.BranchId ?? _org.BranchId,
            sourceModule: request.SourceModule,
            sourceType: request.SourceType,
            sourceId: request.SourceId,
            eventType: request.EventType,
            idempotencyKey: request.IdempotencyKey,
            correlationId: correlationId,
            description: request.Description,
            currencyCode: request.CurrencyCode,
            occurredAtUtc: request.OccurredAtUtc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.OccurredAtUtc, DateTimeKind.Utc)
                : request.OccurredAtUtc.ToUniversalTime(),
            utcNow: _clock.UtcNow);

        _db.AccountingIntegrationRequests.Add(entity);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _db.Entry(entity).State = EntityState.Detached;
            var winner = await FindByNaturalKeyAsync(request, cancellationToken)
                ?? await FindByIdempotencyAsync(request.OrganizationId, request.IdempotencyKey, cancellationToken);

            if (winner is null)
                throw;

            if (winner.Status == AccountingIntegrationStatus.Failed)
            {
                winner.ResetForRetry(correlationId, _clock.UtcNow);
                await _db.SaveChangesAsync(cancellationToken);
                EmitAccepted(request, winner, correlationId);
                return AccountingPostingResult.Accepted(winner.Id, correlationId, winner.Status.ToString());
            }

            return AccountingPostingResult.AlreadyProcessed(winner.Id, winner.CorrelationId, winner.Status.ToString());
        }

        EmitAccepted(request, entity, correlationId);
        _logger.LogInformation(
            "Accounting integration request accepted {IntegrationRequestId} Org={OrganizationId} Source={SourceModule}/{SourceType}/{SourceId} Event={EventType} CorrelationId={CorrelationId}",
            entity.Id, entity.OrganizationId, entity.SourceModule, entity.SourceType, entity.SourceId, entity.EventType, entity.CorrelationId);

        return AccountingPostingResult.Accepted(entity.Id, correlationId, entity.Status.ToString());
    }

    public async Task<AccountingPostingResult> MarkFailedAsync(
        Guid organizationId,
        string idempotencyKey,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey))
            return AccountingPostingResult.Failed(Guid.Empty, "finance.accounting_integration.validation_failed", "OrganizationId and IdempotencyKey are required.");

        var entity = await FindByIdempotencyAsync(organizationId, idempotencyKey, cancellationToken);
        if (entity is null)
            return AccountingPostingResult.Failed(Guid.NewGuid(), "finance.accounting_integration.not_found", "Integration request not found.");

        if (entity.Status == AccountingIntegrationStatus.Succeeded)
            return AccountingPostingResult.Failed(
                entity.CorrelationId,
                "finance.accounting_integration.already_succeeded",
                "Succeeded integration requests cannot be marked failed.",
                entity.Id,
                entity.Status.ToString());

        entity.MarkFailed(errorCode, errorMessage, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Accounting integration request failed {IntegrationRequestId} Code={ErrorCode} CorrelationId={CorrelationId}",
            entity.Id, errorCode, entity.CorrelationId);

        return AccountingPostingResult.Failed(entity.CorrelationId, errorCode, errorMessage, entity.Id, entity.Status.ToString());
    }

    private void EmitAccepted(AccountingPostingRequest request, AccountingIntegrationRequest entity, Guid correlationId)
    {
        IIntegrationEvent evt = AccountingTransactionRequested.From(request, entity.Id, correlationId);
        _integrationEvents.Add(evt);
        _logger.LogInformation(
            "AccountingTransactionRequested EventId={EventId} IntegrationRequestId={IntegrationRequestId} CorrelationId={CorrelationId}",
            ((AccountingTransactionRequested)evt).EventId, entity.Id, correlationId);
    }

    private Task<AccountingIntegrationRequest?> FindByNaturalKeyAsync(
        AccountingPostingRequest request,
        CancellationToken cancellationToken) =>
        _db.AccountingIntegrationRequests.FirstOrDefaultAsync(
            x => x.OrganizationId == request.OrganizationId
                 && x.SourceModule == request.SourceModule
                 && x.SourceType == request.SourceType
                 && x.SourceId == request.SourceId
                 && x.EventType == request.EventType
                 && x.IdempotencyKey == request.IdempotencyKey,
            cancellationToken);

    private Task<AccountingIntegrationRequest?> FindByIdempotencyAsync(
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        _db.AccountingIntegrationRequests.FirstOrDefaultAsync(
            x => x.OrganizationId == organizationId && x.IdempotencyKey == idempotencyKey,
            cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sql)
            return sql.Number is 2627 or 2601;

        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
