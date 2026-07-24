using UserGroupSiteGpt56Sol.Data.Models;
using UserGroupSiteGpt56Sol.Shared.Authorization;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteGpt56Sol.Server.Services;

/// <summary>Creates application roles and optionally provisions a configured first administrator.</summary>
public static class RoleSeeder
{
    /// <summary>Seeds roles idempotently and grants Admin to the configured existing account.</summary>
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        ILogger logger, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new Role { Name = roleName });
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Could not create role {roleName}: " +
                                                        string.Join(" ", result.Errors.Select(error =>
                                                            error.Description)));
                }
            }
        }

        var adminEmail = configuration["InitialAdmin:Email"];
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user is null)
        {
            logger.LogWarning("Initial admin account {AdminEmail} does not exist yet", adminEmail);
            return;
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            var result = await userManager.AddToRoleAsync(user, AppRoles.Admin);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Could not provision the initial administrator: " +
                                                    string.Join(" ", result.Errors.Select(error =>
                                                        error.Description)));
            }

            logger.LogInformation("Provisioned initial administrator {AdminEmail}", adminEmail);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}