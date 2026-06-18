using UserGroupSiteQwen35.Data.Models;
using UserGroupSiteQwen35.Server.Services;

namespace UserGroupSiteQwen35.Server.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        // Public endpoints
        group.MapGet("/", async (EventService service) =>
            await service.GetPublishedEventsAsync());

        group.MapGet("/slug/{slug}", async (string slug, EventService service) =>
        {
            var evt = await service.GetEventBySlugAsync(slug);
            return evt is not null ? Results.Ok(evt) : Results.NotFound();
        });

        // Authenticated endpoints
        group.MapGet("/all", async (EventService service) =>
            await service.GetAllEventsAsync())
            .RequireAuthorization();

        group.MapGet("/{id:int}", async (int id, EventService service) =>
        {
            var evt = await service.GetEventByIdAsync(id);
            return evt is not null ? Results.Ok(evt) : Results.NotFound();
        }).RequireAuthorization();

        group.MapPost("/", async (Event evt, EventService service) =>
        {
            var created = await service.CreateEventAsync(evt);
            return Results.Created($"/api/events/{created.Id}", created);
        }).RequireAuthorization("SpeakerOrAdmin");

        group.MapPut("/{id:int}", async (int id, Event evt, EventService service) =>
        {
            if (id != evt.Id) return Results.BadRequest("ID mismatch");
            var updated = await service.UpdateEventAsync(evt);
            return Results.Ok(updated);
        }).RequireAuthorization("SpeakerOrAdmin");

        group.MapDelete("/{id:int}", async (int id, EventService service) =>
        {
            await service.DeleteEventAsync(id);
            return Results.NoContent();
        }).RequireAuthorization(RoleNames.Admin);

        group.MapGet("/speakers", async (EventService service) =>
            await service.GetApprovedSpeakersAsync())
            .RequireAuthorization("SpeakerOrAdmin");

        group.MapGet("/slug/{slug}/check", async (string slug, int? excludeId, EventService service) =>
        {
            var exists = await service.IsSlugUniqueAsync(slug, excludeId);
            return Results.Ok(!exists); // Returns true if available (not exists)
        }).RequireAuthorization("SpeakerOrAdmin");
    }
}