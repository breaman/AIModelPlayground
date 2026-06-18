using UserGroupSiteNemoTron3.Data.Interfaces;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Server.Endpoints;

public static class UserManagementEndpoints
{
    public static void MapUserManagementEndpoints(this WebApplication app)
    {
        var adminUsersGroup = app.MapGroup("/api/admin/users").WithTags("Admin Users").RequireAuthorization("AdminOnly");

        adminUsersGroup.MapGet("/", async (IUserManagementService userService) =>
        {
            var users = await userService.GetAllUsersAsync();
            return Results.Ok(users);
        });

        adminUsersGroup.MapPut("/{id:int}/roles", async (int id, UpdateUserRolesDto dto, IUserManagementService userService, IUserService currentUserService) =>
        {
            var user = await userService.UpdateUserRolesAsync(id, dto, currentUserService.UserId);
            return Results.Ok(user);
        });
    }
}