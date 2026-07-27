using System.Security.Claims;

using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteOpus5.Server.Services;

/// <summary>
/// Server-side implementation of <see cref="IUserAdminService"/>.
/// </summary>
public class ServerUserAdminService(
    ApplicationDbContext dbContext,
    UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ServerUserAdminService> logger) : IUserAdminService
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserListItem>> GetUsersAsync()
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return [];
        }

        // Role membership is resolved with a single join rather than a UserManager call per user,
        // which would be one round trip per row.
        var roleAssignments = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                select new { userRole.UserId, role.Name })
            .ToListAsync();

        var rolesByUser = roleAssignments
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.Select(r => r.Name).ToHashSet());

        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email, x.MemberSince })
            .ToListAsync();

        return users
            .Select(x =>
            {
                var roles = rolesByUser.GetValueOrDefault(x.Id) ?? [];
                return new UserListItem(
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.Email,
                    x.MemberSince,
                    roles.Contains(RoleNames.Admin),
                    roles.Contains(RoleNames.Speaker));
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<SaveResult> UpdateUserRolesAsync(UserRoleUpdateModel model)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return SaveResult.Failure("Only an administrator can change roles.");
        }

        var currentUserId = CurrentUserId;

        if (currentUserId is null)
        {
            return SaveResult.Failure("Your session could not be identified. Sign in again.");
        }

        var target = await userManager.FindByIdAsync(model.UserId.ToString());

        if (target is null)
        {
            return SaveResult.Failure("That member no longer exists.");
        }

        var isTargetAdmin = await userManager.IsInRoleAsync(target, RoleNames.Admin);
        var isTargetSpeaker = await userManager.IsInRoleAsync(target, RoleNames.Speaker);

        // The requirement: an administrator may not remove their own Admin role. The disabled
        // checkbox in the UI is cosmetic — this check is what stops a hand-crafted PUT to
        // /api/admin/users/{id}/roles from locking the last route into administration behind a
        // demotion the user cannot undo.
        if (model.UserId == currentUserId && isTargetAdmin && !model.IsAdmin)
        {
            return SaveResult.Failure("You cannot remove your own Admin role.");
        }

        var errors = new List<string>();

        if (model.IsAdmin && !isTargetAdmin)
        {
            errors.AddRange(await ApplyAsync(userManager.AddToRoleAsync(target, RoleNames.Admin)));
        }
        else if (!model.IsAdmin && isTargetAdmin)
        {
            errors.AddRange(await ApplyAsync(userManager.RemoveFromRoleAsync(target, RoleNames.Admin)));
        }

        if (model.IsSpeaker && !isTargetSpeaker)
        {
            errors.AddRange(await ApplyAsync(userManager.AddToRoleAsync(target, RoleNames.Speaker)));
        }
        else if (!model.IsSpeaker && isTargetSpeaker)
        {
            errors.AddRange(await ApplyAsync(userManager.RemoveFromRoleAsync(target, RoleNames.Speaker)));
        }

        if (errors.Count > 0)
        {
            return SaveResult.Failure(errors);
        }

        // Role claims live in the target's auth cookie, so without a new security stamp their
        // existing session keeps the old roles until they sign in again. Bumping it makes the
        // cookie fail its next validation and be reissued.
        await userManager.UpdateSecurityStampAsync(target);

        logger.LogInformation(
            "User {ActorId} set roles on user {TargetId}: Admin={IsAdmin}, Speaker={IsSpeaker}.",
            currentUserId, model.UserId, model.IsAdmin, model.IsSpeaker);

        return SaveResult.Success();
    }

    /// <summary>Awaits an Identity operation and returns its error descriptions, if any.</summary>
    private static async Task<IEnumerable<string>> ApplyAsync(Task<IdentityResult> operation)
    {
        var result = await operation;
        return result.Succeeded ? [] : result.Errors.Select(x => x.Description);
    }
}
