using UserGroupSiteGlm52.Shared.Models;

namespace UserGroupSiteGlm52.Shared.Services;

/// <summary>Admin user management: list users and set their Admin/Speaker roles.</summary>
public interface IUserAdminService
{
    /// <summary>All users with their current role flags (and a self-marker for the calling user).</summary>
    Task<IReadOnlyList<UserWithRolesDto>> GetUsersAsync();

    /// <summary>Set a user's Admin and Speaker roles. Refuses to remove Admin from the calling user.</summary>
    Task<ServiceResult> UpdateRolesAsync(int id, bool isAdmin, bool isSpeaker);
}