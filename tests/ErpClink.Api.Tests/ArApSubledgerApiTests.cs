using ErpClink.Modules.Finance.Application.Subledgers;
using ErpClink.Modules.Finance.Domain.Subledgers;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class ArApSubledgerApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ArApSubledgerApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Ar_post_idempotent_and_balance()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var port = scope.ServiceProvider.GetRequiredService<ISubledgerPostingPort>();
        var ar = scope.ServiceProvider.GetRequiredService<IArSubledgerQueryService>();
        var party = Guid.NewGuid();
        var key = $"ar-{Guid.NewGuid():N}";

        var req = BuildAr(party, key, "INV-1", "InvoiceIssued", "Debit", 200m);
        var first = await port.PostArAsync(req);
        var second = await port.PostArAsync(req);
        first.Outcome.Should().Be(SubledgerPostOutcome.Accepted);
        second.Outcome.Should().Be(SubledgerPostOutcome.AlreadyProcessed);

        var credit = await port.PostArAsync(BuildAr(party, $"pay-{Guid.NewGuid():N}", "PAY-1", "PaymentCaptured", "Credit", 50m));
        credit.Outcome.Should().Be(SubledgerPostOutcome.Accepted);

        var balance = await ar.GetCustomerBalanceAsync(party);
        balance.Balance.Should().Be(150m);

        var statement = await ar.GetCustomerStatementAsync(new(party, null, null, null));
        statement.ClosingBalance.Should().Be(150m);
        statement.Items.Should().HaveCount(2);
        statement.Items.Last().RunningBalance.Should().Be(150m);
    }

    [Fact]
    public async Task Concurrent_identical_ar_posts_one_row()
    {
        var party = Guid.NewGuid();
        var key = $"c-{Guid.NewGuid():N}";
        var req = BuildAr(party, key, "SRC", "InvoiceIssued", "Debit", 10m);

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var port = scope.ServiceProvider.GetRequiredService<ISubledgerPostingPort>();
            return await port.PostArAsync(req);
        }));

        results.Count(r => r.Outcome == SubledgerPostOutcome.Accepted).Should().Be(1);
        results.Count(r => r.Outcome == SubledgerPostOutcome.AlreadyProcessed).Should().Be(19);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<FinanceDbContext>();
        (await db.SubledgerTransactions.CountAsync(x => x.IdempotencyKey == key)).Should().Be(1);
        (await db.JournalEntries.CountAsync(x => x.Description != null && x.Description.Contains(key))).Should().Be(0);
    }

    [Fact]
    public async Task Concurrent_different_ar_posts_both_succeed()
    {
        var party = Guid.NewGuid();
        var a = BuildAr(party, $"a-{Guid.NewGuid():N}", "A", "InvoiceIssued", "Debit", 40m);
        var b = BuildAr(party, $"b-{Guid.NewGuid():N}", "B", "InvoiceIssued", "Debit", 60m);

        await Task.WhenAll(
            Task.Run(async () =>
            {
                await using var s = _factory.Services.CreateAsyncScope();
                await s.ServiceProvider.GetRequiredService<ISubledgerPostingPort>().PostArAsync(a);
            }),
            Task.Run(async () =>
            {
                await using var s = _factory.Services.CreateAsyncScope();
                await s.ServiceProvider.GetRequiredService<ISubledgerPostingPort>().PostApAsync(
                    BuildAp(Guid.NewGuid(), $"ap-{Guid.NewGuid():N}", "S1", "ManualProbe", "Credit", 5m));
                await s.ServiceProvider.GetRequiredService<ISubledgerPostingPort>().PostArAsync(b);
            }));

        await using var scope = _factory.Services.CreateAsyncScope();
        var balance = await scope.ServiceProvider.GetRequiredService<IArSubledgerQueryService>().GetCustomerBalanceAsync(party);
        balance.Balance.Should().Be(100m);
    }

    [Fact]
    public async Task Ap_balance_and_no_po_assumption()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var port = scope.ServiceProvider.GetRequiredService<ISubledgerPostingPort>();
        var ap = scope.ServiceProvider.GetRequiredService<IApSubledgerQueryService>();
        var supplier = Guid.NewGuid();

        (await port.PostApAsync(BuildAp(supplier, $"ap1-{Guid.NewGuid():N}", "SI-1", "SupplierInvoicePosted", "Credit", 300m)))
            .Outcome.Should().Be(SubledgerPostOutcome.Accepted);
        (await port.PostApAsync(BuildAp(supplier, $"ap2-{Guid.NewGuid():N}", "SP-1", "SupplierPayment", "Debit", 100m)))
            .Outcome.Should().Be(SubledgerPostOutcome.Accepted);

        (await ap.GetSupplierBalanceAsync(supplier)).Balance.Should().Be(200m);
    }

    [Fact]
    public async Task Org_isolation_on_idempotency()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        var otherOrg = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var key = $"iso-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        var a = SubledgerTransaction.Post(Org, Branch, SubledgerType.Ar, Guid.NewGuid(), "Billing", "Invoice", "1",
            "InvoiceIssued", new DateOnly(2026, 1, 1), 1m, "EGP", SubledgerDirection.Debit, null, null, null,
            Guid.NewGuid(), key, "u", now);
        var b = SubledgerTransaction.Post(otherOrg, Branch, SubledgerType.Ar, Guid.NewGuid(), "Billing", "Invoice", "1",
            "InvoiceIssued", new DateOnly(2026, 1, 1), 1m, "EGP", SubledgerDirection.Debit, null, null, null,
            Guid.NewGuid(), key, "u", now);
        db.SubledgerTransactions.AddRange(a, b);
        await db.SaveChangesAsync();
        (await db.SubledgerTransactions.CountAsync(x => x.IdempotencyKey == key)).Should().Be(2);
    }

    [Fact]
    public async Task Aging_uses_transaction_date_when_no_due_date()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var port = scope.ServiceProvider.GetRequiredService<ISubledgerPostingPort>();
        var ar = scope.ServiceProvider.GetRequiredService<IArSubledgerQueryService>();
        var party = Guid.NewGuid();
        var oldDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-45));

        await port.PostArAsync(BuildAr(party, $"age-{Guid.NewGuid():N}", "INV-AGE", "InvoiceIssued", "Debit", 80m) with
        {
            TransactionDate = oldDate,
            DueDate = null
        });

        var aging = await ar.GetAgingAsync(new(party, null, null));
        aging.Items.Should().ContainSingle();
        aging.Items[0].DueDate.Should().BeNull();
        aging.Items[0].AgingBucket.Should().Be("31-60");
        aging.TotalOutstanding.Should().Be(80m);
    }

    [Fact]
    public async Task Unauthorized_ar_denied()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/finance/ar/customers/{Guid.NewGuid()}/balance");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    private static SubledgerPostRequest BuildAr(
        Guid party, string key, string sourceId, string eventType, string direction, decimal amount) =>
        new(Org, Branch, party, "Billing", "Invoice", sourceId, eventType, new DateOnly(2026, 6, 1),
            amount, "EGP", direction, "test", null, null, Guid.NewGuid(), key);

    private static SubledgerPostRequest BuildAp(
        Guid party, string key, string sourceId, string eventType, string direction, decimal amount) =>
        new(Org, Branch, party, "Test", "SupplierInvoice", sourceId, eventType, new DateOnly(2026, 6, 1),
            amount, "EGP", direction, "test AP foundation", null, null, Guid.NewGuid(), key);
}
