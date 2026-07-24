using UserGroupSiteGpt56Sol.Data.Models;
using UserGroupSiteGpt56Sol.Shared.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteGpt56Sol.Server.Authorization;

/// <summary>Requires an administrator or a speaker assigned to the requested event.</summary>
public sealed class EventEditorRequirement : IAuthorizationRequirement;

/// <summary>Evaluates event edit access against current role and speaker assignments.</summary>
public sealed class EventEditorAuthorizationHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<EventEditorRequirement, int>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        EventEditorRequirement requirement, int eventId)
    {
        if (context.User.IsInRole(AppRoles.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        var identifier = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(identifier, out var userId) &&
            await dbContext.EventSpeakers.AnyAsync(item => item.EventId == eventId && item.UserId == userId))
        {
            context.Succeed(requirement);
        }
    }
}
