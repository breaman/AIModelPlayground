using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

namespace UserGroupSiteOpus5.Server.Endpoints;

/// <summary>
/// HTTP surface backing <see cref="IEventService"/> for the WebAssembly client.
/// </summary>
/// <remarks>
/// Endpoints are deliberately thin: authorize, delegate to the server service, map the result.
/// The service re-checks authorization itself, so a route-level policy is a first filter rather
/// than the only one.
/// </remarks>
public static class EventEndpoints
{
    /// <summary>Maps the event API onto <paramref name="app"/>.</summary>
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        group.MapGet("/", async (IEventService events) =>
                Results.Ok(await events.GetPublishedEventsAsync()))
            .AllowAnonymous()
            .WithName("GetPublishedEvents")
            .WithSummary("Lists published events, most recent first.");

        group.MapGet("/manage", async (IEventService events) =>
                Results.Ok(await events.GetManageableEventsAsync()))
            .RequireAuthorization(PolicyNames.SpeakerOrAdmin)
            .WithName("GetManageableEvents")
            .WithSummary("Lists the events the caller may manage.");

        group.MapGet("/slug", async (string title, int? excludeEventId, IEventService events) =>
                Results.Ok(await events.GenerateUniqueSlugAsync(title, excludeEventId)))
            .RequireAuthorization(PolicyNames.SpeakerOrAdmin)
            .WithName("GenerateEventSlug")
            .WithSummary("Suggests a slug for a title that no other event is using.");

        group.MapGet("/{id:int}/edit", async (int id, IEventService events) =>
                await events.GetEventForEditAsync(id) is { } model
                    ? Results.Ok(model)
                    : Results.NotFound())
            .RequireAuthorization(PolicyNames.SpeakerOrAdmin)
            .WithName("GetEventForEdit")
            .WithSummary("Loads an event for editing, if the caller may edit it.");

        group.MapGet("/{id:int}/can-edit", async (int id, IEventService events) =>
                Results.Ok(await events.CanEditEventAsync(id)))
            .RequireAuthorization()
            .WithName("CanEditEvent")
            .WithSummary("Reports whether the caller may edit the given event.");

        // Registered after the more specific routes so a slug never shadows "manage" or "slug".
        group.MapGet("/{slug}", async (string slug, IEventService events) =>
                await events.GetEventBySlugAsync(slug) is { } detail
                    ? Results.Ok(detail)
                    : Results.NotFound())
            .AllowAnonymous()
            .WithName("GetEventBySlug")
            .WithSummary("Loads a single event by slug.");

        var mutations = app.MapGroup("/api/events")
            .RequireAuthorization(PolicyNames.SpeakerOrAdmin)
            .AddEndpointFilter<RequireClientHeaderFilter>();

        mutations.MapPost("/", async (EventEditModel model, IEventService events) =>
            {
                // The service enforces that only an administrator may create; the route policy is
                // the wider gate that also admits speakers for the update below.
                model.Id = 0;
                return (await events.SaveEventAsync(model)).ToHttpResult();
            })
            .WithName("CreateEvent")
            .WithSummary("Creates an event. Administrators only.");

        mutations.MapPut("/{id:int}", async (int id, EventEditModel model, IEventService events) =>
            {
                model.Id = id;
                return (await events.SaveEventAsync(model)).ToHttpResult();
            })
            .WithName("UpdateEvent")
            .WithSummary("Updates an event the caller may edit.");

        app.MapGet("/api/speakers", async (IEventService events) =>
                Results.Ok(await events.GetAvailableSpeakersAsync()))
            .RequireAuthorization(PolicyNames.SpeakerOrAdmin)
            .WithName("GetAvailableSpeakers")
            .WithSummary("Lists the members holding the Speaker role.");

        return app;
    }
}
