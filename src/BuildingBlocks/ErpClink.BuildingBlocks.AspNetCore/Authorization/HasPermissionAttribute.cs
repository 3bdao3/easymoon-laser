using Microsoft.AspNetCore.Authorization;

namespace ErpClink.BuildingBlocks.AspNetCore.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionCode)
        : base(policy: PermissionPolicyProvider.PolicyPrefix + permissionCode)
    {
        PermissionCode = permissionCode;
    }

    public string PermissionCode { get; }
}
