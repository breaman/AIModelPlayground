using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using UserGroupSiteMiniMaxM3.Data.Models;

namespace UserGroupSiteMiniMaxM3.Data.Services;

/// <summary>
/// Provides one-time seeding of Identity roles. Idempotent: running
/// <see cref="SeedAsync"/> repeatedly has no effect after the first successful run.
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Ensures the <see cref="Roles.Admin"/> and <see cref="Roles.Speaker"/> roles exist.
    /// Safe to call from application startup or on demand.
    /// </summary>
    /// <param name="serviceProvider">The application's service provider used to resolve <see cref="RoleManager{T}"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RoleManager<Role>>>();

        string[] roles = [Roles.Admin, Roles.Speaker];
        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role).WaitAsync(cancellationToken))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new Role { Name = role }).WaitAsync(cancellationToken);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create role {Role}: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            else
            {
                logger.LogInformation("Created role {Role}", role);
            }
        }
    }
}