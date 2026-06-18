using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Server.Endpoints;

/// <summary>Topic suggestion API: list, suggest, vote, and volunteer (any authenticated user).</summary>
public static class TopicEndpoints
{
    public static void MapTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics").RequireAuthorization();

        group.MapGet("/", async (ITopicService topics) => Results.Ok(await topics.GetTopicsAsync()));

        group.MapPost("/", async (TopicCreateDto dto, ITopicService topics) =>
            (await topics.CreateTopicAsync(dto)).ToHttpResult());

        group.MapPost("/{id:int}/vote", async (int id, ITopicService topics) =>
            (await topics.VoteAsync(id)).ToHttpResult());

        group.MapPost("/{id:int}/volunteer", async (int id, ITopicService topics) =>
            (await topics.VolunteerAsync(id)).ToHttpResult());
    }
}