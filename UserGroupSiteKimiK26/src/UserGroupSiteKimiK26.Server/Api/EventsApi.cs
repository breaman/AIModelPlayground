using System.Security.Claims;

using UserGroupSiteKimiK26.Data.Models;
using UserGroupSiteKimiK26.Shared.Dtos;
using UserGroupSiteKimiK26.Shared.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteKimiK26.Server.Api;

public static class EventsApi
{
    public static IEndpointRouteBuilder MapEventsApi(this IEndpointRouteBuilder endpoints)
    {
        // Public endpoints
        endpoints.MapGet("/api/events", async (IEventsService eventsService) =>
        {
            var events = await eventsService.GetPublishedEventsAsync();
            return Results.Ok(events);
        });

        endpoints.MapGet("/api/events/{slug}", async (string slug, IEventsService eventsService) =>
        {
            var evt = await eventsService.GetEventBySlugAsync(slug);
            return evt is null ? Results.NotFound() : Results.Ok(evt);
        });

        // Authenticated manage endpoints
        endpoints.MapGet("/api/events/manage", [Authorize] async (IEventsService eventsService) =>
        {
            var events = await eventsService.GetEditableEventsForUserAsync();
            return Results.Ok(events);
        });

        endpoints.MapGet("/api/events/manage/{id:int}", [Authorize] async (int id, IEventsService eventsService, IAuthorizationService authService, ClaimsPrincipal user) =>
        {
            var evt = await eventsService.GetEventByIdAsync(id);
            if (evt is null) return Results.NotFound();

            var authResult = await authService.AuthorizeAsync(user, id, "CanEditEvent");
            if (!authResult.Succeeded) return Results.Forbid();

            return Results.Ok(evt);
        });

        // Admin-only create and delete
        endpoints.MapPost("/api/events", [Authorize(Roles = "Admin")] async (EventDto dto, IEventsService eventsService) =>
        {
            var (success, errors) = await eventsService.CreateEventAsync(dto);
            if (!success) return Results.BadRequest(errors);
            return Results.Ok();
        });

        endpoints.MapPut("/api/events/{id:int}", [Authorize] async (int id, EventDto dto, IEventsService eventsService, IAuthorizationService authService, ClaimsPrincipal user) =>
        {
            var authResult = await authService.AuthorizeAsync(user, id, "CanEditEvent");
            if (!authResult.Succeeded) return Results.Forbid();

            var (success, errors) = await eventsService.UpdateEventAsync(id, dto);
            if (!success) return Results.BadRequest(errors);
            return Results.NoContent();
        });

        endpoints.MapDelete("/api/events/{id:int}", [Authorize(Roles = "Admin")] async (int id, IEventsService eventsService) =>
        {
            var deleted = await eventsService.DeleteEventAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        // Markdown preview endpoint
        endpoints.MapPost("/api/markdown/preview", (string markdown) =>
        {
            var html = Markdig.Markdown.ToHtml(markdown ?? "");
            return Results.Text(html, "text/html");
        });

        // Get speakers list for dropdown
        endpoints.MapGet("/api/speakers", async (UserManager<User> userManager) =>
        {
            var speakers = await userManager.GetUsersInRoleAsync("Speaker");
            return Results.Ok(speakers.Select(u => new SpeakerDto
            {
                Id = u.Id,
                FirstName = u.FirstName ?? "",
                LastName = u.LastName ?? "",
                Email = u.Email
            }).ToList());
        });

        return endpoints;
    }
}
