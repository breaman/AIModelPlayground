using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK26.Data.Models;

namespace UserGroupSiteKimiK26.Server.Services;

public class EventEditorAuthorizationHandler(ApplicationDbContext dbContext) : AuthorizationHandler<EventEditorRequirement, int>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EventEditorRequirement requirement,
        int eventId)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var isSpeaker = await dbContext.EventSpeakers
            .AnyAsync(es => es.EventId == eventId && es.UserId == userId);

        if (isSpeaker)
        {
            context.Succeed(requirement);
        }
    }
}