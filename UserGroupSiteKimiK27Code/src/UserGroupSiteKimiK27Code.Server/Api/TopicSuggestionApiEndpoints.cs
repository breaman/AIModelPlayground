using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Server.Api;

public static class TopicSuggestionApiEndpoints
{
    public static IEndpointRouteBuilder MapTopicSuggestionApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics").WithTags("Topic Suggestions").RequireAuthorization();

        group.MapGet("/", async (ITopicSuggestionService service) =>
        {
            var suggestions = await service.GetSuggestionsAsync();
            return Results.Ok(suggestions);
        });

        group.MapPost("/", async (CreateTopicSuggestionRequest request, ITopicSuggestionService service) =>
        {
            var suggestion = await service.CreateSuggestionAsync(request);
            return suggestion is null ? Results.BadRequest() : Results.Ok(suggestion);
        });

        group.MapPost("/{id:int}/vote", async (int id, ITopicSuggestionService service) =>
        {
            var result = await service.VoteAsync(id);
            return result is null ? Results.BadRequest() : Results.Ok(result);
        });

        group.MapPost("/{id:int}/volunteer", async (int id, ITopicSuggestionService service) =>
        {
            var suggestion = await service.VolunteerAsync(id);
            return suggestion is null ? Results.BadRequest() : Results.Ok(suggestion);
        });

        return app;
    }
}