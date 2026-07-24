using UserGroupSiteGpt56Sol.Server.Services;
using UserGroupSiteGpt56Sol.Shared.Authorization;
using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace UserGroupSiteGpt56Sol.Server.Endpoints;

/// <summary>Maps application JSON APIs used by Interactive WebAssembly components.</summary>
public static class ApplicationEndpoints
{
    /// <summary>Maps public queries and protected commands.</summary>
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var events = endpoints.MapGroup("/api/events");
        events.MapGet("/", async (IEventService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPublishedAsync(cancellationToken)));
        events.MapGet("/{slug}", async (string slug, IEventService service,
                CancellationToken cancellationToken) =>
            await service.GetPublishedBySlugAsync(slug, cancellationToken) is { } item
                ? Results.Ok(item)
                : Results.NotFound());

        var eventManagement = events.MapGroup("/manage").RequireAuthorization();
        eventManagement.MapGet("/", async (IEventService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetManageListAsync(cancellationToken)));
        eventManagement.MapGet("/speakers", async (IEventService service,
                CancellationToken cancellationToken) =>
            Results.Ok(await service.GetSpeakerOptionsAsync(cancellationToken)));
        eventManagement.MapGet("/{id:int}", async (int id, IEventService service,
                IAuthorizationService authorization, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!(await authorization.AuthorizeAsync(context.User, id, AppPolicies.EventEditor)).Succeeded)
            {
                return Results.Forbid();
            }

            return await service.GetForEditAsync(id, cancellationToken) is { } item
                ? Results.Ok(item)
                : Results.NotFound();
        });
        eventManagement.MapPost("/", async (EventEditRequest request, IEventService service,
                CancellationToken cancellationToken) =>
            Results.Ok(await service.SaveAsync(null, request, cancellationToken)))
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin))
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        eventManagement.MapPut("/{id:int}", async (int id, EventEditRequest request, IEventService service,
                IAuthorizationService authorization, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!(await authorization.AuthorizeAsync(context.User, id, AppPolicies.EventEditor)).Succeeded)
            {
                return Results.Forbid();
            }

            return Results.Ok(await service.SaveAsync(id, request, cancellationToken));
        }).WithMetadata(new RequireAntiforgeryTokenAttribute(true));

        var topics = endpoints.MapGroup("/api/topics").RequireAuthorization();
        topics.MapGet("/", async (ITopicService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(cancellationToken)));
        topics.MapPost("/", async (CreateTopicRequest request, ITopicService service,
                CancellationToken cancellationToken) =>
            Results.Ok(await service.CreateAsync(request, cancellationToken)))
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        topics.MapPost("/{id:int}/vote", async (int id, ITopicService service,
                CancellationToken cancellationToken) =>
            Results.Ok(await service.VoteAsync(id, cancellationToken)))
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        topics.MapDelete("/{id:int}/vote", async (int id, ITopicService service,
                CancellationToken cancellationToken) =>
            Results.Ok(await service.UnvoteAsync(id, cancellationToken)))
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        topics.MapPost("/{id:int}/volunteer", async (int id, ITopicService service,
                CancellationToken cancellationToken) =>
            Results.Ok(await service.VolunteerAsync(id, cancellationToken)))
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));

        var users = endpoints.MapGroup("/api/admin/users")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin));
        users.MapGet("/", async (IUserAdministrationService service,
                CancellationToken cancellationToken) =>
            Results.Ok(await service.GetUsersAsync(cancellationToken)));
        users.MapPut("/{id:int}/roles", async (int id, UpdateUserRolesRequest request,
                IUserAdministrationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateRolesAsync(id, request, cancellationToken)))
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));

        endpoints.MapGet("/api/antiforgery/token", (HttpContext context, IAntiforgery antiforgery) =>
            Results.Ok(new AntiforgeryTokenResponse(antiforgery.GetAndStoreTokens(context).RequestToken!)))
            .RequireAuthorization();
        return endpoints;
    }

    private sealed record AntiforgeryTokenResponse(string Token);
}
