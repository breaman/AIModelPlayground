using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Common;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteOpus5.Server.Data;

/// <summary>
/// Ensures the application's roles exist and that at least one administrator can sign in.
/// </summary>
/// <remarks>
/// Runs once at startup, after the Aspire <c>ef-migrations</c> resource has applied the schema.
/// The bootstrap administrator is the only way to get the first admin into the system: role
/// management itself requires an admin, so without this the site would be permanently locked out
/// of its own administration screens.
/// </remarks>
public class DatabaseSeeder(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<DatabaseSeeder> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // A hosted service is a singleton, so the scoped Identity services must be resolved from a
        // scope created here rather than injected directly.
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedAdministratorAsync(userManager);

        if (environment.IsDevelopment())
        {
            await SeedSampleEventsAsync(dbContext, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>Creates any of the application's roles that do not yet exist.</summary>
    private async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new Role { Name = roleName });
            if (result.Succeeded)
            {
                logger.LogInformation("Created role {RoleName}.", roleName);
            }
            else
            {
                logger.LogError("Failed to create role {RoleName}: {Errors}",
                    roleName,
                    string.Join(", ", result.Errors.Select(x => x.Description)));
            }
        }
    }

    /// <summary>
    /// Creates the bootstrap administrator from configuration, or promotes the account to Admin if
    /// it already exists without the role. Does nothing when the configuration keys are absent.
    /// </summary>
    private async Task SeedAdministratorAsync(UserManager<User> userManager)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No Admin:Email/Admin:Password configured; skipping bootstrap administrator. " +
                "Set these in user secrets to be able to sign in as an administrator.");
            return;
        }

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                // The bootstrap admin bypasses the confirmation flow: the template's email sender
                // is a no-op, so an unconfirmed bootstrap account could never sign in.
                EmailConfirmed = true,
                FirstName = configuration["Admin:FirstName"] ?? "Site",
                LastName = configuration["Admin:LastName"] ?? "Administrator",
                MemberSince = DateTimeOffset.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create the bootstrap administrator: {Errors}",
                    string.Join(", ", createResult.Errors.Select(x => x.Description)));
                return;
            }

            logger.LogInformation("Created bootstrap administrator {Email}.", email);
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            await userManager.AddToRoleAsync(user, RoleNames.Admin);
            logger.LogInformation("Granted the Admin role to {Email}.", email);
        }

        // The bootstrap admin doubles as a speaker so a fresh development database has someone to
        // assign to an event.
        if (!await userManager.IsInRoleAsync(user, RoleNames.Speaker))
        {
            await userManager.AddToRoleAsync(user, RoleNames.Speaker);
        }
    }

    /// <summary>
    /// Adds a small set of example events in Development so the home page is not empty on a fresh
    /// database. Skipped entirely once any event exists.
    /// </summary>
    private async Task SeedSampleEventsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Events.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        dbContext.Events.AddRange(
            new Event
            {
                Title = "Minimal APIs in .NET 10",
                Slug = "minimal-apis-in-net-10",
                ShortDescription = "A tour of what changed in minimal APIs, and when to still reach for controllers.",
                Description = "## What we'll cover\n\n- Route groups and typed results\n- Validation and `ProblemDetails`\n- Testing endpoints without a browser\n\nBring questions.",
                EventDateTime = now.AddDays(-30),
                Location = "Community Hall, Room 2",
                IsPublished = true
            },
            new Event
            {
                Title = "Blazor WebAssembly Performance",
                Slug = "blazor-webassembly-performance",
                ShortDescription = "Trimming payloads, pre-rendering, and knowing when a render is wasted.",
                Description = "We look at real traces from a production Blazor app and work out where the time actually goes.",
                EventDateTime = now.AddDays(-2),
                Location = "Community Hall, Room 1",
                IsPublished = true
            },
            new Event
            {
                Title = "Untitled Draft Meeting",
                Slug = "untitled-draft-meeting",
                ShortDescription = "Still being planned.",
                IsPublished = false
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded sample events for development.");
    }
}
