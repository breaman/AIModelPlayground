using UserGroupSiteFable5.Shared.Dtos;

namespace UserGroupSiteFable5.Shared.Services;

/// <summary>
/// Admin-only user and role management. Dual-implemented (HTTP client / database server)
/// per the WASM pre-rendering pattern.
/// </summary>
public interface IUserAdminService
{
    Task<List<UserAdminDto>> GetUsersAsync();

    /// <summary>
    /// Sets a user's Admin/Speaker membership. Fails when the acting admin attempts to
    /// remove their own Admin role (self-lockout guard).
    /// </summary>
    Task<ServiceResult> UpdateUserRolesAsync(int userId, UpdateUserRolesDto roles);
}
