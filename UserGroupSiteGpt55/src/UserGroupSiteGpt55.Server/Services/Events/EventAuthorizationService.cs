using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteGpt55.Data.Models;
using UserGroupSiteGpt55.Data.Models.Events;
using UserGroupSiteGpt55.Shared.Authorization;

namespace UserGroupSiteGpt55.Server.Services.Events;

/// <summary>
/// Centralizes event editor checks for Admins and assigned speakers.
/// </summary>
public sealed class EventAuthorizationService(ApplicationDbContext dbContext, UserManager<User> userManager)
{
    /// <summary>
    /// Returns whether a user may edit a specific event.
    /// </summary>
    public async Task<bool> CanEditEventAsync(int userId, int eventId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        if (await userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
        {
            return true;
        }

        return await dbContext.EventSpeakers.AnyAsync(e => e.EventId == eventId && e.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Returns whether a user is an administrator.
    /// </summary>
    public async Task<bool> IsAdminAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.IsInRoleAsync(user, ApplicationRoles.Admin);
    }
}
