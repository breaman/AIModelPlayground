using UserGroupSiteKimiK26.Shared.Dtos;
using UserGroupSiteKimiK26.Shared.Services;

namespace UserGroupSiteKimiK26.Server.Api;

public static class UsersApi
{
    public static IEndpointRouteBuilder MapUsersApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users").RequireAuthorization("RequireAdmin");

        group.MapGet("/", async (IUsersService usersService) =>
        {
            var users = await usersService.GetAllUsersAsync();
            return Results.Ok(users);
        });

        group.MapPut("/{id:int}/roles", async (int id, UpdateUserRolesDto dto, IUsersService usersService) =>
        {
            try
            {
                await usersService.UpdateUserRolesAsync(id, dto);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return endpoints;
    }
}