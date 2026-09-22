namespace ErpClink.BuildingBlocks.Application.Abstractions;

/// <summary>
/// V1 organization/branch context. Values come from server configuration, never from the client.
/// </summary>
public interface IOrganizationContext
{
    Guid OrganizationId { get; }
    Guid BranchId { get; }
}
