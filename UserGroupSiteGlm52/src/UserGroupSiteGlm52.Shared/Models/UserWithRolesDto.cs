namespace UserGroupSiteGlm52.Shared.Models;

/// <summary>A user with role flags, for the admin user-management page.</summary>
public sealed record UserWithRolesDto
{
    public int Id { get; init; }
    public string Email { get; init; } = "";
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool IsAdmin { get; init; }
    public bool IsSpeaker { get; init; }

    /// <summary>True when this row is the currently logged-in user (disables self-Admin-toggle in the UI).</summary>
    public bool IsCurrentUser { get; init; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? Email
            : $"{FirstName} {LastName}".Trim();
}