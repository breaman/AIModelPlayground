using Microsoft.AspNetCore.Authorization;

namespace UserGroupSiteQwen35.Server.Authorization;

public class EventEditorHandler : AuthorizationHandler<EventEditorRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EventEditorRequirement requirement)
    {
        // Admin can edit any event
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Speaker can edit events
        if (context.User.IsInRole("Speaker"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // TODO: Check if user is an assigned speaker for the specific event
        // This would require accessing the event and checking the EventSpeaker join table

        return Task.CompletedTask;
    }
}