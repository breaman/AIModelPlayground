using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Shared.Models.Topics;
using UserGroupSiteMiniMaxM3.Shared.Services;

namespace UserGroupSiteMiniMaxM3.Server.Endpoints;

/// <summary>
/// Minimal API endpoints for topic suggestions. Topics are visible to any
/// signed-in user (the Topics page is auth-gated). All operations resolve
/// the current user from the auth cookie on the server side.
/// </summary>
internal static class TopicEndpoints
{
    public static IEndpointRouteBuilder MapTopicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/topics").RequireAuthorization();

        // List all topics with vote/volunteer counts and the requester's state.
        group.MapGet("", async (ClaimsPrincipal user, ITopicService svc, CancellationToken ct) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return TypedResults.Ok(await svc.ListAsync(currentUserId, ct));
        });

        // Create a new topic. Signed-in users only.
        group.MapPost("", async (
            [FromBody] TopicCreateDto input,
            ClaimsPrincipal user,
            ITopicService svc,
            CancellationToken ct) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId is null) return Results.Unauthorized();
            try
            {
                var created = await svc.CreateAsync(input, currentUserId, ct);
                return Results.Created($"/api/topics/{created.Id}", created);
            }
            catch (TopicValidationException ex)
            {
                return Results.ValidationProblem(ex.Errors);
            }
        });

        // Toggle the requester's vote on a topic.
        group.MapPost("{id:int}/vote", async (
            int id,
            ClaimsPrincipal user,
            ITopicService svc,
            CancellationToken ct) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId is null) return Results.Unauthorized();
            try
            {
                var isOn = await svc.ToggleVoteAsync(id, currentUserId, ct);
                return TypedResults.Ok(new { isOn });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });

        // Toggle the requester's volunteer offer.
        group.MapPost("{id:int}/volunteer", async (
            int id,
            ClaimsPrincipal user,
            ITopicService svc,
            CancellationToken ct) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId is null) return Results.Unauthorized();
            try
            {
                var isOn = await svc.ToggleVolunteerAsync(id, currentUserId, ct);
                return TypedResults.Ok(new { isOn });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });

        // Delete a topic. Suggester or admin only.
        group.MapDelete("{id:int}", async (
            int id,
            ClaimsPrincipal user,
            ITopicService svc,
            CancellationToken ct) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId is null) return Results.Unauthorized();
            var isAdmin = user.IsInRole(Roles.Admin);
            try
            {
                await svc.DeleteAsync(id, currentUserId, isAdmin, ct);
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
        });

        return endpoints;
    }
}