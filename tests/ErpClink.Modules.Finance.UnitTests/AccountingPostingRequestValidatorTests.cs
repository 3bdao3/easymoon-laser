using ErpClink.Modules.Finance.Application.Integration;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ErpClink.Modules.Finance.UnitTests;

public sealed class AccountingPostingRequestValidatorTests
{
    private readonly AccountingPostingRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var request = ValidRequest();
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
        request.ToSourceReference().OrganizationId.Should().NotBeEmpty();
        request.ToSourceReference().SourceModule.Should().Be(AccountingSourceModules.Test);
    }

    [Fact]
    public void OrganizationId_required()
    {
        var result = _validator.TestValidate(ValidRequest() with { OrganizationId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void IdempotencyKey_required()
    {
        var result = _validator.TestValidate(ValidRequest() with { IdempotencyKey = "" });
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void Source_reference_fields_required()
    {
        _validator.TestValidate(ValidRequest() with { SourceModule = "" }).ShouldHaveValidationErrorFor(x => x.SourceModule);
        _validator.TestValidate(ValidRequest() with { SourceType = "" }).ShouldHaveValidationErrorFor(x => x.SourceType);
        _validator.TestValidate(ValidRequest() with { SourceId = "" }).ShouldHaveValidationErrorFor(x => x.SourceId);
        _validator.TestValidate(ValidRequest() with { EventType = "" }).ShouldHaveValidationErrorFor(x => x.EventType);
    }

    private static AccountingPostingRequest ValidRequest() =>
        new(
            OrganizationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            BranchId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            SourceModule: AccountingSourceModules.Test,
            SourceType: "ManualProbe",
            SourceId: "PROBE-1",
            EventType: "AccountingIntegrationProbe",
            OccurredAtUtc: DateTime.UtcNow,
            CorrelationId: Guid.NewGuid(),
            IdempotencyKey: $"test-{Guid.NewGuid():N}");
}
