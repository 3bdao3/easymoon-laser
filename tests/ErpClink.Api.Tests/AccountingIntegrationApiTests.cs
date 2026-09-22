using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Finance.Application.Integration;
using ErpClink.Modules.Finance.Domain.Integration;
using ErpClink.Modules.Finance.Infrastructure.Events;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class AccountingIntegrationApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherOrg = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public AccountingIntegrationApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task First_request_accepted_duplicate_already_processed()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var port = scope.ServiceProvider.GetRequiredService<IAccountingPostingPort>();
        var collector = scope.ServiceProvider.GetRequiredService<FinanceIntegrationEventCollector>();
        collector.Clear();

        var key = $"idem-{Guid.NewGuid():N}";
        var request = BuildRequest(key, sourceId: "INV-100", eventType: "InvoiceIssued");

        var first = await port.RequestPostingAsync(request);
        first.Outcome.Should().Be(AccountingPostingOutcome.Accepted);
        first.Status.Should().Be(nameof(AccountingIntegrationStatus.Pending));
        first.IntegrationRequestId.Should().NotBeNull();

        var second = await port.RequestPostingAsync(request);
        second.Outcome.Should().Be(AccountingPostingOutcome.AlreadyProcessed);
        second.IntegrationRequestId.Should().Be(first.IntegrationRequestId);

        collector.Events.OfType<AccountingTransactionRequested>().Should().ContainSingle(e => e.IdempotencyKey == key);

        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        var journals = await db.JournalEntries.CountAsync();
        // Foundation must not invent journals from integration acceptance
        _ = journals;
        (await db.AccountingIntegrationRequests.CountAsync(x => x.IdempotencyKey == key)).Should().Be(1);
    }

    [Fact]
    public async Task Different_source_or_event_accepted()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var port = scope.ServiceProvider.GetRequiredService<IAccountingPostingPort>();

        var a = await port.RequestPostingAsync(BuildRequest($"k-{Guid.NewGuid():N}", "INV-A", "InvoiceIssued"));
        var b = await port.RequestPostingAsync(BuildRequest($"k-{Guid.NewGuid():N}", "INV-B", "InvoiceIssued"));
        var c = await port.RequestPostingAsync(BuildRequest($"k-{Guid.NewGuid():N}", "INV-A", "InvoiceVoided"));

        a.Outcome.Should().Be(AccountingPostingOutcome.Accepted);
        b.Outcome.Should().Be(AccountingPostingOutcome.Accepted);
        c.Outcome.Should().Be(AccountingPostingOutcome.Accepted);
        new[] { a.IntegrationRequestId, b.IntegrationRequestId, c.IntegrationRequestId }.Distinct().Should().HaveCount(3);
    }

    [Fact]
    public async Task Organization_isolation_on_unique_constraint()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IBusinessClock>();
        var now = clock.UtcNow;
        var key = $"shared-key-{Guid.NewGuid():N}";

        var orgA = AccountingIntegrationRequest.CreatePending(
            Org, Branch, "Billing", "Invoice", "INV-100", "InvoiceIssued", key, Guid.NewGuid(), null, null, now, now);
        var orgB = AccountingIntegrationRequest.CreatePending(
            OtherOrg, Branch, "Billing", "Invoice", "INV-100", "InvoiceIssued", key, Guid.NewGuid(), null, null, now, now);

        db.AccountingIntegrationRequests.Add(orgA);
        db.AccountingIntegrationRequests.Add(orgB);
        await db.SaveChangesAsync();

        (await db.AccountingIntegrationRequests.CountAsync(x => x.IdempotencyKey == key)).Should().Be(2);
    }

    [Fact]
    public async Task Failed_request_stays_failed_until_retry_then_pending()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var port = scope.ServiceProvider.GetRequiredService<IAccountingPostingPort>();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var key = $"fail-{Guid.NewGuid():N}";
        var accepted = await port.RequestPostingAsync(BuildRequest(key, "GR-1", "GoodsReceiptPosted"));
        accepted.Outcome.Should().Be(AccountingPostingOutcome.Accepted);

        var failed = await port.MarkFailedAsync(Org, key, "finance.accounting_integration.probe_failed", "Simulated failure");
        failed.Outcome.Should().Be(AccountingPostingOutcome.Failed);
        failed.Status.Should().Be(nameof(AccountingIntegrationStatus.Failed));

        var row = await db.AccountingIntegrationRequests.SingleAsync(x => x.Id == accepted.IntegrationRequestId);
        row.Status.Should().Be(AccountingIntegrationStatus.Failed);
        row.ErrorCode.Should().Be("finance.accounting_integration.probe_failed");

        var retry = await port.RequestPostingAsync(BuildRequest(key, "GR-1", "GoodsReceiptPosted"));
        retry.Outcome.Should().Be(AccountingPostingOutcome.Accepted);
        retry.Status.Should().Be(nameof(AccountingIntegrationStatus.Pending));

        await db.Entry(row).ReloadAsync();
        row.Status.Should().Be(AccountingIntegrationStatus.Pending);
        row.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task Concurrent_duplicate_requests_accept_exactly_one()
    {
        var key = $"concurrent-{Guid.NewGuid():N}";
        var request = BuildRequest(key, "PO-789", "PurchaseOrderApproved");

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var port = scope.ServiceProvider.GetRequiredService<IAccountingPostingPort>();
            return await port.RequestPostingAsync(request);
        });

        var results = await Task.WhenAll(tasks);
        results.Count(r => r.Outcome == AccountingPostingOutcome.Accepted).Should().Be(1);
        results.Count(r => r.Outcome == AccountingPostingOutcome.AlreadyProcessed).Should().Be(19);
        results.Select(r => r.IntegrationRequestId).Distinct().Should().ContainSingle();

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<FinanceDbContext>();
        (await db.AccountingIntegrationRequests.CountAsync(x => x.IdempotencyKey == key)).Should().Be(1);
        (await db.JournalEntries.CountAsync(x => x.Description != null && x.Description.Contains(key))).Should().Be(0);
    }

    [Fact]
    public async Task Validation_failure_does_not_persist()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var port = scope.ServiceProvider.GetRequiredService<IAccountingPostingPort>();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        var before = await db.AccountingIntegrationRequests.CountAsync();

        var result = await port.RequestPostingAsync(BuildRequest("", "X", "Y") with { IdempotencyKey = "" });
        result.Outcome.Should().Be(AccountingPostingOutcome.Failed);
        result.ErrorCode.Should().Be("finance.accounting_integration.validation_failed");

        (await db.AccountingIntegrationRequests.CountAsync()).Should().Be(before);
    }

    private static AccountingPostingRequest BuildRequest(string idempotencyKey, string sourceId, string eventType) =>
        new(
            OrganizationId: Org,
            BranchId: Branch,
            SourceModule: AccountingSourceModules.Test,
            SourceType: "ManualProbe",
            SourceId: sourceId,
            EventType: eventType,
            OccurredAtUtc: DateTime.UtcNow,
            CorrelationId: Guid.NewGuid(),
            IdempotencyKey: idempotencyKey,
            Description: "STEP 18 foundation probe — no journal mapping");
}
