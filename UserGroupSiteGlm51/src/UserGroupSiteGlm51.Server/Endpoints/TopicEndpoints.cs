using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;
using UserGroupSiteGlm51.Shared.Models;

namespace UserGroupSiteGlm51.Server.Endpoints;

/// <summary>
/// Minimal API endpoints for topic suggestion management.
/// </summary>
public static class TopicEndpoints
{
    public static void MapTopicEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/topics").RequireAuthorization();

        // Get all topic suggestions
        group.MapGet("/", async (ITopicSuggestionService topicService, IUserService userService) =>
        {
            var topics = await topicService.GetAllTopicsAsync();
            var currentUserId = userService.UserId;
            var dtos = topics.Select(t => MapToDto(t, currentUserId, t.Votes.Any(v => v.UserId == currentUserId)));
            return Results.Ok(dtos);
        });

        // Create a new topic suggestion
        group.MapPost("/", async (CreateTopicRequest request, ITopicSuggestionService topicService, IUserService userService) =>
        {
            var topic = new TopicSuggestion
            {
                Title = request.Title,
                Description = request.Description,
                SuggestedById = userService.UserId
            };

            var created = await topicService.CreateTopicAsync(topic);
            return Results.Created($"/api/topics/{created.Id}", MapToDto(created, userService.UserId, false));
        });

        // Vote on a topic
        group.MapPost("/{id}/vote", async (int id, ITopicSuggestionService topicService, IUserService userService) =>
        {
            await topicService.VoteAsync(id, userService.UserId);
            return Results.Ok();
        });

        // Remove vote from a topic
        group.MapDelete("/{id}/vote", async (int id, ITopicSuggestionService topicService, IUserService userService) =>
        {
            await topicService.UnvoteAsync(id, userService.UserId);
            return Results.NoContent();
        });

        // Volunteer to present a topic
        group.MapPost("/{id}/volunteer", async (int id, ITopicSuggestionService topicService, IUserService userService) =>
        {
            await topicService.VolunteerAsync(id, userService.UserId);
            return Results.Ok();
        });

        // Unvolunteer from a topic
        group.MapDelete("/{id}/volunteer", async (int id, ITopicSuggestionService topicService, IUserService userService) =>
        {
            await topicService.UnvolunteerAsync(id, userService.UserId);
            return Results.NoContent();
        });
    }

    private static TopicSuggestionDto MapToDto(TopicSuggestion t, int currentUserId, bool hasVoted) => new(
        t.Id,
        t.Title,
        t.Description,
        t.SuggestedById,
        t.SuggestedBy?.FirstName ?? t.SuggestedBy?.UserName,
        t.VolunteerId,
        t.Volunteer?.FirstName ?? t.Volunteer?.UserName,
        t.Votes?.Count ?? 0,
        hasVoted,
        t.VolunteerId == currentUserId
    );
}