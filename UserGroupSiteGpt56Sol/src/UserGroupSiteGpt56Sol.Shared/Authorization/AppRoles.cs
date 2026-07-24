namespace UserGroupSiteGpt56Sol.Shared.Authorization;

/// <summary>Defines the only application roles that may be managed by the site.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Speaker = "Speaker";
    public static readonly string[] All = [Admin, Speaker];
}
