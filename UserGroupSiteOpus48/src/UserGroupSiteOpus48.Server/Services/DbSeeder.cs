using UserGroupSiteOpus48.Data.Models;
using UserGroupSiteOpus48.Shared.Authorization;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace UserGroupSiteOpus48.Server.Services;

/// <summary>
/// Seeds the required application roles and a bootstrap administrator at startup so the
/// site is always usable on a fresh database. Run after EF migrations have been applied.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Ensures the <see cref="RoleNames.Admin"/> and <see cref="RoleNames.Speaker"/> roles exist
    /// and that the configured bootstrap admin account exists, is confirmed, and is in the Admin role.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var roleManager = sp.GetRequiredService<RoleManager<Role>>();
        var userManager = sp.GetRequiredService<UserManager<User>>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));

        // Ensure every required role exists.
        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.Speaker })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new Role { Name = roleName });
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // Ensure the bootstrap admin exists and is an administrator.
        var options = sp.GetRequiredService<IOptions<BootstrapAdminOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("BootstrapAdmin email/password not configured; skipping admin seeding.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(options.Email);
        if (admin is null)
        {
            admin = new User
            {
                UserName = options.Email,
                Email = options.Email,
                // Confirm immediately so login works despite RequireConfirmedAccount = true.
                EmailConfirmed = true,
                FirstName = options.FirstName,
                LastName = options.LastName,
                MemberSince = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(admin, options.Password);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create bootstrap admin {Email}: {Errors}", options.Email,
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Created bootstrap admin {Email}.", options.Email);
        }

        // Promote to Admin if not already (covers both new and pre-existing accounts).
        if (!await userManager.IsInRoleAsync(admin, RoleNames.Admin))
        {
            await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        }
    }
}
