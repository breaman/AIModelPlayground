using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;

namespace UserGroupSiteGlm51.Server.Services;

/// <summary>
/// Server-side implementation of <see cref="IUserManagementService"/> using
/// UserManager and RoleManager for role-based user management.
/// </summary>
public class UserManagementService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager) : IUserManagementService
{
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await userManager.Users
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<IList<string>> GetUserRolesAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        return await userManager.GetRolesAsync(user);
    }

    public async Task AddToRoleAsync(int userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        // Ensure the role exists before adding
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new Role { Name = role });
        }

        await userManager.AddToRoleAsync(user, role);
    }

    public async Task RemoveFromRoleAsync(int userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        await userManager.RemoveFromRoleAsync(user, role);
    }

    public async Task<bool> IsLastAdminAsync(int userId)
    {
        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole is null)
        {
            return false;
        }

        var adminCount = await userManager.GetUsersInRoleAsync("Admin");
        return adminCount.Count == 1 && adminCount[0].Id == userId;
    }
}