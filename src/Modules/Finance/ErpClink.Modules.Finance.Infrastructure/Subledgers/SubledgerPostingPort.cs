using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Finance.Application.Integration;
using ErpClink.Modules.Finance.Application.Subledgers;
using ErpClink.Modules.Finance.Domain.Subledgers;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Finance.Infrastructure.Subledgers;

public sealed class SubledgerPostingPort : ISubledgerPostingPort
{
    private readonly FinanceDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly IBusinessClock _clock;
    private readonly ICurrentUser _user;
    private readonly IValidator<SubledgerPostRequest> _validator;
    private readonly IAccountingPostingPort _accounting;
    private readonly ILogger<SubledgerPostingPort> _logger;

    public SubledgerPostingPort(
        FinanceDbContext db,
        IOrganizationContext org,
        IBusinessClock clock,
        ICurrentUser user,
        IValidator<SubledgerPostRequest> validator,
        IAccountingPostingPort accounting,
        ILogger<SubledgerPostingPort> logger)
    {
        _db = db;
        _org = org;
        _clock = clock;
        _user = user;
        _validator = validator;
        _accounting = accounting;
        _logger = logger;
    }

    public Task<SubledgerPostResult> PostArAsync(SubledgerPostRequest request, CancellationToken cancellationToken = default) =>
        PostAsync(request, SubledgerType.Ar, cancellationToken);

    public Task<SubledgerPostResult> PostApAsync(SubledgerPostRequest request, CancellationToken cancellationToken = default) =>
        PostAsync(request, SubledgerType.Ap, cancellationToken);

    private async Task<SubledgerPostResult> PostAsync(
        SubledgerPostRequest request,
        SubledgerType type,
        CancellationToken cancellationToken)
    {
        var correlationId = request.CorrelationId is { } c && c != Guid.Empty ? c : Guid.NewGuid();
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return SubledgerPostResult.Failed(correlationId, "finance.subledger.validation_failed",
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        if (request.OrganizationId != _org.OrganizationId)
            return SubledgerPostResult.Failed(correlationId, "finance.subledger.organization_mismatch",
                "Request OrganizationId must match the current organization context.");

        if (!Enum.TryParse<SubledgerDirection>(request.Direction, true, out var direction))
            return SubledgerPostResult.Failed(correlationId, "finance.subledger.invalid_direction", "Direction must be Debit or Credit.");

        var existing = await FindByNaturalKeyAsync(request, cancellationToken);
        if (existing is not null)
            return SubledgerPostResult.AlreadyProcessed(existing.Id, existing.CorrelationId, existing.Status.ToString());

        var entity = SubledgerTransaction.Post(
            request.OrganizationId,
            request.BranchId ?? _org.BranchId,
            type,
            request.PartyId,
            request.SourceModule,
            request.SourceType,
            request.SourceId,
            request.EventType,
            request.TransactionDate,
            request.Amount,
            request.CurrencyCode,
            direction,
            request.Description,
            request.PartyDisplayNameSnapshot,
            request.DueDate,
            correlationId,
            request.IdempotencyKey,
            _user.UserId,
            _clock.UtcNow);

        _db.SubledgerTransactions.Add(entity);
        await ApplyBalanceAsync(entity, cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _db.ChangeTracker.Clear();
            var winner = await FindByNaturalKeyAsync(request, cancellationToken)
                ?? await _db.SubledgerTransactions.FirstOrDefaultAsync(
                    x => x.OrganizationId == request.OrganizationId && x.IdempotencyKey == request.IdempotencyKey,
                    cancellationToken);
            if (winner is null) throw;
            return SubledgerPostResult.AlreadyProcessed(winner.Id, winner.CorrelationId, winner.Status.ToString());
        }

        await RegisterAccountingIntentAsync(request, entity, type, cancellationToken);

        _logger.LogInformation(
            "Subledger {Type} posted {TransactionId} Party={PartyId} Amount={Amount} {Direction} CorrelationId={CorrelationId}",
            type, entity.Id, entity.PartyId, entity.Amount, entity.Direction, entity.CorrelationId);

        return SubledgerPostResult.Accepted(entity.Id, correlationId, entity.Status.ToString());
    }

    private async Task ApplyBalanceAsync(SubledgerTransaction entity, CancellationToken cancellationToken)
    {
        var balance = await _db.SubledgerPartyBalances.SingleOrDefaultAsync(
            x => x.OrganizationId == entity.OrganizationId
                 && x.SubledgerType == entity.SubledgerType
                 && x.PartyId == entity.PartyId,
            cancellationToken);

        if (balance is null)
        {
            balance = SubledgerPartyBalance.Create(
                entity.OrganizationId, entity.SubledgerType, entity.PartyId, entity.CurrencyCode, _clock.UtcNow);
            _db.SubledgerPartyBalances.Add(balance);
        }

        balance.ApplyImpact(entity.SignedBalanceImpact, _clock.UtcNow);
    }

    private async Task RegisterAccountingIntentAsync(
        SubledgerPostRequest request,
        SubledgerTransaction entity,
        SubledgerType type,
        CancellationToken cancellationToken)
    {
        try
        {
            await _accounting.RequestPostingAsync(
                new AccountingPostingRequest(
                    OrganizationId: entity.OrganizationId,
                    BranchId: entity.BranchId,
                    SourceModule: AccountingSourceModules.Finance,
                    SourceType: type == SubledgerType.Ar ? "ArSubledger" : "ApSubledger",
                    SourceId: entity.Id.ToString("N"),
                    EventType: "SubledgerPosted",
                    OccurredAtUtc: entity.PostedAtUtc,
                    CorrelationId: entity.CorrelationId,
                    IdempotencyKey: $"subledger:{entity.Id:N}",
                    Description: $"{type} subledger {entity.EventType} {entity.SourceId}",
                    CurrencyCode: entity.CurrencyCode),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Subledger remains source of truth; accounting foundation is best-effort until Outbox (STEP 24).
            _logger.LogWarning(ex,
                "Accounting integration registration failed for subledger {TransactionId}; subledger post succeeded.",
                entity.Id);
        }
    }

    private Task<SubledgerTransaction?> FindByNaturalKeyAsync(SubledgerPostRequest request, CancellationToken cancellationToken) =>
        _db.SubledgerTransactions.FirstOrDefaultAsync(
            x => x.OrganizationId == request.OrganizationId
                 && x.SourceModule == request.SourceModule
                 && x.SourceType == request.SourceType
                 && x.SourceId == request.SourceId
                 && x.EventType == request.EventType
                 && x.IdempotencyKey == request.IdempotencyKey,
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
