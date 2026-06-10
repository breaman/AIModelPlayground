using UserGroupSiteFable5.Data.Interfaces;
using UserGroupSiteFable5.Data.Models;
using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteFable5.Server.Services;

/// <summary>
/// Server-side <see cref="IUserAdminService"/> backing the admin role-management page
/// and its API endpoints.
/// </summary>
public class UserAdminService(
    ApplicationDbContext dbContext,
    UserManager<User> userManager,
    IUserService currentUser) : IUserAdminService
{
    public async Task<List<UserAdminDto>> GetUsersAsync()
    {
        // Single round-trip: project role membership inline instead of N calls to UserManager.
        return await dbContext.Users
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ThenBy(u => u.Email)
            .Select(u => new UserAdminDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty,
                MemberSince = u.MemberSince,
                IsAdmin = dbContext.UserRoles.Any(ur => ur.UserId == u.Id &&
                    dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")),
                IsSpeaker = dbContext.UserRoles.Any(ur => ur.UserId == u.Id &&
                    dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Speaker"))
            })
            .ToListAsync();
    }

    public async Task<ServiceResult> UpdateUserRolesAsync(int userId, UpdateUserRolesDto roles)
    {
        // Self-lockout guard: an admin can never remove their own Admin role. The client
        // disables the toggle too, but this server check is the source of truth.
        if (currentUser.UserId == userId && !roles.IsAdmin)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "You cannot remove your own Admin role.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "User not found.");
        }

        var result = await SetRoleMembershipAsync(user, "Admin", roles.IsAdmin)
                     ?? await SetRoleMembershipAsync(user, "Speaker", roles.IsSpeaker);

        return result ?? ServiceResult.Ok;
    }

    /// <summary>Adds or removes a role to match the desired state; null result means success.</summary>
    private async Task<ServiceResult?> SetRoleMembershipAsync(User user, string role, bool shouldBeMember)
    {
        var isMember = await userManager.IsInRoleAsync(user, role);
        if (isMember == shouldBeMember)
        {
            return null;
        }

        var identityResult = shouldBeMember
            ? await userManager.AddToRoleAsync(user, role)
            : await userManager.RemoveFromRoleAsync(user, role);

        return identityResult.Succeeded
            ? null
            : ServiceResult.Fail(ServiceErrorType.Validation,
                string.Join("; ", identityResult.Errors.Select(e => e.Description)));
    }
}
