using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Server.Endpoints;

/// <summary>Admin-only user and role management API.</summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IUserAdminService users) => Results.Ok(await users.GetUsersAsync()));

        group.MapPut("/{id:int}/roles", async (int id, UpdateUserRolesDto dto, IUserAdminService users) =>
            (await users.UpdateUserRolesAsync(id, dto)).ToHttpResult());
    }
}
