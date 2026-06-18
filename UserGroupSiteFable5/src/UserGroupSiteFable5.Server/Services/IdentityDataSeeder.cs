using Microsoft.AspNetCore.Identity;

using UserGroupSiteFable5.Data.Models;

namespace UserGroupSiteFable5.Server.Services;

/// <summary>
/// Seeds a development-only initial admin account so the first user can manage roles.
/// Roles themselves are seeded via <c>HasData</c> in <see cref="ApplicationDbContext"/>.
/// </summary>
public static class IdentityDataSeeder
{
    public const string DevAdminEmail = "admin@usergroup.local";
    private const string DevAdminPassword = "Admin123!";

    /// <summary>Creates the dev admin user (idempotent) and ensures it holds the Admin role.</summary>
    public static async Task SeedDevAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var admin = await userManager.FindByEmailAsync(DevAdminEmail);
        if (admin is null)
        {
            admin = new User
            {
                UserName = DevAdminEmail,
                Email = DevAdminEmail,
                EmailConfirmed = true,
                FirstName = "Site",
                LastName = "Admin",
                MemberSince = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, DevAdminPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed dev admin user: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}