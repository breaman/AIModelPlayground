using System.Security.Claims;

using UserGroupSiteDeepSeekV4Pro.Shared.Constants;
using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Server.Endpoints;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        // GET /api/events/published - Anonymous
        group.MapGet("/published", async (IEventService eventService) =>
        {
            var events = await eventService.GetPublishedEventsAsync();
            return Results.Ok(events);
        });

        // GET /api/events - Admin/Speaker - all events
        group.MapGet("/", async (IEventService eventService) =>
        {
            var events = await eventService.GetAllEventsAsync();
            return Results.Ok(events);
        }).RequireAuthorization(Policies.CanManageEvents);

        // GET /api/events/{id}
        group.MapGet("/{id:int}", async (IEventService eventService, int id, ClaimsPrincipal user) =>
        {
            var evt = await eventService.GetEventByIdAsync(id);
            if (evt is null)
                return Results.NotFound();

            // Anonymous access allowed if published, otherwise requires editor role
            if (!evt.IsPublished && !user.IsInRole(RoleNames.Admin) && !user.IsInRole(RoleNames.Speaker))
                return Results.Forbid();

            return Results.Ok(evt);
        });

        // GET /api/events/slug/{slug}
        group.MapGet("/slug/{slug}", async (IEventService eventService, string slug, ClaimsPrincipal user) =>
        {
            var evt = await eventService.GetEventBySlugAsync(slug);
            if (evt is null)
                return Results.NotFound();

            if (!evt.IsPublished && !user.IsInRole(RoleNames.Admin) && !user.IsInRole(RoleNames.Speaker))
                return Results.Forbid();

            return Results.Ok(evt);
        });

        // POST /api/events - Admin only
        group.MapPost("/", async (IEventService eventService, EventDto dto, ClaimsPrincipal user) =>
        {
            if (!user.IsInRole(RoleNames.Admin))
                return Results.Forbid();

            var (isValid, errorMessage) = ValidateEvent(dto);
            if (!isValid)
                return Results.BadRequest(errorMessage);

            var created = await eventService.CreateEventAsync(dto);
            return Results.Created($"/api/events/{created.Id}", created);
        }).RequireAuthorization(Policies.CanManageEvents);

        // PUT /api/events/{id} - Admin or assigned Speaker
        group.MapPut("/{id:int}", async (IEventService eventService, int id, EventDto dto, ClaimsPrincipal user) =>
        {
            var existing = await eventService.GetEventByIdAsync(id);
            if (existing is null)
                return Results.NotFound();

            // Check authorization: Admin can edit any; Speaker can only edit their own events
            if (!user.IsInRole(RoleNames.Admin))
            {
                var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (!existing.SpeakerUserIds.Contains(userId))
                    return Results.Forbid();
            }

            var (isValid, errorMessage) = ValidateEvent(dto);
            if (!isValid)
                return Results.BadRequest(errorMessage);

            dto.Id = id;
            var updated = await eventService.UpdateEventAsync(dto);
            return Results.Ok(updated);
        }).RequireAuthorization(Policies.CanManageEvents);

        // DELETE /api/events/{id} - Admin only
        group.MapDelete("/{id:int}", async (IEventService eventService, int id) =>
        {
            await eventService.DeleteEventAsync(id);
            return Results.NoContent();
        }).RequireAuthorization(Policies.CanManageUsers);

        // GET /api/events/speakers/available
        group.MapGet("/speakers/available", async (IEventService eventService) =>
        {
            var speakers = await eventService.GetAvailableSpeakersAsync();
            return Results.Ok(speakers);
        }).RequireAuthorization(Policies.CanManageEvents);

        return group;
    }

    private static (bool IsValid, string? ErrorMessage) ValidateEvent(EventDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return (false, "Title is required.");

        if (string.IsNullOrWhiteSpace(dto.Slug))
            return (false, "Slug is required.");

        if (dto.IsPublished)
        {
            if (string.IsNullOrWhiteSpace(dto.Description))
                return (false, "Description is required when publishing.");

            if (dto.EventDateTime == default)
                return (false, "Event date/time is required when publishing.");

            if (string.IsNullOrWhiteSpace(dto.Location))
                return (false, "Location is required when publishing.");

            if (dto.SpeakerUserIds.Count == 0)
                return (false, "At least one speaker is required when publishing.");
        }

        return (true, null);
    }
}