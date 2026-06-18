using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Server.Endpoints;

public static class TopicEndpoints
{
    public static RouteGroupBuilder MapTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics").RequireAuthorization();

        // GET /api/topics - Authenticated users
        group.MapGet("/", async (ITopicSuggestionService topicService) =>
        {
            var topics = await topicService.GetAllAsync();
            return Results.Ok(topics);
        });

        // POST /api/topics - Create a new topic suggestion
        group.MapPost("/", async (ITopicSuggestionService topicService, TopicSuggestionDto dto) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return Results.BadRequest("Title is required.");

            var created = await topicService.CreateAsync(dto);
            return Results.Created($"/api/topics/{created.Id}", created);
        });

        // POST /api/topics/{id}/vote - Toggle vote (add)
        group.MapPost("/{id:int}/vote", async (ITopicSuggestionService topicService, int id) =>
        {
            await topicService.VoteAsync(id);
            return Results.Ok();
        });

        // DELETE /api/topics/{id}/vote - Toggle vote (remove)
        group.MapDelete("/{id:int}/vote", async (ITopicSuggestionService topicService, int id) =>
        {
            await topicService.RemoveVoteAsync(id);
            return Results.Ok();
        });

        // POST /api/topics/{id}/volunteer - Volunteer to speak
        group.MapPost("/{id:int}/volunteer", async (ITopicSuggestionService topicService, int id) =>
        {
            try
            {
                await topicService.VolunteerAsync(id);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        // DELETE /api/topics/{id}/volunteer - Remove volunteer
        group.MapDelete("/{id:int}/volunteer", async (ITopicSuggestionService topicService, int id) =>
        {
            await topicService.RemoveVolunteerAsync(id);
            return Results.Ok();
        });

        return group;
    }
}