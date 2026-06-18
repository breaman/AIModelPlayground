using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using UserGroupSiteFable5.Data.Models;
using UserGroupSiteFable5.Server.Services;
using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Server.Endpoints;

/// <summary>
/// Event API. The public list is anonymous; edit operations use resource-based
/// authorization (Admin or assigned speaker) inside <see cref="IEventService"/>.
/// </summary>
public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        group.MapGet("/published", GetPublishedEventsAsync).AllowAnonymous();

        group.MapGet("/", async (IEventService events) => Results.Ok(await events.GetEditableEventsAsync()))
            .RequireAuthorization("AdminOrSpeaker");

        group.MapGet("/{id:int}", GetEventForEditAsync).RequireAuthorization("AdminOrSpeaker");

        group.MapPost("/", async (EventEditDto dto, IEventService events) =>
                (await events.CreateEventAsync(dto)).ToHttpResult())
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{id:int}", async (int id, EventEditDto dto, IEventService events) =>
            {
                dto.Id = id;

                return (await events.UpdateEventAsync(dto)).ToHttpResult();
            })
            .RequireAuthorization("AdminOrSpeaker");

        app.MapGet("/api/speakers", async (IEventService events) => Results.Ok(await events.GetSpeakersAsync()))
            .RequireAuthorization("AdminOrSpeaker");
    }

    private static async Task<IResult> GetPublishedEventsAsync(ApplicationDbContext dbContext)
    {
        var events = await dbContext.Events
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.StartsAt)
            .Select(e => new EventDetailDto
            {
                Id = e.Id,
                Title = e.Title,
                Slug = e.Slug,
                ShortDescription = e.ShortDescription,
                Description = e.Description,
                StartsAt = e.StartsAt,
                Location = e.Location,
                // Inline expression (not a helper method) so EF can translate it to SQL
                // inside this nested collection projection.
                SpeakerNames = e.Speakers
                    .Select(s => s.User.FirstName == null && s.User.LastName == null
                        ? s.User.Email ?? ""
                        : ((s.User.FirstName ?? "") + " " + (s.User.LastName ?? "")).Trim())
                    .ToList()
            })
            .ToListAsync();

        return Results.Ok(events);
    }

    private static async Task<IResult> GetEventForEditAsync(int id, ClaimsPrincipal user,
        IEventService events, IEventAuthorizationService eventAuthorization)
    {
        // Distinguish 403 from 404 here; the service alone returns null for both.
        if (!await eventAuthorization.CanEditEventAsync(user, id))
        {
            return Results.Forbid();
        }

        var dto = await events.GetEventForEditAsync(id);

        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }
}