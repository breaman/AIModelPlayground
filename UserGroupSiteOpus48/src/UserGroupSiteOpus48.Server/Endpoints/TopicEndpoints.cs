using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Server.Endpoints;

/// <summary>
/// Minimal API endpoints for topic suggestions. All actions require an authenticated user; the
/// vote/volunteer rules are enforced inside the service.
/// </summary>
public static class TopicEndpoints
{
    public static IEndpointRouteBuilder MapTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics").RequireAuthorization();

        group.MapGet("/", (ITopicService topics) => topics.GetTopicsAsync());

        group.MapPost("/", (TopicCreateDto dto, ITopicService topics) => topics.SuggestTopicAsync(dto));

        group.MapPost("/{id:int}/vote", (int id, ITopicService topics) => topics.VoteAsync(id));

        group.MapPost("/{id:int}/volunteer", (int id, ITopicService topics) => topics.VolunteerAsync(id));

        return app;
    }
}