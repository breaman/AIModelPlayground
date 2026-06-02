using UserGroupSiteMiniMaxM3.Shared.Models;

namespace UserGroupSiteMiniMaxM3.Data.Services;

/// <summary>
/// Server-only admin user-management operations. Includes the self-demotion guard
/// that prevents an admin from removing their own <see cref="Roles.Admin"/> role.
/// Cross-platform consumers should depend on <see cref="Shared.Services.IUserAdminService"/>.
/// </summary>
public interface IUserAdminService
{
    /// <summary>Lists all users with their current roles.</summary>
    Task<IReadOnlyList<UserSummary>> ListUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates the roles assigned to a user, applying the self-demotion guard.</summary>
    /// <param name="targetUserId">User to modify.</param>
    /// <param name="isAdmin">True to add the Admin role, false to remove it.</param>
    /// <param name="isSpeaker">True to add the Speaker role, false to remove it.</param>
    /// <param name="currentUserId">The signed-in admin's user id (used for the self-demotion check).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">When a self-demotion is attempted.</exception>
    /// <exception cref="KeyNotFoundException">When the target user does not exist.</exception>
    Task UpdateRolesAsync(int targetUserId, bool isAdmin, bool isSpeaker, int currentUserId, CancellationToken cancellationToken = default);
}