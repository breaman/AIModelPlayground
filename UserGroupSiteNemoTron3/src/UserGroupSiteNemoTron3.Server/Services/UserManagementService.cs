using UserGroupSiteNemoTron3.Data.Models;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteNemoTron3.Server.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public UserManagementService(UserManager<User> userManager, RoleManager<Role> roleManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task<UserDto[]> GetAllUsersAsync()
    {
        var users = await _dbContext.Users
            .OrderBy(u => u.UserName)
            .ToArrayAsync();

        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                MemberSince = user.MemberSince,
                IsAdmin = roles.Contains("Admin"),
                IsSpeaker = roles.Contains("Speaker")
            });
        }

        return result.ToArray();
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            MemberSince = user.MemberSince,
            IsAdmin = roles.Contains("Admin"),
            IsSpeaker = roles.Contains("Speaker")
        };
    }

    public async Task<UserDto> UpdateUserRolesAsync(int userId, UpdateUserRolesDto dto, int currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new InvalidOperationException("User not found");
        var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString()) ?? throw new InvalidOperationException("Current user not found");

        // Prevent self-removal from Admin role
        if (userId == currentUserId && dto.IsAdmin == false)
        {
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            if (currentUserRoles.Contains("Admin"))
            {
                throw new InvalidOperationException("Cannot remove yourself from Admin role");
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        // Update Admin role
        if (dto.IsAdmin && !currentRoles.Contains("Admin"))
        {
            await _userManager.AddToRoleAsync(user, "Admin");
        }
        else if (!dto.IsAdmin && currentRoles.Contains("Admin"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
        }

        // Update Speaker role
        if (dto.IsSpeaker && !currentRoles.Contains("Speaker"))
        {
            await _userManager.AddToRoleAsync(user, "Speaker");
        }
        else if (!dto.IsSpeaker && currentRoles.Contains("Speaker"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Speaker");
        }

        var updatedRoles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            MemberSince = user.MemberSince,
            IsAdmin = updatedRoles.Contains("Admin"),
            IsSpeaker = updatedRoles.Contains("Speaker")
        };
    }

    public async Task<bool> IsCurrentUserAdminAsync(int currentUserId)
    {
        var user = await _userManager.FindByIdAsync(currentUserId.ToString());
        if (user == null) return false;
        return await _userManager.IsInRoleAsync(user, "Admin");
    }
}