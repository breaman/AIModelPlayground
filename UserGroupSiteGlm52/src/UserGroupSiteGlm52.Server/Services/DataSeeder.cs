using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using UserGroupSiteGlm52.Data.Models;

namespace UserGroupSiteGlm52.Server.Services;

/// <summary>
/// Idempotent startup seeder that ensures the <c>Admin</c> and <c>Speaker</c>
/// roles exist and creates a default administrator from configuration so that
/// someone can manage roles immediately after a fresh database.
/// </summary>
public static class DataSeeder
{
    private const string AdminRole = "Admin";
    private const string SpeakerRole = "Speaker";

    /// <summary>Ensure roles + the configured default admin user exist. Safe to call on every startup.</summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // RoleManager/UserManager are scoped; resolve them from a dedicated scope.
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var roleManager = provider.GetRequiredService<RoleManager<Role>>();
        var userManager = provider.GetRequiredService<UserManager<User>>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var logger = provider.GetRequiredService<ILogger<DatabaseSeederLogger>>();

        await EnsureRoleAsync(roleManager, AdminRole, logger, cancellationToken);
        await EnsureRoleAsync(roleManager, SpeakerRole, logger, cancellationToken);
        await EnsureDefaultAdminAsync(userManager, configuration, logger, cancellationToken);
    }

    private static async Task EnsureRoleAsync(RoleManager<Role> roleManager, string name,
        ILogger logger, CancellationToken cancellationToken)
    {
        if (await roleManager.RoleExistsAsync(name))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new Role { Name = name });
        if (result.Succeeded)
        {
            logger.LogInformation("Seeded role {RoleName}", name);
        }
        else
        {
            logger.LogError("Failed to seed role {RoleName}: {Errors}", name,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task EnsureDefaultAdminAsync(UserManager<User> userManager,
        IConfiguration configuration, ILogger logger, CancellationToken cancellationToken)
    {
        // Configuration first (user secrets / appsettings), with a documented dev fallback
        // so a fresh environment is usable without extra setup.
        var email = configuration["AdminUser:Email"];
        var password = configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            email = "admin@usergroup.local";
            password = "Admin123!";
            logger.LogWarning("No AdminUser:Email/Password configured; using dev fallback '{Email}' / '{Password}'", email, password);
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Ensure an existing admin-named account still holds the Admin role.
            if (!await userManager.IsInRoleAsync(existing, AdminRole))
            {
                await userManager.AddToRoleAsync(existing, AdminRole);
            }

            return;
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // so the seeded admin can log in without the dev confirm link
            FirstName = configuration["AdminUser:FirstName"] ?? "Site",
            LastName = configuration["AdminUser:LastName"] ?? "Administrator",
            MemberSince = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to seed default admin {Email}: {Errors}", email,
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, AdminRole);
        logger.LogInformation("Seeded default admin {Email}", email);
    }

    /// <summary>Marker type used only to obtain a named logger for the seeder.</summary>
    internal sealed class DatabaseSeederLogger;
}