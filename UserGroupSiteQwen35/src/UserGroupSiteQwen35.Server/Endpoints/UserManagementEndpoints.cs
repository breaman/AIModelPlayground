using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

using UserGroupSiteQwen35.Data.Models;
using UserGroupSiteQwen35.Server.Services;

namespace UserGroupSiteQwen35.Server.Endpoints;

public static class UserManagementEndpoints
{
    public static void MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");

        group.MapGet("/", async (UserManagementService service) =>
            await service.GetAllUsersWithRolesAsync())
            .RequireAuthorization(RoleNames.Admin);

        group.MapGet("/current", async (HttpContext httpContext, UserManagementService service) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            return Results.Ok(await service.GetCurrentUserWithRolesAsync(int.Parse(userId)));
        }).RequireAuthorization();

        group.MapGet("/{id:int}", async (int id, UserManagementService service) =>
        {
            var user = await service.GetUserByIdAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        }).RequireAuthorization(RoleNames.Admin);

        group.MapPut("/{id:int}/role", async (
            int id,
            RoleUpdateDto dto,
            HttpContext httpContext,
            UserManagementService service) =>
        {
            var currentUserId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Results.Unauthorized();

            var result = dto.Operation.ToLower() switch
            {
                "add" => await service.AddRoleToUserAsync(id, dto.Role),
                "remove" => await service.RemoveRoleFromUserAsync(id, dto.Role, int.Parse(currentUserId)),
                _ => IdentityResult.Failed(new IdentityError { Description = "Invalid operation" })
            };

            return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
        }).RequireAuthorization(RoleNames.Admin);

        group.MapGet("/{id:int}/role/{role}", async (
            int id,
            string role,
            UserManagementService service) =>
        {
            var isInRole = await service.IsUserInRoleAsync(id, role);
            return Results.Ok(isInRole);
        }).RequireAuthorization();
    }
}

public class RoleUpdateDto
{
    public string Role { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
}

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
}