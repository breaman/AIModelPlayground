using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK26.Data.Interfaces;
using UserGroupSiteKimiK26.Data.Models;
using UserGroupSiteKimiK26.Shared.Dtos;
using UserGroupSiteKimiK26.Shared.Services;

namespace UserGroupSiteKimiK26.Server.Services;

public class UsersService(
    UserManager<User> userManager,
    IUserService currentUserService) : IUsersService
{
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await userManager.Users.ToListAsync();
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            });
        }

        return result;
    }

    public async Task UpdateUserRolesAsync(int userId, UpdateUserRolesDto dto)
    {
        if (userId == currentUserService.UserId && !dto.IsAdmin)
        {
            throw new InvalidOperationException("You cannot remove your own Admin role.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var currentRoles = await userManager.GetRolesAsync(user);

        if (dto.IsAdmin && !currentRoles.Contains("Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
        else if (!dto.IsAdmin && currentRoles.Contains("Admin"))
        {
            await userManager.RemoveFromRoleAsync(user, "Admin");
        }

        if (dto.IsSpeaker && !currentRoles.Contains("Speaker"))
        {
            await userManager.AddToRoleAsync(user, "Speaker");
        }
        else if (!dto.IsSpeaker && currentRoles.Contains("Speaker"))
        {
            await userManager.RemoveFromRoleAsync(user, "Speaker");
        }
    }
}