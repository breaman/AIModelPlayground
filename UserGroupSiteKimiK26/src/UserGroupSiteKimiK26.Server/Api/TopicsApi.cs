using UserGroupSiteKimiK26.Shared.Dtos;
using UserGroupSiteKimiK26.Shared.Services;

using Microsoft.AspNetCore.Authorization;

namespace UserGroupSiteKimiK26.Server.Api;

public static class TopicsApi
{
    public static IEndpointRouteBuilder MapTopicsApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/topics");

        group.MapGet("/", [Authorize] async (ITopicSuggestionsService topicService) =>
        {
            var suggestions = await topicService.GetAllSuggestionsAsync();
            return Results.Ok(suggestions);
        });

        group.MapPost("/", [Authorize] async (CreateSuggestionDto dto, ITopicSuggestionsService topicService) =>
        {
            try
            {
                var suggestion = await topicService.CreateSuggestionAsync(dto);
                return Results.Ok(suggestion);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/{id:int}/vote", [Authorize] async (int id, ITopicSuggestionsService topicService) =>
        {
            try
            {
                var result = await topicService.VoteAsync(id);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/{id:int}/volunteer", [Authorize] async (int id, ITopicSuggestionsService topicService) =>
        {
            try
            {
                var result = await topicService.VolunteerAsync(id);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapDelete("/{id:int}", [Authorize] async (int id, ITopicSuggestionsService topicService) =>
        {
            var deleted = await topicService.DeleteSuggestionAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return endpoints;
    }
}
