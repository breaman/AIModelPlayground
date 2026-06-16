using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UserGroupSiteKimiK27Code.Data.Models;

namespace UserGroupSiteKimiK27Code.Tests;

public static class TestHelpers
{
    public static UserManager<User> CreateUserManager(ApplicationDbContext dbContext)
    {
        var store = new UserStore<User, Role, ApplicationDbContext, int>(dbContext);
        var optionsAccessor = Options.Create(new IdentityOptions
        {
            Password = { RequireDigit = false, RequiredLength = 6, RequireLowercase = false, RequireUppercase = false, RequireNonAlphanumeric = false }
        });
        var passwordHasher = new PasswordHasher<User>();
        var userValidators = new List<IUserValidator<User>> { new UserValidator<User>() };
        var passwordValidators = new List<IPasswordValidator<User>> { new PasswordValidator<User>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = new LoggerFactory().CreateLogger<UserManager<User>>();

        return new UserManager<User>(
            store,
            optionsAccessor,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            new ServiceProviderStub(),
            logger);
    }

    public static async Task SeedRolesAsync(ApplicationDbContext dbContext)
    {
        foreach (var roleName in new[] { "Admin", "Speaker" })
        {
            if (!dbContext.Roles.Any(r => r.Name == roleName))
            {
                dbContext.Roles.Add(new Role { Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task<User> CreateSpeakerAsync(ApplicationDbContext dbContext, UserManager<User> userManager, string firstName, string lastName)
    {
        await SeedRolesAsync(dbContext);

        var email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@test.local";
        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            MemberSince = DateTime.UtcNow
        };

        await userManager.CreateAsync(user, "Password1");
        await userManager.AddToRoleAsync(user, "Speaker");
        return user;
    }

    private sealed class ServiceProviderStub : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ILogger<UserManager<User>>))
            {
                return new LoggerFactory().CreateLogger<UserManager<User>>();
            }

            return null;
        }
    }
}