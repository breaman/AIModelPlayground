using UserGroupSiteDeepSeekV4Pro.Data.Interfaces;
using UserGroupSiteDeepSeekV4Pro.Data.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Constants;
using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteDeepSeekV4Pro.Server.Services;

public class ServerUserManagementService(UserManager<User> userManager, IUserService userService) : IUserManagementService
{
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await userManager.Users
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                Roles = roles.ToList()
            });
        }

        return result;
    }

    public async Task UpdateUserRolesAsync(int userId, List<string> roles)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var currentRoles = await userManager.GetRolesAsync(user);

        // Guard: cannot remove own admin role
        if (user.Id == userService.UserId
            && currentRoles.Contains(RoleNames.Admin)
            && !roles.Contains(RoleNames.Admin))
        {
            throw new InvalidOperationException("Cannot remove your own Admin role.");
        }

        // Remove roles not in the new list
        var rolesToRemove = currentRoles.Except(roles).ToList();
        await userManager.RemoveFromRolesAsync(user, rolesToRemove);

        // Add roles not in the current list
        var rolesToAdd = roles.Except(currentRoles).ToList();
        await userManager.AddToRolesAsync(user, rolesToAdd);
    }
}
