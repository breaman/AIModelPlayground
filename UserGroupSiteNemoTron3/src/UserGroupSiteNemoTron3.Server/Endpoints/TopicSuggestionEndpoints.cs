using UserGroupSiteNemoTron3.Data.Interfaces;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

using Microsoft.AspNetCore.Authorization;

namespace UserGroupSiteNemoTron3.Server.Endpoints;

public static class TopicSuggestionEndpoints
{
    public static void MapTopicSuggestionEndpoints(this WebApplication app)
    {
        var topicsGroup = app.MapGroup("/api/topics").WithTags("Topics").RequireAuthorization();

        topicsGroup.MapGet("/", async (ITopicSuggestionService topicService) =>
        {
            var topics = await topicService.GetAllAsync();
            return Results.Ok(topics);
        });

        topicsGroup.MapPost("/", async (CreateTopicSuggestionDto dto, ITopicSuggestionService topicService, IUserService userService) =>
        {
            var topic = await topicService.CreateAsync(dto, userService.UserId);
            return Results.Created($"/api/topics/{topic.Id}", topic);
        });

        topicsGroup.MapPost("/{id:int}/vote", async (int id, ITopicSuggestionService topicService, IUserService userService) =>
        {
            await topicService.VoteAsync(id, userService.UserId);
            var topic = await topicService.GetByIdAsync(id);
            return Results.Ok(topic);
        });

        topicsGroup.MapDelete("/{id:int}/vote", async (int id, ITopicSuggestionService topicService, IUserService userService) =>
        {
            await topicService.RemoveVoteAsync(id, userService.UserId);
            var topic = await topicService.GetByIdAsync(id);
            return Results.Ok(topic);
        });

        topicsGroup.MapPost("/{id:int}/volunteer", async (int id, ITopicSuggestionService topicService, IUserService userService) =>
        {
            await topicService.VolunteerAsync(id, userService.UserId);
            var topic = await topicService.GetByIdAsync(id);
            return Results.Ok(topic);
        });
    }
}