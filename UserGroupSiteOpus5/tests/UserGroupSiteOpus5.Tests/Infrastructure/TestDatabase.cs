using System.Security.Claims;

using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Server.Authorization;
using UserGroupSiteOpus5.Shared.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace UserGroupSiteOpus5.Tests.Infrastructure;

/// <summary>
/// A disposable SQLite-backed database with Identity and the application's authorization services
/// wired up, for exercising the server-side services against real SQL.
/// </summary>
/// <remarks>
/// SQLite rather than the in-memory provider: the in-memory provider does not enforce unique
/// indexes, and the duplicate-vote and slug-collision behaviour under test depends on exactly
/// those constraints holding.
/// </remarks>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    /// <summary>The principal the services see as the current user.</summary>
    public ClaimsPrincipal CurrentUser { get; private set; } = new(new ClaimsIdentity());

    /// <summary>Creates an empty database with the schema applied.</summary>
    public TestDatabase()
    {
        // The connection must stay open: an in-memory SQLite database lives only as long as its
        // connection does.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<ApplicationDbContext>(options => options
            .UseSqlite(_connection)
            .ReplaceService<IModelCustomizer, SqliteModelCustomizer>());

        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireRole(RoleNames.Admin))
            .AddPolicy(PolicyNames.SpeakerOrAdmin, policy => policy.RequireRole(RoleNames.Admin, RoleNames.Speaker))
            .AddPolicy(PolicyNames.EventEditor, policy => policy.AddRequirements(new EventEditRequirement()));

        services.AddScoped<IAuthorizationHandler, EventEditorAuthorizationHandler>();

        // The services under test read the current principal from the ambient HttpContext.
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(_ => new DefaultHttpContext { User = CurrentUser });
        services.AddSingleton(accessor);

        _services = services.BuildServiceProvider();

        DbContext.Database.EnsureCreated();
        SeedRoles();
    }

    /// <summary>
    /// Creates every application role, mirroring what <c>DatabaseSeeder</c> does at startup.
    /// </summary>
    /// <remarks>
    /// Without this, a test that grants a role through a service would fail on a role row that
    /// exists in every real environment.
    /// </remarks>
    private void SeedRoles()
    {
        foreach (var roleName in RoleNames.All)
        {
            DbContext.Roles.Add(new Role
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });
        }

        DbContext.SaveChanges();
    }

    /// <summary>The service provider holding the configured Identity and authorization services.</summary>
    public IServiceProvider Services => _services;

    /// <summary>A long-lived context for arranging and asserting test data.</summary>
    public ApplicationDbContext DbContext => _services.GetRequiredService<ApplicationDbContext>();

    /// <summary>Resolves a service, typically one of the server-side feature services.</summary>
    public T GetService<T>() where T : notnull
    {
        return _services.GetRequiredService<T>();
    }

    /// <summary>
    /// Creates a user, optionally in the given roles, and returns them.
    /// </summary>
    public async Task<User> CreateUserAsync(string email, string? firstName = null, params string[] roles)
    {
        var userManager = _services.GetRequiredService<UserManager<User>>();
        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            MemberSince = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, "Password1!");
        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors.Select(x => x.Description)));

        if (roles.Length > 0)
        {
            await userManager.AddToRolesAsync(user, roles);
        }

        return user;
    }

    /// <summary>Makes subsequent service calls run as the given user, with the given roles.</summary>
    public void SignIn(User user, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? "")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    /// <summary>Makes subsequent service calls run anonymously.</summary>
    public void SignOut()
    {
        CurrentUser = new ClaimsPrincipal(new ClaimsIdentity());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
    }
}
