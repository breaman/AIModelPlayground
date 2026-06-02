using UserGroupSiteKimiK26.Data.Models;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteKimiK26.Server.Services;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();
        string[] roles = ["Admin", "Speaker"];

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role { Name = roleName });
            }
        }
    }
}
