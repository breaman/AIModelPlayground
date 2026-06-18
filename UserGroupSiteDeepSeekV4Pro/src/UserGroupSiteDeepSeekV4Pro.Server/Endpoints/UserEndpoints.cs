using System.Security.Claims;

using UserGroupSiteDeepSeekV4Pro.Shared.Constants;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Server.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization(Policies.CanManageUsers);

        // GET /api/users - List all users with roles
        group.MapGet("/", async (IUserManagementService userService) =>
        {
            var users = await userService.GetAllUsersAsync();
            return Results.Ok(users);
        });

        // PUT /api/users/{id}/roles - Update user roles
        group.MapPut("/{id:int}/roles", async (IUserManagementService userService, int id, List<string> roles, ClaimsPrincipal user) =>
        {
            try
            {
                await userService.UpdateUserRolesAsync(id, roles);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return group;
    }
}