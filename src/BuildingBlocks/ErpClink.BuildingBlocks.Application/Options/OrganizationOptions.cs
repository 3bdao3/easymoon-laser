namespace ErpClink.BuildingBlocks.Application.Options;

public sealed class OrganizationOptions
{
    public const string SectionName = "Organization";

    /// <summary>Default V1 organization id (single-org deployment).</summary>
    public Guid DefaultOrganizationId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Default V1 branch id (single-branch deployment).</summary>
    public Guid DefaultBranchId { get; set; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
}
