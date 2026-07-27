using System.Security.Claims;

using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteOpus5.Server.Authorization;

/// <summary>
/// Decides whether a user may edit a given event.
/// </summary>
/// <remarks>
/// <para>
/// An editor is any administrator, or a speaker assigned to that specific event. The event is
/// identified by the integer resource passed to
/// <see cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, object?, string)"/>.
/// </para>
/// <para>
/// An event id of zero means "a new event", which has no speakers and so is reachable by
/// administrators only. That falls out of the assignment lookup rather than being special-cased.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await authorizationService.AuthorizeAsync(user, eventId, PolicyNames.EventEditor);
/// if (!result.Succeeded) return SaveResult&lt;int&gt;.Failure("You may not edit this event.");
/// </code>
/// </example>
public class EventEditorAuthorizationHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<EventEditRequirement, int>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EventEditRequirement requirement,
        int eventId)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Administrators may edit every event, including ones they are not speaking at.
        if (context.User.IsInRole(RoleNames.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.IsInRole(RoleNames.Speaker))
        {
            return;
        }

        if (!int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return;
        }

        var isAssigned = await dbContext.EventSpeakers
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId && x.UserId == userId);

        if (isAssigned)
        {
            context.Succeed(requirement);
        }
    }
}
