using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

namespace UserGroupSiteOpus5.Server.Endpoints;

/// <summary>
/// HTTP surface backing <see cref="IUserAdminService"/> for the WebAssembly client.
/// </summary>
public static class UserAdminEndpoints
{
    /// <summary>Maps the member administration API onto <paramref name="app"/>.</summary>
    public static IEndpointRouteBuilder MapUserAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .RequireAuthorization(PolicyNames.AdminOnly);

        group.MapGet("/", async (IUserAdminService users) =>
                Results.Ok(await users.GetUsersAsync()))
            .WithName("GetUsers")
            .WithSummary("Lists every member with their role assignments.");

        group.MapPut("/{id:int}/roles", async (int id, UserRoleUpdateModel model, IUserAdminService users) =>
                // The id comes from the route, not the body, so a mismatched body cannot retarget
                // the change. The service still applies the self-demotion guard.
                (await users.UpdateUserRolesAsync(model with { UserId = id })).ToHttpResult())
            .AddEndpointFilter<RequireClientHeaderFilter>()
            .WithName("UpdateUserRoles")
            .WithSummary("Changes a member's Admin and Speaker roles.");

        return app;
    }
}
