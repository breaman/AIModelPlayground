using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

using UserGroupSiteKimiK27Code.Shared;
using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Server.Api;

public static class EventApiEndpoints
{
    public static IEndpointRouteBuilder MapEventApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events").WithTags("Events");

        group.MapGet("/published", async (IEventService eventService) =>
        {
            var events = await eventService.GetPublishedEventsAsync();
            return Results.Ok(events);
        });

        group.MapGet("/{slug}", async (string slug, IEventService eventService, IEventAuthorizationService authService, ClaimsPrincipal user) =>
        {
            var includeUnpublished = authService.IsAdmin(user) || authService.IsSpeaker(user);
            var evt = await eventService.GetEventBySlugAsync(slug, includeUnpublished);
            return evt is null ? Results.NotFound() : Results.Ok(evt);
        });

        group.MapGet("/{id:int}/edit", async (int id, IEventService eventService) =>
        {
            var evt = await eventService.GetEventForEditAsync(id);
            return evt is null ? Results.NotFound() : Results.Ok(evt);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Roles.Admin},{Roles.Speaker}" });

        group.MapPost("/", async (EventEditDto dto, IEventService eventService) =>
        {
            var result = await eventService.CreateEventAsync(dto);
            return result.Success ? Results.Created($"/api/events/{result.EventId}", result) : Results.BadRequest(result);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

        group.MapPut("/{id:int}", async (int id, EventEditDto dto, IEventService eventService, IEventAuthorizationService authService, ClaimsPrincipal user) =>
        {
            if (!authService.IsAdmin(user) && !await authService.CanEditEventAsync(user, id))
            {
                return Results.Forbid();
            }

            var result = await eventService.UpdateEventAsync(id, dto);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Roles.Admin},{Roles.Speaker}" });

        app.MapGet("/api/speakers", async (IEventService eventService) =>
        {
            var speakers = await eventService.GetSpeakersAsync();
            return Results.Ok(speakers);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Roles.Admin},{Roles.Speaker}" })
        .WithTags("Speakers");

        return app;
    }
}