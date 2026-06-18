using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Server.Endpoints;

/// <summary>
/// Maps the application's Minimal API endpoints. Authorization is enforced per
/// endpoint via <c>RequireAuthorization</c>; the services apply the finer
/// admin-or-assigned-speaker rules. Failures return RFC 9457 ProblemDetails.
/// </summary>
public static class ApiEndpoints
{
    /// <summary>Register all app API endpoints on <paramref name="app"/>.</summary>
    public static IEndpointRouteBuilder MapAppApi(this IEndpointRouteBuilder app)
    {
        app.MapEventEndpoints();
        app.MapTopicEndpoints();
        app.MapUserAdminEndpoints();
        return app;
    }

    private static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var events = app.MapGroup("/api/events");
        var manage = app.MapGroup("/api/events/manage").RequireAuthorization("Admin");
        var admin = app.MapGroup("/api/events/admin").RequireAuthorization("Admin");
        var edit = app.MapGroup("/api/events/edit").RequireAuthorization();

        // Public: published events (home page) and published detail by slug.
        events.MapGet("/", async (IEventService svc) =>
            Results.Ok(await svc.GetPublishedEventsAsync()));

        events.MapGet("/{slug}", async (string slug, IEventService svc) =>
        {
            var detail = await svc.GetPublishedBySlugAsync(slug);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        // Admin manage list (all events).
        manage.MapGet("/", async (IEventService svc) =>
            Results.Ok(await svc.GetEditableListAsync()));

        // Speaker options for the event editor's picker (create mode has no event yet).
        manage.MapGet("/speakers", async (IEventService svc) =>
            Results.Ok(await svc.GetSpeakerOptionsAsync()));

        // Edit endpoints: any logged-in user may request, the service enforces
        // admin-or-assigned-speaker (403 otherwise), 404 when missing.
        edit.MapGet("/{id:int}", async (int id, IEventService svc) => ToHttp(await svc.GetForEditAsync(id)));

        admin.MapPost("/", async (EventEditDto dto, IEventService svc) => ToHttp(await svc.CreateAsync(dto)));

        edit.MapPut("/{id:int}", async (int id, EventEditDto dto, IEventService svc) =>
            ToHttp(await svc.UpdateAsync(id, dto)));

        return app;
    }

    private static IEndpointRouteBuilder MapTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var topics = app.MapGroup("/api/topics").RequireAuthorization();

        topics.MapGet("/", async (ITopicService svc) =>
            Results.Ok(await svc.GetTopicsAsync()));

        topics.MapPost("/", async (TopicInputDto dto, ITopicService svc) => ToHttp(await svc.SuggestAsync(dto)));

        topics.MapPost("/{id:int}/vote", async (int id, ITopicService svc) => ToHttp(await svc.VoteAsync(id)));

        topics.MapDelete("/{id:int}/vote", async (int id, ITopicService svc) => ToHttp(await svc.UnvoteAsync(id)));

        topics.MapPost("/{id:int}/volunteer", async (int id, ITopicService svc) =>
            ToHttp(await svc.VolunteerAsync(id)));

        return app;
    }

    private static IEndpointRouteBuilder MapUserAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/admin/users").RequireAuthorization("Admin");

        users.MapGet("/", async (IUserAdminService svc) =>
            Results.Ok(await svc.GetUsersAsync()));

        users.MapPut("/{id:int}/roles", async (int id, RolesInput input, IUserAdminService svc) =>
            ToHttp(await svc.UpdateRolesAsync(id, input.IsAdmin, input.IsSpeaker)));

        return app;
    }

    /// <summary>Translate a typed service result into an HTTP response.</summary>
    private static IResult ToHttp<T>(ServiceResult<T> result)
    {
        return result.Succeeded ? Results.Ok(result.Value) : Problem(result.ErrorStatus, result.Errors, result.ValidationErrors);
    }

    /// <summary>Translate a void service result into an HTTP response.</summary>
    private static IResult ToHttp(ServiceResult result)
    {
        return result.Succeeded ? Results.NoContent() : Problem(result.ErrorStatus, result.Errors, result.ValidationErrors);
    }

    private static IResult Problem(int errorStatus, IReadOnlyList<string> errors, IReadOnlyDictionary<string, string[]> validationErrors)
    {
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors.ToDictionary(kv => kv.Key, kv => kv.Value),
                statusCode: errorStatus);
        }

        return Results.Problem(
            detail: errors.Count > 0 ? string.Join(" ", errors) : "The request could not be processed.",
            statusCode: errorStatus);
    }

    /// <summary>Bind shape for the role-update body.</summary>
    public sealed class RolesInput
    {
        public bool IsAdmin { get; set; }

        public bool IsSpeaker { get; set; }
    }
}