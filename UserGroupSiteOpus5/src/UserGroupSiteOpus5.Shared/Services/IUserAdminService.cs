using UserGroupSiteOpus5.Shared.Models;

namespace UserGroupSiteOpus5.Shared.Services;

/// <summary>
/// Member and role administration. Every method requires the caller to be an administrator.
/// </summary>
public interface IUserAdminService
{
    /// <summary>Returns every member with their current role assignments.</summary>
    Task<IReadOnlyList<UserListItem>> GetUsersAsync();

    /// <summary>
    /// Applies a role change to a member.
    /// </summary>
    /// <param name="model">The desired role state.</param>
    /// <remarks>
    /// Rejects an administrator removing their own Admin role: doing so would lock them out of the
    /// very screen they used to do it, with no way back in.
    /// </remarks>
    Task<SaveResult> UpdateUserRolesAsync(UserRoleUpdateModel model);
}
