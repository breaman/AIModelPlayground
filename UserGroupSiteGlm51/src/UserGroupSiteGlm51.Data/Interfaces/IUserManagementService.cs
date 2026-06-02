using UserGroupSiteGlm51.Data.Models;

namespace UserGroupSiteGlm51.Data.Interfaces;

/// <summary>
/// Service for admin-level user management — listing users and managing role assignments.
/// </summary>
public interface IUserManagementService
{
    /// <summary>Gets all registered users.</summary>
    Task<IEnumerable<User>> GetAllUsersAsync();

    /// <summary>Gets a user by ID. Returns null if not found.</summary>
    Task<User?> GetUserByIdAsync(int userId);

    /// <summary>Gets the roles assigned to a user.</summary>
    Task<IList<string>> GetUserRolesAsync(int userId);

    /// <summary>Adds a user to a role.</summary>
    Task AddToRoleAsync(int userId, string role);

    /// <summary>Removes a user from a role.</summary>
    Task RemoveFromRoleAsync(int userId, string role);

    /// <summary>
    /// Checks if removing this user from the Admin role would leave no admins.
    /// Returns true if the user is the only Admin.
    /// </summary>
    Task<bool> IsLastAdminAsync(int userId);
}