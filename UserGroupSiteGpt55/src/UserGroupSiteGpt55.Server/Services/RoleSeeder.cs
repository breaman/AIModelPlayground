using Microsoft.AspNetCore.Identity;

using UserGroupSiteGpt55.Data.Models;
using UserGroupSiteGpt55.Shared.Authorization;

namespace UserGroupSiteGpt55.Server.Services;

/// <summary>
/// Ensures required roles exist and optionally assigns the first configured administrator.
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Creates Admin and Speaker roles and assigns Admin to InitialAdminEmail when configured.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        foreach (var roleName in new[] { ApplicationRoles.Admin, ApplicationRoles.Speaker })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role { Name = roleName });
            }
        }

        var initialAdminEmail = configuration["InitialAdminEmail"];
        if (string.IsNullOrWhiteSpace(initialAdminEmail))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(initialAdminEmail);
        if (user is not null && !await userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
        {
            await userManager.AddToRoleAsync(user, ApplicationRoles.Admin);
        }
    }
}
