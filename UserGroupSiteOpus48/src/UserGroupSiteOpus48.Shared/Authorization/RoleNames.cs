namespace UserGroupSiteOpus48.Shared.Authorization;

/// <summary>
/// Canonical role name constants so role strings are never hard-coded throughout the app.
/// These map to ASP.NET Identity roles seeded at startup.
/// </summary>
public static class RoleNames
{
    /// <summary>Full administrative access (user/role management, event create/edit).</summary>
    public const string Admin = "Admin";

    /// <summary>A user who can be assigned as a speaker on events and edit their own events.</summary>
    public const string Speaker = "Speaker";
}