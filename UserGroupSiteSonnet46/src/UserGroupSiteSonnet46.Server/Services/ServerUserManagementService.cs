using UserGroupSiteSonnet46.Data.Models;
using UserGroupSiteSonnet46.Shared.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteSonnet46.Server.Services;

/// <summary>
/// Server-side user management that operates directly against ASP.NET Identity stores.
/// Registered in the Server DI container; the Client project uses <see cref="ClientUserManagementService"/> instead.
/// </summary>
public class ServerUserManagementService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager) : IUserManagementService
{
    /// <inheritdoc />
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await userManager.Users.ToListAsync();

        var result = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                IsAdmin: roles.Contains("Admin"),
                IsSpeaker: roles.Contains("Speaker")));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task SetRoleAsync(int userId, string role, bool enabled)
    {
        // Ensure the role exists before assigning it
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new Role { Name = role });
        }

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        if (enabled)
        {
            await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            await userManager.RemoveFromRoleAsync(user, role);
        }
    }
}
