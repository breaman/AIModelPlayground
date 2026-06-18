using UserGroupSiteOpus48.Shared.Authorization;
using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Server.Endpoints;

/// <summary>
/// Minimal API endpoints for events. Public read endpoints serve published events; editing requires
/// authorization (create = Admin; update = admin or assigned speaker, enforced inside the service).
/// </summary>
public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        // Public: published events, newest first.
        group.MapGet("/", (IEventService events) => events.GetPublishedEventsAsync())
            .AllowAnonymous();

        // Editors: all events including unpublished.
        group.MapGet("/all", (IEventService events) => events.GetAllEventsAsync())
            .RequireAuthorization(Policies.EventEditors);

        // Editors: users assignable as speakers.
        group.MapGet("/speakers", (IEventService events) => events.GetAssignableSpeakersAsync())
            .RequireAuthorization(Policies.EventEditors);

        // Editors: editable payload for a specific event.
        group.MapGet("/edit/{id:int}", async (int id, IEventService events) =>
        {
            var dto = await events.GetEventForEditAsync(id);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).RequireAuthorization(Policies.EventEditors);

        // Public: a single published event by slug.
        group.MapGet("/{slug}", async (string slug, IEventService events) =>
        {
            var dto = await events.GetEventBySlugAsync(slug);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).AllowAnonymous();

        // Admin only: create.
        group.MapPost("/", (EventEditDto dto, IEventService events) => events.CreateEventAsync(dto))
            .RequireAuthorization(Policies.AdminOnly);

        // Editors: update (service re-checks admin-or-assigned-speaker for the specific event).
        group.MapPut("/", (EventEditDto dto, IEventService events) => events.UpdateEventAsync(dto))
            .RequireAuthorization(Policies.EventEditors);

        return app;
    }
}