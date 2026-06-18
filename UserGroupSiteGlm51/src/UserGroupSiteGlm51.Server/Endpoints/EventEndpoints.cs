using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;
using UserGroupSiteGlm51.Shared.Models;

namespace UserGroupSiteGlm51.Server.Endpoints;

/// <summary>
/// Minimal API endpoints for event management.
/// </summary>
public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/events");

        // Public: get all published events
        group.MapGet("/", async (IEventService eventService) =>
        {
            var events = await eventService.GetPublishedEventsAsync();
            var dtos = events.Select(MapToDto);
            return Results.Ok(dtos);
        });

        // Public: get event by slug
        group.MapGet("/{slug}", async (string slug, IEventService eventService) =>
        {
            var eventEntity = await eventService.GetEventBySlugAsync(slug);
            return eventEntity is null ? Results.NotFound() : Results.Ok(MapToDto(eventEntity));
        });

        // Admin: create event
        group.MapPost("/", async (CreateEventRequest request, IEventService eventService, IUserService userService) =>
        {
            var eventEntity = new Event
            {
                Title = request.Title,
                Slug = request.Slug ?? string.Empty,
                ShortDescription = request.ShortDescription,
                Description = request.Description,
                EventDate = request.EventDate,
                Location = request.Location,
                IsPublished = request.IsPublished
            };

            // Validate publish requirements if publishing
            if (eventEntity.IsPublished && !await eventService.CanPublishAsync(eventEntity))
            {
                return Results.BadRequest("Published events must have a title, slug, description, date, location, and at least one speaker.");
            }

            var created = await eventService.CreateEventAsync(eventEntity);

            // Assign speakers
            foreach (var speakerId in request.SpeakerIds)
            {
                await eventService.AddSpeakerToEventAsync(created.Id, speakerId);
            }

            // Re-fetch to include speakers
            created = await eventService.GetEventByIdAsync(created.Id);
            return Results.Created($"/api/events/{created!.Slug}", MapToDto(created));
        }).RequireAuthorization("Admin");

        // Admin or assigned speaker: update event
        group.MapPut("/{id}", async (int id, UpdateEventRequest request, IEventService eventService, IUserService userService) =>
        {
            var eventEntity = await eventService.GetEventByIdAsync(id);
            if (eventEntity is null)
            {
                return Results.NotFound();
            }

            // Check authorization: admin or assigned speaker
            if (!await eventService.IsEditorAsync(id, userService.UserId))
            {
                return Results.Forbid();
            }

            eventEntity.Title = request.Title;
            eventEntity.Slug = request.Slug;
            eventEntity.ShortDescription = request.ShortDescription;
            eventEntity.Description = request.Description;
            eventEntity.EventDate = request.EventDate;
            eventEntity.Location = request.Location;
            eventEntity.IsPublished = request.IsPublished;

            // Validate publish requirements if publishing
            if (eventEntity.IsPublished && !await eventService.CanPublishAsync(eventEntity))
            {
                return Results.BadRequest("Published events must have a title, slug, description, date, location, and at least one speaker.");
            }

            var updated = await eventService.UpdateEventAsync(eventEntity);

            // Re-assign speakers
            foreach (var speakerId in request.SpeakerIds)
            {
                await eventService.AddSpeakerToEventAsync(updated.Id, speakerId);
            }

            // Re-fetch to include updated speakers
            updated = await eventService.GetEventByIdAsync(updated.Id);
            return Results.Ok(MapToDto(updated!));
        }).RequireAuthorization();

        // Admin: delete event
        group.MapDelete("/{id}", async (int id, IEventService eventService) =>
        {
            await eventService.DeleteEventAsync(id);
            return Results.NoContent();
        }).RequireAuthorization("Admin");

        // Admin: add speaker to event
        group.MapPost("/{id}/speakers", async (int id, SpeakerRequest request, IEventService eventService) =>
        {
            await eventService.AddSpeakerToEventAsync(id, request.UserId);
            return Results.Ok();
        }).RequireAuthorization("Admin");

        // Admin: remove speaker from event
        group.MapDelete("/{id}/speakers/{userId}", async (int id, int userId, IEventService eventService) =>
        {
            await eventService.RemoveSpeakerFromEventAsync(id, userId);
            return Results.NoContent();
        }).RequireAuthorization("Admin");
    }

    private static EventDto MapToDto(Event e) => new(
        e.Id,
        e.Title,
        e.Slug,
        e.ShortDescription,
        e.Description,
        e.EventDate,
        e.Location,
        e.IsPublished,
        e.EventSpeakers.Select(es => new EventSpeakerDto(
            es.UserId,
            es.User.FirstName,
            es.User.LastName,
            es.User.Email
        )).ToList()
    );
}

/// <summary>
/// Request body for adding/removing a speaker from an event.
/// </summary>
public record SpeakerRequest(int UserId);