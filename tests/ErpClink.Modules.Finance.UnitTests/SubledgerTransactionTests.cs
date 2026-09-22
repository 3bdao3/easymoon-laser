using ErpClink.Modules.Finance.Domain.Subledgers;
using FluentAssertions;

namespace ErpClink.Modules.Finance.UnitTests;

public sealed class SubledgerTransactionTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void Ar_debit_increases_signed_impact()
    {
        var tx = Post(SubledgerType.Ar, SubledgerDirection.Debit, 100m);
        tx.SignedBalanceImpact.Should().Be(100m);
        tx.Status.Should().Be(SubledgerTransactionStatus.Posted);
    }

    [Fact]
    public void Ap_credit_increases_signed_impact()
    {
        var tx = Post(SubledgerType.Ap, SubledgerDirection.Credit, 75m);
        tx.SignedBalanceImpact.Should().Be(75m);
    }

    [Fact]
    public void Zero_amount_rejected()
    {
        var act = () => Post(SubledgerType.Ar, SubledgerDirection.Debit, 0m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Reversal_creates_compensating_and_marks_original()
    {
        var original = Post(SubledgerType.Ar, SubledgerDirection.Debit, 50m);
        var reversal = original.CreateReversal("rev-key", Guid.NewGuid(), "u", Now);
        original.Status.Should().Be(SubledgerTransactionStatus.Reversed);
        reversal.Direction.Should().Be(SubledgerDirection.Credit);
        reversal.ReversalOfTransactionId.Should().Be(original.Id);
        original.ReversedByTransactionId.Should().Be(reversal.Id);
        (original.SignedBalanceImpact + reversal.SignedBalanceImpact).Should().Be(0m);
    }

    [Fact]
    public void Posted_transaction_cannot_be_mutated_via_second_reversal_of_reversal()
    {
        var original = Post(SubledgerType.Ar, SubledgerDirection.Debit, 10m);
        var reversal = original.CreateReversal("rev-2", Guid.NewGuid(), "u", Now);
        var act = () => reversal.CreateReversal("rev-3", Guid.NewGuid(), "u", Now);
        act.Should().Throw<InvalidOperationException>();
    }

    private static SubledgerTransaction Post(SubledgerType type, SubledgerDirection direction, decimal amount) =>
        SubledgerTransaction.Post(
            Org, Guid.NewGuid(), type, Guid.NewGuid(), "Billing", "Invoice", Guid.NewGuid().ToString("N"),
            "InvoiceIssued", new DateOnly(2026, 3, 1), amount, "EGP", direction, "test", null, null,
            Guid.NewGuid(), Guid.NewGuid().ToString("N"), "u", Now);
}
