using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteGpt55.Data.Models;
using UserGroupSiteGpt55.Shared.Authorization;
using UserGroupSiteGpt55.Shared.Users;

namespace UserGroupSiteGpt55.Server.Services.Users;

/// <summary>
/// Implements admin user and role management through ASP.NET Core Identity.
/// </summary>
public sealed class ServerUserAdminService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IHttpContextAccessor httpContextAccessor) : IUserAdminService
{
    /// <summary>
    /// Lists registered users with their Admin and Speaker role state.
    /// </summary>
    public async Task<IReadOnlyList<UserAdminListItem>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
        var users = await userManager.Users.OrderBy(u => u.Email).ToListAsync(cancellationToken);
        var result = new List<UserAdminListItem>();

        foreach (var user in users)
        {
            result.Add(new UserAdminListItem(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email ?? user.UserName ?? "",
                await userManager.IsInRoleAsync(user, ApplicationRoles.Admin),
                await userManager.IsInRoleAsync(user, ApplicationRoles.Speaker),
                currentUserId == user.Id));
        }

        return result;
    }

    /// <summary>
    /// Updates Admin and Speaker roles while preventing the current admin from removing their own Admin role.
    /// </summary>
    public async Task<UserRoleUpdateResult> UpdateRolesAsync(UserRoleUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
        if (UserRoleRules.RemovesCurrentUsersAdminRole(currentUserId, request))
        {
            return UserRoleUpdateResult.Failure("You cannot remove your own Admin role.");
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserRoleUpdateResult.Failure("The selected user could not be found.");
        }

        var errors = new List<string>();
        await SetRoleAsync(user, ApplicationRoles.Admin, request.IsAdmin, errors);
        await SetRoleAsync(user, ApplicationRoles.Speaker, request.IsSpeaker, errors);

        if (errors.Count > 0)
        {
            return new UserRoleUpdateResult(false, errors);
        }

        if (currentUserId == user.Id)
        {
            await signInManager.RefreshSignInAsync(user);
        }

        return UserRoleUpdateResult.Success();
    }

    private async Task SetRoleAsync(User user, string roleName, bool shouldHaveRole, List<string> errors)
    {
        var hasRole = await userManager.IsInRoleAsync(user, roleName);
        if (hasRole == shouldHaveRole)
        {
            return;
        }

        var result = shouldHaveRole
            ? await userManager.AddToRoleAsync(user, roleName)
            : await userManager.RemoveFromRoleAsync(user, roleName);

        if (!result.Succeeded)
        {
            errors.AddRange(result.Errors.Select(e => e.Description));
        }
    }
}