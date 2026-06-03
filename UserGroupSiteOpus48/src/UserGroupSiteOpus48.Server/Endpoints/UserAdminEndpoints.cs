using UserGroupSiteOpus48.Shared.Authorization;
using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Server.Endpoints;

/// <summary>
/// Minimal API endpoints for admin user/role management. The entire group requires the Admin policy.
/// </summary>
public static class UserAdminEndpoints
{
    public static IEndpointRouteBuilder MapUserAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users").RequireAuthorization(Policies.AdminOnly);

        group.MapGet("/", (IUserAdminService users) => users.GetUsersAsync());

        group.MapPost("/role", (SetRoleRequest request, IUserAdminService users) => users.SetRoleAsync(request));

        return app;
    }
}
