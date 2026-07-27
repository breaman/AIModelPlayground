namespace UserGroupSiteOpus5.Shared.Models;

/// <summary>A member as shown on the administration screen.</summary>
/// <param name="Id">The user id.</param>
/// <param name="FirstName">The member's first name.</param>
/// <param name="LastName">The member's last name.</param>
/// <param name="Email">The member's email address.</param>
/// <param name="MemberSince">When the member joined.</param>
/// <param name="IsAdmin">Whether the member holds the Admin role.</param>
/// <param name="IsSpeaker">Whether the member holds the Speaker role.</param>
public record UserListItem(
    int Id,
    string? FirstName,
    string? LastName,
    string? Email,
    DateTimeOffset MemberSince,
    bool IsAdmin,
    bool IsSpeaker)
{
    /// <summary>The member's full name, falling back to their email address.</summary>
    public string DisplayName =>
        string.Join(" ", new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) is { Length: > 0 } name
            ? name
            : Email ?? "Unnamed member";
}

/// <summary>A requested change to a member's roles.</summary>
/// <param name="UserId">The member whose roles are changing.</param>
/// <param name="IsAdmin">The desired Admin state.</param>
/// <param name="IsSpeaker">The desired Speaker state.</param>
public record UserRoleUpdateModel(int UserId, bool IsAdmin, bool IsSpeaker);
