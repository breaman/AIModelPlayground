namespace UserGroupSiteGpt55.Shared.Users;

/// <summary>
/// Provides shared role-management guard rules.
/// </summary>
public static class UserRoleRules
{
    /// <summary>
    /// Returns whether a role update attempts to remove the current user's own Admin role.
    /// </summary>
    public static bool RemovesCurrentUsersAdminRole(int? currentUserId, UserRoleUpdateRequest request)
    {
        return currentUserId == request.UserId && !request.IsAdmin;
    }
}