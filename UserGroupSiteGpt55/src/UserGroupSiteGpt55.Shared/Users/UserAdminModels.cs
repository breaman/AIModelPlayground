namespace UserGroupSiteGpt55.Shared.Users;

/// <summary>
/// Provides a registered user projection for admin role management.
/// </summary>
public sealed record UserAdminListItem(
    int Id,
    string? FirstName,
    string? LastName,
    string Email,
    bool IsAdmin,
    bool IsSpeaker,
    bool IsCurrentUser);

/// <summary>
/// Provides role update input for admin user management.
/// </summary>
public sealed record UserRoleUpdateRequest(int UserId, bool IsAdmin, bool IsSpeaker);

/// <summary>
/// Provides the result of user role updates.
/// </summary>
public sealed record UserRoleUpdateResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static UserRoleUpdateResult Success()
    {
        return new UserRoleUpdateResult(true, []);
    }

    public static UserRoleUpdateResult Failure(params string[] errors)
    {
        return new UserRoleUpdateResult(false, errors);
    }
}

/// <summary>
/// Defines shared admin user management operations.
/// </summary>
public interface IUserAdminService
{
    Task<IReadOnlyList<UserAdminListItem>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserRoleUpdateResult> UpdateRolesAsync(UserRoleUpdateRequest request, CancellationToken cancellationToken = default);
}
