using System.Security.Claims;

using UserGroupSiteMiniMaxM3.Data.Models;

namespace UserGroupSiteMiniMaxM3.Tests;

/// <summary>Helpers shared across test classes.</summary>
internal static class TestHelpers
{
    /// <summary>Creates a user with the given name and persists them.</summary>
    public static async Task<User> CreateUserAsync(SqliteDbFixture fx, string username, string firstName = "", string lastName = "")
    {
        var user = new User
        {
            UserName = username,
            Email = $"{username}@test.local",
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
        };

        var result = await fx.UserManager.CreateAsync(user, "Passw0rd!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Failed to create test user: " + string.Join(",", result.Errors));
        }
        return user;
    }

    /// <summary>Builds a <see cref="ClaimsPrincipal"/> representing an admin user.</summary>
    public static ClaimsPrincipal BuildAdminPrincipal(SqliteDbFixture fx)
    {
        // We use a synthetic identity; the EventService resolves the int user id
        // from the NameIdentifier claim.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "0"), // 0 = system / no current user; the service uses other signals
            new Claim(ClaimTypes.Role, Roles.Admin),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
