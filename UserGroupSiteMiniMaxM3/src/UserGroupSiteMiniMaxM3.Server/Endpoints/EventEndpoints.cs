using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Shared.Models.Events;

namespace UserGroupSiteMiniMaxM3.Server.Endpoints;

/// <summary>
/// Minimal API endpoints for event management. Most pages still render server-side
/// via Blazor components; these endpoints exist for the WebAssembly client
/// (the event editor) to load the available speakers, fetch a single event by
/// slug, and submit a create/update.
/// </summary>
internal static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/events");

        // Public listing endpoint for the home / events list page (server-rendered
        // components call the service directly; this is for any future WASM callers).
        group.MapGet("", async (IEventService svc, CancellationToken ct) =>
            TypedResults.Ok(await svc.ListPublishedAsync(ct)));

        // Fetch a single event by slug. Unpublished events only visible to editors.
        group.MapGet("by-slug/{slug}", async (string slug, ClaimsPrincipal user, IEventService svc, CancellationToken ct) =>
        {
            var ev = await svc.GetBySlugAsync(slug, user);
            return ev is null ? Results.NotFound() : TypedResults.Ok(ev);
        });

        // Manage list — visible to any signed-in user; the service filters by role.
        group.MapGet("manage", async (ClaimsPrincipal user, IEventService svc, CancellationToken ct) =>
        {
            if (user.Identity?.IsAuthenticated != true) return Results.Unauthorized();
            return TypedResults.Ok(await svc.ListForUserAsync(user, ct));
        }).RequireAuthorization();

        // Create a new event. Signed-in users only.
        group.MapPost("", async (
            [FromBody] EventEditDto input,
            ClaimsPrincipal user,
            IEventService svc,
            CancellationToken ct) =>
        {
            if (user.Identity?.IsAuthenticated != true) return Results.Unauthorized();
            try
            {
                var id = await svc.CreateAsync(input, user, ct);
                return Results.Created($"/api/events/{id}", new { id });
            }
            catch (EventValidationException ex)
            {
                return Results.ValidationProblem(ToRfc9457(ex.Result.Errors));
            }
        }).RequireAuthorization();

        // Update an existing event. Editor (admin or assigned speaker) only.
        group.MapPut("{id:int}", async (
            int id,
            [FromBody] EventEditDto input,
            ClaimsPrincipal user,
            IEventService svc,
            CancellationToken ct) =>
        {
            try
            {
                await svc.UpdateAsync(id, input, user, ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
            }
            catch (EventValidationException ex)
            {
                return Results.ValidationProblem(ToRfc9457(ex.Result.Errors));
            }
        }).RequireAuthorization();

        return endpoints;
    }

    /// <summary>Lightweight user-projection endpoint for the speaker picker.</summary>
    public static IEndpointRouteBuilder MapUserLookupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users").RequireAuthorization();

        // Returns the full membership so editors can pick any user as a speaker.
        // (Speaker assignment is per-event, not gated by the Speaker role.)
        group.MapGet("speakers", async (IUserLookupService svc, CancellationToken ct) =>
            TypedResults.Ok(await svc.GetSpeakerOptionsAsync(ct)));

        return endpoints;
    }

    private static Dictionary<string, string[]> ToRfc9457(Dictionary<string, string[]> source)
    {
        // ASP.NET Core's Results.ValidationProblem expects the model-state shape
        // (key is field name, value is array of messages). We pass through directly.
        return new Dictionary<string, string[]>(source, StringComparer.OrdinalIgnoreCase);
    }
}