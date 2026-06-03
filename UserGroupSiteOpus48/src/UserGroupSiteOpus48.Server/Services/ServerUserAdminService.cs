using UserGroupSiteOpus48.Data.Interfaces;
using UserGroupSiteOpus48.Data.Models;
using UserGroupSiteOpus48.Shared.Authorization;
using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteOpus48.Server.Services;

/// <summary>
/// Server-side <see cref="IUserAdminService"/> for managing Admin/Speaker membership. Enforces that
/// only the Admin and Speaker roles are toggled and that an admin cannot remove their own Admin role.
/// </summary>
public class ServerUserAdminService(
    UserManager<User> userManager,
    IUserService userService) : IUserAdminService
{
    public async Task<UserAdminDto[]> GetUsersAsync()
    {
        // Resolve role membership once via set lookups rather than per-user role queries.
        var adminIds = (await userManager.GetUsersInRoleAsync(RoleNames.Admin)).Select(u => u.Id).ToHashSet();
        var speakerIds = (await userManager.GetUsersInRoleAsync(RoleNames.Speaker)).Select(u => u.Id).ToHashSet();

        return userManager.Users
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .AsEnumerable()
            .Select(u => new UserAdminDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                adminIds.Contains(u.Id),
                speakerIds.Contains(u.Id)))
            .ToArray();
    }

    public async Task<OperationResult> SetRoleAsync(SetRoleRequest request)
    {
        if (request.Role is not (RoleNames.Admin or RoleNames.Speaker))
        {
            return OperationResult.Fail($"Unknown role '{request.Role}'.");
        }

        // An admin cannot remove their own Admin role (prevents locking out the last admin via self-demotion).
        if (request.Role == RoleNames.Admin && !request.InRole && request.UserId == userService.UserId)
        {
            return OperationResult.Fail("You cannot remove your own Admin role.");
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return OperationResult.Fail("User not found.");
        }

        var isInRole = await userManager.IsInRoleAsync(user, request.Role);
        IdentityResult result;

        if (request.InRole && !isInRole)
        {
            result = await userManager.AddToRoleAsync(user, request.Role);
        }
        else if (!request.InRole && isInRole)
        {
            result = await userManager.RemoveFromRoleAsync(user, request.Role);
        }
        else
        {
            // Already in the desired state; nothing to do.
            return OperationResult.Ok();
        }

        return result.Succeeded
            ? OperationResult.Ok()
            : OperationResult.Fail([.. result.Errors.Select(e => e.Description)]);
    }
}
