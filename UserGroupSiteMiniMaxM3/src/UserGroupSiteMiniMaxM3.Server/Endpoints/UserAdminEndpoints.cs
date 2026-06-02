using System.Security.Claims;

using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Shared.Services;

namespace UserGroupSiteMiniMaxM3.Server.Endpoints;

/// <summary>
/// Minimal API endpoints that expose admin user-management operations to the
/// Blazor WebAssembly client. All endpoints require the <see cref="Roles.Admin"/> role.
/// </summary>
internal static class UserAdminEndpoints
{
    /// <summary>Mounts the user-admin endpoints under <c>/api/admin/users</c>.</summary>
    public static IEndpointRouteBuilder MapUserAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/users")
            .RequireAuthorization()
            .RequireAuthorization(p => p.RequireRole(Roles.Admin));

        group.MapGet("", async (IUserAdminService svc, CancellationToken ct) =>
            TypedResults.Ok(await svc.ListUsersAsync(ct)));

        group.MapPut("{id:int}/roles", async (
            int id,
            UpdateRolesRequest body,
            IUserAdminService svc,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            try
            {
                await svc.UpdateRolesAsync(id, body.IsAdmin, body.IsSpeaker, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        });

        return endpoints;
    }

    /// <summary>Request body for updating a user's roles.</summary>
    public record UpdateRolesRequest(bool IsAdmin, bool IsSpeaker);
}