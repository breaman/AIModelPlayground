namespace UserGroupSiteOpus5.Shared.Common;

/// <summary>
/// The application's role names, defined once so that <c>[Authorize(Roles = ...)]</c> attributes,
/// role seeding, and role-management code cannot drift apart.
/// </summary>
public static class RoleNames
{
    /// <summary>Full administrative access: manage users, roles, and every event.</summary>
    public const string Admin = "Admin";

    /// <summary>May be assigned to an event and edit the events they are assigned to.</summary>
    public const string Speaker = "Speaker";

    /// <summary>Every role the application seeds and recognises.</summary>
    public static readonly IReadOnlyList<string> All = [Admin, Speaker];
}
