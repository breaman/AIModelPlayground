using UserGroupSiteOpus48.Shared.Dtos;

namespace UserGroupSiteOpus48.Shared.Services;

/// <summary>
/// Admin operations to manage user role membership (Admin/Speaker). Dual-mode (Client HTTP / Server DB).
/// All operations require the Admin policy (enforced server-side).
/// </summary>
public interface IUserAdminService
{
    /// <summary>All users with their current Admin/Speaker membership.</summary>
    Task<UserAdminDto[]> GetUsersAsync();

    /// <summary>
    /// Grants or revokes a role for a user. The server rejects an admin removing their own Admin role.
    /// </summary>
    Task<OperationResult> SetRoleAsync(SetRoleRequest request);
}