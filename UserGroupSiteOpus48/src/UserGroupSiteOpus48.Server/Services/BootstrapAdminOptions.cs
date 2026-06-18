namespace UserGroupSiteOpus48.Server.Services;

/// <summary>
/// Configuration for the bootstrap admin account created at startup so there is always at
/// least one administrator. Bind from the "BootstrapAdmin" configuration section
/// (appsettings or, preferably, user-secrets for the password).
/// </summary>
public class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    /// <summary>Email/username of the bootstrap admin.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Initial password used only when the account is first created.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional first name for the seeded admin.</summary>
    public string? FirstName { get; set; }

    /// <summary>Optional last name for the seeded admin.</summary>
    public string? LastName { get; set; }
}