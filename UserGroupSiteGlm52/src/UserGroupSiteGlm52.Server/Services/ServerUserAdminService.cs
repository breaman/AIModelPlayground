using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteGlm52.Data.Models;
using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Server.Services;

/// <summary>DB-backed <see cref="IUserAdminService"/> for managing user roles.</summary>
public sealed class ServerUserAdminService(
    UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor) : IUserAdminService
{
    private const string AdminRole = "Admin";
    private const string SpeakerRole = "Speaker";

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserWithRolesDto>> GetUsersAsync()
    {
        var currentUserId = CurrentUserId();

        // Load role membership once to avoid an N+1 of IsInRoleAsync per user.
        var users = await userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync();
        var adminIds = (await userManager.GetUsersInRoleAsync(AdminRole)).Select(u => u.Id).ToHashSet();
        var speakerIds = (await userManager.GetUsersInRoleAsync(SpeakerRole)).Select(u => u.Id).ToHashSet();

        return users
            .Select(u => new UserWithRolesDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsAdmin = adminIds.Contains(u.Id),
                IsSpeaker = speakerIds.Contains(u.Id),
                IsCurrentUser = u.Id == currentUserId
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateRolesAsync(int id, bool isAdmin, bool isSpeaker)
    {
        var currentUserId = CurrentUserId();

        // A user must not remove their own Admin role (would lock out management).
        if (id == currentUserId && !isAdmin)
        {
            return ServiceResult.Forbidden("You cannot remove your own Admin role.");
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return ServiceResult.Failed("User not found.");
        }

        await SetRoleAsync(user, AdminRole, isAdmin);
        await SetRoleAsync(user, SpeakerRole, isSpeaker);

        return ServiceResult.Success();
    }

    private async Task SetRoleAsync(User user, string role, bool shouldBeInRole)
    {
        var isInRole = await userManager.IsInRoleAsync(user, role);
        if (shouldBeInRole && !isInRole)
        {
            await userManager.AddToRoleAsync(user, role);
        }
        else if (!shouldBeInRole && isInRole)
        {
            await userManager.RemoveFromRoleAsync(user, role);
        }
    }

    private int CurrentUserId()
    {
        var nameId = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(nameId, out var id) ? id : 0;
    }
}