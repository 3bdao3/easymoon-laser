using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using Microsoft.Extensions.Options;

namespace ErpClink.BuildingBlocks.Infrastructure.Organization;

public sealed class ConfigurationOrganizationContext : IOrganizationContext
{
    public ConfigurationOrganizationContext(IOptions<OrganizationOptions> options)
    {
        var value = options.Value;
        OrganizationId = value.DefaultOrganizationId;
        BranchId = value.DefaultBranchId;
    }

    public Guid OrganizationId { get; }
    public Guid BranchId { get; }
}
