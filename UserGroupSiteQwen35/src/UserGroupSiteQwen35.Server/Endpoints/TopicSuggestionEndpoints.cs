using UserGroupSiteQwen35.Data.Models;
using UserGroupSiteQwen35.Server.Services;

namespace UserGroupSiteQwen35.Server.Endpoints;

public static class TopicSuggestionEndpoints
{
    public static void MapTopicSuggestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics");

        group.MapGet("/", async (TopicSuggestionService service) =>
            await service.GetAllWithVoteCountsAsync())
            .RequireAuthorization();

        group.MapGet("/{id:int}", async (int id, TopicSuggestionService service) =>
        {
            var topic = await service.GetByIdAsync(id);
            return topic is not null ? Results.Ok(topic) : Results.NotFound();
        }).RequireAuthorization();

        group.MapPost("/", async (
            TopicSuggestion suggestion,
            HttpContext httpContext,
            TopicSuggestionService service) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            suggestion.SuggestedByUserId = int.Parse(userId);
            suggestion.CreatedBy = int.Parse(userId);
            var created = await service.CreateSuggestionAsync(suggestion);
            return Results.Created($"/api/topics/{created.Id}", created);
        }).RequireAuthorization();

        group.MapPost("/{id:int}/vote", async (
            int id,
            HttpContext httpContext,
            TopicSuggestionService service) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var success = await service.VoteAsync(id, int.Parse(userId));
            return success ? Results.Ok() : Results.BadRequest("Already voted");
        }).RequireAuthorization();

        group.MapDelete("/{id:int}/vote", async (
            int id,
            HttpContext httpContext,
            TopicSuggestionService service) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var success = await service.RemoveVoteAsync(id, int.Parse(userId));
            return success ? Results.Ok() : Results.BadRequest("No vote found");
        }).RequireAuthorization();

        group.MapGet("/{id:int}/voted", async (
            int id,
            HttpContext httpContext,
            TopicSuggestionService service) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var hasVoted = await service.HasVotedAsync(id, int.Parse(userId));
            return Results.Ok(hasVoted);
        }).RequireAuthorization();

        group.MapPost("/{id:int}/volunteer", async (
            int id,
            HttpContext httpContext,
            TopicSuggestionService service) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var success = await service.VolunteerAsync(id, int.Parse(userId));
            return success ? Results.Ok() : Results.BadRequest("Already has volunteer");
        }).RequireAuthorization();

        group.MapDelete("/{id:int}/volunteer", async (
            int id,
            HttpContext httpContext,
            TopicSuggestionService service) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var success = await service.RemoveVolunteerAsync(id, int.Parse(userId));
            return success ? Results.Ok() : Results.BadRequest("Not a volunteer");
        }).RequireAuthorization();

        group.MapGet("/{id:int}/volunteer", async (
            int id,
            HttpContext httpContext,
            TopicSuggestionService service) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var isVolunteer = await service.IsUserVolunteerAsync(id, int.Parse(userId));
            return Results.Ok(isVolunteer);
        }).RequireAuthorization();
    }
}