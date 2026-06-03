using UserGroupSiteQwen35.Data.Models;
using UserGroupSiteQwen35.Shared.Dto;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteQwen35.Server.Services;

public class UserManagementService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public UserManagementService(UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<UserWithRolesDto>> GetAllUsersWithRolesAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserWithRolesDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserWithRolesDto
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

    public async Task<UserWithRolesDto?> GetUserByIdAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserWithRolesDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList()
        };
    }

    public async Task<IdentityResult> AddRoleToUserAsync(int userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return IdentityResult.Failed(new IdentityError { Description = $"Role '{role}' does not exist" });
        }

        return await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<IdentityResult> RemoveRoleFromUserAsync(int userId, string role, int currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        // Prevent self-removal of Admin role
        if (userId == currentUserId && role == RoleNames.Admin)
        {
            return IdentityResult.Failed(new IdentityError { Description = "You cannot remove your own Admin role" });
        }

        return await _userManager.RemoveFromRoleAsync(user, role);
    }

    public async Task<bool> IsUserInRoleAsync(int userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<UserWithRolesDto?> GetCurrentUserWithRolesAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserWithRolesDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList()
        };
    }
}

