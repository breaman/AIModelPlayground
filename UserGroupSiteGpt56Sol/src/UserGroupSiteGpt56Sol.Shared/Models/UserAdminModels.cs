namespace UserGroupSiteGpt56Sol.Shared.Models;

/// <summary>Displays one user and their editable role membership.</summary>
public sealed record UserAdminDto(int Id, string DisplayName, string Email,
    IReadOnlyList<string> Roles, bool IsCurrentUser);

/// <summary>Represents an administrator's role membership update.</summary>
public sealed class UpdateUserRolesRequest
{
    public bool IsAdmin { get; set; }
    public bool IsSpeaker { get; set; }
}
