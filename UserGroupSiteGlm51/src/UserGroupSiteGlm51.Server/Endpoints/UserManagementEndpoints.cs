using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Shared.Models;

namespace UserGroupSiteGlm51.Server.Endpoints;

/// <summary>
/// Minimal API endpoints for admin user management.
/// </summary>
public static class UserManagementEndpoints
{
    public static void MapUserManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization("Admin");

        // Get all users
        group.MapGet("/", async (IUserManagementService userMgmtService) =>
        {
            var users = await userMgmtService.GetAllUsersAsync();
            var dtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await userMgmtService.GetUserRolesAsync(user.Id);
                dtos.Add(new UserDto(user.Id, user.FirstName, user.LastName, user.Email, roles.ToList()));
            }

            return Results.Ok(dtos);
        });

        // Get user by ID
        group.MapGet("/{id}", async (int id, IUserManagementService userMgmtService) =>
        {
            var user = await userMgmtService.GetUserByIdAsync(id);
            if (user is null)
            {
                return Results.NotFound();
            }

            var roles = await userMgmtService.GetUserRolesAsync(user.Id);
            return Results.Ok(new UserDto(user.Id, user.FirstName, user.LastName, user.Email, roles.ToList()));
        });

        // Get user roles
        group.MapGet("/{id}/roles", async (int id, IUserManagementService userMgmtService) =>
        {
            var roles = await userMgmtService.GetUserRolesAsync(id);
            return Results.Ok(new UserRoleDto(id, roles.ToList()));
        });

        // Add user to role
        group.MapPost("/{id}/roles/{role}", async (int id, string role, IUserManagementService userMgmtService, IUserService userService) =>
        {
            // Prevent self-removal from Admin role is handled on delete; add is always allowed
            await userMgmtService.AddToRoleAsync(id, role);
            return Results.Ok();
        });

        // Remove user from role
        group.MapDelete("/{id}/roles/{role}", async (int id, string role, IUserManagementService userMgmtService, IUserService userService) =>
        {
            // Cannot remove yourself from Admin role
            if (role == "Admin" && userService.UserId == id)
            {
                return Results.BadRequest("You cannot remove yourself from the Admin role.");
            }

            // Cannot remove the last Admin
            if (role == "Admin" && await userMgmtService.IsLastAdminAsync(id))
            {
                return Results.BadRequest("Cannot remove the last Admin user.");
            }

            await userMgmtService.RemoveFromRoleAsync(id, role);
            return Results.NoContent();
        });
    }
}