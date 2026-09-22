using ErpClink.Modules.Finance.Domain.Integration;
using FluentAssertions;

namespace ErpClink.Modules.Finance.UnitTests;

public sealed class AccountingIntegrationRequestTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void CreatePending_sets_expected_state()
    {
        var correlation = Guid.NewGuid();
        var entity = AccountingIntegrationRequest.CreatePending(
            Org, Guid.NewGuid(), "Billing", "Invoice", "INV-1", "InvoiceIssued",
            "key-1", correlation, "desc", "SAR", Now, Now);

        entity.Status.Should().Be(AccountingIntegrationStatus.Pending);
        entity.CorrelationId.Should().Be(correlation);
        entity.CurrencyCode.Should().Be("SAR");
    }

    [Fact]
    public void Failed_can_reset_for_retry_without_becoming_succeeded()
    {
        var entity = AccountingIntegrationRequest.CreatePending(
            Org, null, "Test", "Manual", "1", "Probe", "k", Guid.NewGuid(), null, null, Now, Now);
        entity.MarkFailed("finance.test", "boom", Now);
        entity.Status.Should().Be(AccountingIntegrationStatus.Failed);
        entity.ErrorCode.Should().Be("finance.test");

        var retryCorrelation = Guid.NewGuid();
        entity.ResetForRetry(retryCorrelation, Now.AddMinutes(1));
        entity.Status.Should().Be(AccountingIntegrationStatus.Pending);
        entity.ErrorCode.Should().BeNull();
        entity.CorrelationId.Should().Be(retryCorrelation);
        entity.Status.Should().NotBe(AccountingIntegrationStatus.Succeeded);
    }

    [Fact]
    public void Succeeded_cannot_be_marked_processing()
    {
        var entity = AccountingIntegrationRequest.CreatePending(
            Org, null, "Test", "Manual", "1", "Probe", "k2", Guid.NewGuid(), null, null, Now, Now);
        entity.MarkSucceeded(Now);
        var act = () => entity.MarkProcessing(Now);
        act.Should().Throw<InvalidOperationException>();
    }
}
