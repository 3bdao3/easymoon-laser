namespace ErpClink.Modules.Administration.Application.Options;

public sealed class InitialAdminOptions
{
    public const string SectionName = "Authentication:InitialAdmin";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = "System Super Admin";
}
