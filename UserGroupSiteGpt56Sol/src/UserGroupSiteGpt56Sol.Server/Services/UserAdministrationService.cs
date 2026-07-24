using UserGroupSiteGpt56Sol.Data.Models;
using UserGroupSiteGpt56Sol.Shared.Authorization;
using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteGpt56Sol.Server.Services;

/// <summary>Implements the protected user-role administration workflow.</summary>
public sealed class UserAdministrationService(
    ApplicationDbContext dbContext,
    UserManager<User> userManager,
    CurrentUserAccessor currentUser,
    ILogger<UserAdministrationService> logger) : IUserAdministrationService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserAdminDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var users = await dbContext.Users.AsNoTracking()
            .OrderBy(user => user.FirstName).ThenBy(user => user.LastName).ThenBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.UserName,
                Roles = (from userRole in dbContext.UserRoles
                         join role in dbContext.Roles on userRole.RoleId equals role.Id
                         where userRole.UserId == user.Id
                         select role.Name!).ToList()
            })
            .ToListAsync(cancellationToken);

        return users.Select(user => new UserAdminDto(user.Id,
                DisplayName(user.FirstName, user.LastName, user.Email ?? user.UserName), user.Email ?? string.Empty,
                user.Roles, user.Id == currentUser.UserId))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateRolesAsync(int userId, UpdateUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return ServiceResult.Failure("That user no longer exists.");
        }

        if (userId == currentUser.UserId && !request.IsAdmin)
        {
            return ServiceResult.Failure("You cannot remove your own Admin role.");
        }

        var desiredRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (request.IsAdmin)
        {
            desiredRoles.Add(AppRoles.Admin);
        }

        if (request.IsSpeaker)
        {
            desiredRoles.Add(AppRoles.Speaker);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(user,
            currentRoles.Where(role => AppRoles.All.Contains(role, StringComparer.OrdinalIgnoreCase) &&
                                       !desiredRoles.Contains(role)));
        if (!removeResult.Succeeded)
        {
            return IdentityFailure(removeResult);
        }

        var addResult = await userManager.AddToRolesAsync(user,
            desiredRoles.Where(role => !currentRoles.Contains(role, StringComparer.OrdinalIgnoreCase)));
        if (!addResult.Succeeded)
        {
            return IdentityFailure(addResult);
        }

        await userManager.UpdateSecurityStampAsync(user);
        logger.LogInformation("Administrator {AdminId} updated roles for user {UserId} to {Roles}",
            currentUser.UserId, userId, desiredRoles);
        return ServiceResult.Success("Roles updated. The user must sign in again to refresh their session.");
    }

    /// <summary>Converts Identity errors to a safe service result.</summary>
    private static ServiceResult IdentityFailure(IdentityResult result) =>
        ServiceResult.Failure(string.Join(" ", result.Errors.Select(error => error.Description)));

    /// <summary>Builds a useful display name with an account-name fallback.</summary>
    private static string DisplayName(string? firstName, string? lastName, string? fallback)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? fallback ?? "Member" : name;
    }

    /// <summary>Throws when the request is not from an administrator.</summary>
    private void EnsureAdmin()
    {
        if (!currentUser.IsAdmin)
        {
            throw new UnauthorizedAccessException("Administrator access is required.");
        }
    }
}