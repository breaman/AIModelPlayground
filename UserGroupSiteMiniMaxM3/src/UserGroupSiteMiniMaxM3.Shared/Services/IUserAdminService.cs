using UserGroupSiteMiniMaxM3.Shared.Models;

namespace UserGroupSiteMiniMaxM3.Shared.Services;

/// <summary>
/// Cross-platform service contract for admin user management. Implemented
/// server-side against <c>UserManager&lt;User&gt;</c> and client-side as an HTTP
/// client that calls the admin user-management API.
/// </summary>
public interface IUserAdminService
{
    /// <summary>Lists all users with their current roles (Admin only).</summary>
    Task<IReadOnlyList<UserSummary>> ListUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates a user's roles (Admin only). Throws on self-demotion attempts.</summary>
    Task UpdateRolesAsync(int targetUserId, bool isAdmin, bool isSpeaker, CancellationToken cancellationToken = default);
}