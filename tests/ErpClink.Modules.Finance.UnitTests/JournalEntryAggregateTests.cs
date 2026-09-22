using ErpClink.Modules.Finance.Domain.Accounts;
using ErpClink.Modules.Finance.Domain.FiscalPeriods;
using ErpClink.Modules.Finance.Domain.FiscalYears;
using ErpClink.Modules.Finance.Domain.Journals;
using FluentAssertions;

namespace ErpClink.Modules.Finance.UnitTests;

public sealed class JournalEntryAggregateTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void Balanced_two_line_journal_posts()
    {
        var cash = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        var entry = JournalEntry.CreateDraft(Org, Branch, "JE-2026-000001", new DateOnly(2026, 1, 15), "Test", "u", Now);
        entry.AddLine(cash, null, 100m, 0m, 1, "u", Now);
        entry.AddLine(revenue, null, 0m, 100m, 2, "u", Now);
        entry.EnsureBalanced();
        entry.Post(Guid.NewGuid(), "u", Now);
        entry.Status.Should().Be(JournalStatus.Posted);
        entry.TotalDebit.Should().Be(100m);
        entry.TotalCredit.Should().Be(100m);
    }

    [Fact]
    public void One_line_rejected_on_post()
    {
        var entry = JournalEntry.CreateDraft(Org, Branch, "JE-2026-000002", new DateOnly(2026, 1, 15), null, "u", Now);
        entry.AddLine(Guid.NewGuid(), null, 100m, 0m, 1, "u", Now);
        var act = () => entry.EnsureBalanced();
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least two*");
    }

    [Fact]
    public void Both_debit_and_credit_on_line_rejected()
    {
        var act = () => JournalEntryLine.Create(Guid.NewGuid(), Guid.NewGuid(), null, 10m, 10m, 1);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Zero_debit_and_credit_rejected()
    {
        var act = () => JournalEntryLine.Create(Guid.NewGuid(), Guid.NewGuid(), null, 0m, 0m, 1);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Unbalanced_journal_rejected()
    {
        var entry = JournalEntry.CreateDraft(Org, Branch, "JE-2026-000003", new DateOnly(2026, 1, 15), null, "u", Now);
        entry.AddLine(Guid.NewGuid(), null, 100m, 0m, 1, "u", Now);
        entry.AddLine(Guid.NewGuid(), null, 0m, 50m, 2, "u", Now);
        var act = () => entry.EnsureBalanced();
        act.Should().Throw<InvalidOperationException>().WithMessage("*not balanced*");
    }

    [Fact]
    public void Posted_journal_cannot_be_updated()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var entry = JournalEntry.CreateDraft(Org, Branch, "JE-2026-000004", new DateOnly(2026, 1, 15), null, "u", Now);
        entry.AddLine(a, null, 50m, 0m, 1, "u", Now);
        entry.AddLine(b, null, 0m, 50m, 2, "u", Now);
        entry.Post(Guid.NewGuid(), "u", Now);
        var act = () => entry.UpdateDraft(new DateOnly(2026, 1, 16), "x", "u", Now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reversal_swaps_debit_and_credit()
    {
        var cash = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        var original = JournalEntry.CreateDraft(Org, Branch, "JE-2026-000005", new DateOnly(2026, 1, 15), null, "u", Now);
        original.AddLine(cash, null, 200m, 0m, 1, "u", Now);
        original.AddLine(revenue, null, 0m, 200m, 2, "u", Now);
        original.Post(Guid.NewGuid(), "u", Now);

        var reversal = JournalEntry.CreatePostedReversal(
            Org, Branch, "JE-2026-000006", original.JournalDate, null, Guid.NewGuid(), original, "u", Now);

        reversal.Lines.Should().HaveCount(2);
        reversal.Lines.Single(l => l.AccountId == cash).Debit.Should().Be(0m);
        reversal.Lines.Single(l => l.AccountId == cash).Credit.Should().Be(200m);
        reversal.Lines.Single(l => l.AccountId == revenue).Debit.Should().Be(200m);
        reversal.Lines.Single(l => l.AccountId == revenue).Credit.Should().Be(0m);
        reversal.Status.Should().Be(JournalStatus.Posted);
    }

    [Fact]
    public void Inactive_account_cannot_be_used_on_line()
    {
        var account = Account.Create(Org, "1000", "Cash", null, AccountType.Asset, true, null, "u", Now);
        account.Deactivate("u", Now);
        var act = () => account.EnsureUsableForJournalLine();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Inactive*");
    }

    [Fact]
    public void Non_postable_account_rejected()
    {
        var account = Account.Create(Org, "9000", "Header", null, AccountType.Asset, false, null, "u", Now);
        var act = () => account.EnsureUsableForJournalLine();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Non-postable*");
    }

    [Fact]
    public void Fiscal_year_overlap_detected()
    {
        var y1 = FiscalYear.Create(Org, "FY2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "u", Now);
        y1.Overlaps(new DateOnly(2026, 6, 1), new DateOnly(2027, 5, 31)).Should().BeTrue();
        y1.Overlaps(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31)).Should().BeFalse();
    }

    [Fact]
    public void Closed_period_rejects_posting()
    {
        var period = FiscalPeriod.Create(Org, Guid.NewGuid(), "Jan", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "u", Now);
        period.Close("u", Now);
        var act = () => period.EnsureOpenForPosting();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Account_cannot_be_own_parent_on_update()
    {
        var account = Account.Create(Org, "1100", "Child", null, AccountType.Asset, true, null, "u", Now);
        var act = () => account.Update("x", null, AccountType.Asset, true, account.Id, "u", Now);
        act.Should().Throw<InvalidOperationException>();
    }
}
