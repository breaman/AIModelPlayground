using Microsoft.AspNetCore.Http.HttpResults;

using UserGroupSiteGpt55.Shared.Authorization;
using UserGroupSiteGpt55.Shared.Events;
using UserGroupSiteGpt55.Shared.Markdown;
using UserGroupSiteGpt55.Shared.Topics;
using UserGroupSiteGpt55.Shared.Users;

namespace UserGroupSiteGpt55.Server.Endpoints;

/// <summary>
/// Maps user group feature APIs used by the WebAssembly client.
/// </summary>
public static class UserGroupEndpoints
{
    /// <summary>
    /// Adds event, user admin, markdown, and topic suggestion endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapUserGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/events/published", (IEventService service, CancellationToken cancellationToken) =>
            service.GetPublishedEventsAsync(cancellationToken));

        api.MapGet("/events/editable", (IEventService service, CancellationToken cancellationToken) =>
            service.GetEditableEventsAsync(cancellationToken))
            .RequireAuthorization();

        api.MapGet("/events/by-slug/{slug}", async Task<Results<Ok<EventDetail>, NotFound>> (
            string slug,
            IEventService service,
            CancellationToken cancellationToken) =>
        {
            var detail = await service.GetEventBySlugAsync(slug, cancellationToken);
            return detail is null ? TypedResults.NotFound() : TypedResults.Ok(detail);
        });

        api.MapGet("/events/{eventId:int}/edit", async Task<Results<Ok<EventEditModel>, NotFound>> (
            int eventId,
            IEventService service,
            CancellationToken cancellationToken) =>
        {
            var model = await service.GetEventForEditAsync(eventId, cancellationToken);
            return model is null ? TypedResults.NotFound() : TypedResults.Ok(model);
        }).RequireAuthorization(ApplicationPolicies.EditEvents);

        api.MapGet("/events/draft", (IEventService service, CancellationToken cancellationToken) =>
            service.CreateDraftAsync(cancellationToken))
            .RequireAuthorization(ApplicationPolicies.CreateEvents);

        api.MapPost("/events/speakers", (IReadOnlyCollection<int> selectedSpeakerIds, IEventService service, CancellationToken cancellationToken) =>
            service.GetSpeakerOptionsAsync(selectedSpeakerIds, cancellationToken))
            .RequireAuthorization(ApplicationPolicies.EditEvents);

        api.MapPost("/events/validate", (EventEditModel model, IEventService service, CancellationToken cancellationToken) =>
            service.ValidateEventAsync(model, cancellationToken))
            .RequireAuthorization(ApplicationPolicies.EditEvents);

        api.MapPost("/events", (EventEditModel model, IEventService service, CancellationToken cancellationToken) =>
            service.SaveEventAsync(model, cancellationToken))
            .RequireAuthorization(ApplicationPolicies.EditEvents);

        api.MapPost("/markdown/preview", (MarkdownPreviewRequest request, IMarkdownRenderer renderer) =>
            new MarkdownPreviewResponse(renderer.Render(request.Markdown)))
            .RequireAuthorization();

        api.MapGet("/admin/users", (IUserAdminService service, CancellationToken cancellationToken) =>
            service.GetUsersAsync(cancellationToken))
            .RequireAuthorization(ApplicationPolicies.AdminUsers);

        api.MapPost("/admin/users/roles", (UserRoleUpdateRequest request, IUserAdminService service, CancellationToken cancellationToken) =>
            service.UpdateRolesAsync(request, cancellationToken))
            .RequireAuthorization(ApplicationPolicies.AdminUsers);

        api.MapGet("/topics", (ITopicSuggestionService service, CancellationToken cancellationToken) =>
            service.GetSuggestionsAsync(cancellationToken))
            .RequireAuthorization(ApplicationPolicies.ManageTopics);

        api.MapPost("/topics", (TopicSuggestionCreateModel model, ITopicSuggestionService service, CancellationToken cancellationToken) =>
            service.CreateSuggestionAsync(model, cancellationToken))
            .RequireAuthorization(ApplicationPolicies.ManageTopics);

        api.MapPost("/topics/{topicSuggestionId:int}/vote", (int topicSuggestionId, ITopicSuggestionService service, CancellationToken cancellationToken) =>
            service.VoteAsync(topicSuggestionId, cancellationToken))
            .RequireAuthorization(ApplicationPolicies.ManageTopics);

        api.MapDelete("/topics/{topicSuggestionId:int}/vote", (int topicSuggestionId, ITopicSuggestionService service, CancellationToken cancellationToken) =>
            service.RemoveVoteAsync(topicSuggestionId, cancellationToken))
            .RequireAuthorization(ApplicationPolicies.ManageTopics);

        api.MapPost("/topics/{topicSuggestionId:int}/volunteer", (int topicSuggestionId, ITopicSuggestionService service, CancellationToken cancellationToken) =>
            service.VolunteerAsync(topicSuggestionId, cancellationToken))
            .RequireAuthorization(ApplicationPolicies.ManageTopics);

        return app;
    }

    public sealed record MarkdownPreviewRequest(string? Markdown);

    public sealed record MarkdownPreviewResponse(string Html);
}