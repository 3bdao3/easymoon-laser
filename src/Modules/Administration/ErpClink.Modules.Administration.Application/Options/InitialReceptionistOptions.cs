namespace ErpClink.Modules.Administration.Application.Options;

public sealed class InitialReceptionistOptions
{
    public const string SectionName = "Authentication:InitialReceptionist";

    /// <summary>Login username (also accepted by auth in addition to email).</summary>
    public string UserName { get; set; } = "socialmedia";

    public string Email { get; set; } = "socialmedia@easymoon.local";

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = "موظفة السوشيال ميديا";
}
