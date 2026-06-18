using UserGroupSiteNemoTron3.Data.Interfaces;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Server.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var eventsGroup = app.MapGroup("/api/events").WithTags("Events");
        var adminEventsGroup = app.MapGroup("/api/admin/events").WithTags("Admin Events").RequireAuthorization("SpeakerOrAdmin");

        // Public endpoints
        eventsGroup.MapGet("/", async (IEventService eventService) =>
        {
            var events = await eventService.GetPublishedEventsAsync();
            return Results.Ok(events);
        });

        eventsGroup.MapGet("/{slug}", async (string slug, IEventService eventService) =>
        {
            var evt = await eventService.GetEventBySlugAsync(slug);
            return evt is not null ? Results.Ok(evt) : Results.NotFound();
        });

        // Admin/Speaker endpoints
        adminEventsGroup.MapGet("/", async (IEventService eventService) =>
        {
            // Get all events (including drafts) for admin/speaker
            var events = await eventService.GetPublishedEventsAsync(); // TODO: Add GetAllEventsAsync for admin
            return Results.Ok(events);
        });

        adminEventsGroup.MapPost("/", async (CreateEventDto dto, IEventService eventService, IUserService userService) =>
        {
            var evt = await eventService.CreateEventAsync(dto);
            return Results.Created($"/api/events/{evt.Slug}", evt);
        }).RequireAuthorization("AdminOnly");

        adminEventsGroup.MapPut("/{id:int}", async (int id, UpdateEventDto dto, IEventService eventService, IUserService userService) =>
        {
            // Check if user can edit this event (Admin or assigned speaker)
            var canEdit = await eventService.CanUserEditEventAsync(id, userService.UserId);
            if (!canEdit)
            {
                return Results.Forbid();
            }

            var evt = await eventService.UpdateEventAsync(id, dto);
            return Results.Ok(evt);
        });

        adminEventsGroup.MapDelete("/{id:int}", async (int id, IEventService eventService) =>
        {
            await eventService.DeleteEventAsync(id);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        // Admin-only endpoint for generating slug
        adminEventsGroup.MapPost("/generate-slug", async (string title, IEventService eventService) =>
        {
            var slug = await eventService.GenerateSlugAsync(title);
            return Results.Ok(new { slug });
        }).RequireAuthorization("AdminOnly");
    }
}