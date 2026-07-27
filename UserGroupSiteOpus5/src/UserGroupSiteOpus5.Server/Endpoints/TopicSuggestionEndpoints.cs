using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

namespace UserGroupSiteOpus5.Server.Endpoints;

/// <summary>
/// HTTP surface backing <see cref="ITopicSuggestionService"/> for the WebAssembly client.
/// </summary>
public static class TopicSuggestionEndpoints
{
    /// <summary>Maps the topic suggestion API onto <paramref name="app"/>.</summary>
    public static IEndpointRouteBuilder MapTopicSuggestionEndpoints(this IEndpointRouteBuilder app)
    {
        // Every topic operation requires a signed-in member: suggestions, votes, and volunteering
        // are all attributed to a person.
        var group = app.MapGroup("/api/topics")
            .RequireAuthorization();

        group.MapGet("/", async (ITopicSuggestionService topics) =>
                Results.Ok(await topics.GetSuggestionsAsync()))
            .WithName("GetTopicSuggestions")
            .WithSummary("Lists topic suggestions, most-voted first.");

        var mutations = app.MapGroup("/api/topics")
            .RequireAuthorization()
            .AddEndpointFilter<RequireClientHeaderFilter>();

        mutations.MapPost("/", async (TopicSuggestionCreateModel model, ITopicSuggestionService topics) =>
                (await topics.CreateSuggestionAsync(model)).ToHttpResult())
            .WithName("CreateTopicSuggestion")
            .WithSummary("Suggests a new topic.");

        mutations.MapPost("/{id:int}/vote", async (int id, ITopicSuggestionService topics) =>
                (await topics.VoteAsync(id)).ToHttpResult())
            .WithName("VoteForTopicSuggestion")
            .WithSummary("Casts the caller's vote for a suggestion.");

        mutations.MapPost("/{id:int}/volunteer", async (int id, ITopicSuggestionService topics) =>
                (await topics.VolunteerAsync(id)).ToHttpResult())
            .WithName("VolunteerForTopicSuggestion")
            .WithSummary("Claims the presenter slot for a suggestion.");

        return app;
    }
}
