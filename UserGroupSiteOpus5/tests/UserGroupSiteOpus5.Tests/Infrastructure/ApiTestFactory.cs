using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Common;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace UserGroupSiteOpus5.Tests.Infrastructure;

/// <summary>
/// Boots the real server application against SQLite, with header-driven authentication, so the API
/// endpoints can be exercised over HTTP exactly as the WebAssembly client calls them.
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <summary>
    /// Whether to swap the Identity cookie for header-driven authentication.
    /// </summary>
    /// <remarks>
    /// Set to false to exercise the real cookie handler, which is the only way to test how an
    /// unauthenticated API call is rejected — the substitute handler would answer with its own
    /// challenge and hide a login redirect.
    /// </remarks>
    public bool UseHeaderAuthentication { get; init; } = true;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment(Environments.Production);

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // The real registration reads this before the test replaces the provider; a
                // placeholder keeps AddDbContext from failing during host construction.
                ["ConnectionStrings:" + ServiceDefaults.Constants.DatabaseConnectionString] =
                    "Server=(local);Database=placeholder;Trusted_Connection=True;TrustServerCertificate=True",
                // No bootstrap admin: the tests create exactly the users they need.
                ["Admin:Email"] = "",
                ["Admin:Password"] = ""
            });
        });

        builder.ConfigureTestServices(services =>
        {
            ReplaceDatabaseWithSqlite(services);
            RemoveDatabaseSeeder(services);

            if (UseHeaderAuthentication)
            {
                ReplaceAuthenticationWithHeaders(services);
            }
        });
    }

    /// <summary>
    /// Drops the startup seeder from the test host.
    /// </summary>
    /// <remarks>
    /// It runs during host startup, before the test has had a chance to create the SQLite schema,
    /// and a hosted service that throws in <c>StartAsync</c> aborts the whole host. The tests seed
    /// the roles they need themselves in <see cref="InitializeDatabaseAsync"/>.
    /// </remarks>
    private static void RemoveDatabaseSeeder(IServiceCollection services)
    {
        var descriptors = services
            .Where(x => x.ImplementationType == typeof(Server.Data.DatabaseSeeder))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    /// <summary>Swaps the SQL Server context registration for the shared in-memory SQLite one.</summary>
    /// <remarks>
    /// The options-configuration registrations must go as well as the options themselves. They are
    /// what actually call <c>UseSqlServer</c> (and Aspire's enrichment), and leaving them in place
    /// registers two providers in one container, which EF Core rejects outright.
    /// </remarks>
    private void ReplaceDatabaseWithSqlite(IServiceCollection services)
    {
        services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
        services.RemoveAll<DbContextOptions>();

        services.AddDbContext<ApplicationDbContext>(options => options
            .UseSqlite(_connection)
            .ReplaceService<IModelCustomizer, SqliteModelCustomizer>());
    }

    /// <summary>
    /// Makes the test scheme the default so <c>RequireAuthorization</c> resolves the principal the
    /// test supplied rather than looking for an Identity cookie.
    /// </summary>
    private static void ReplaceAuthenticationWithHeaders(IServiceCollection services)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName, _ => { });
    }

    /// <summary>Creates the schema and seeds the application roles.</summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        foreach (var roleName in RoleNames.All)
        {
            dbContext.Roles.Add(new Role { Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>Runs an action against a scoped service, for arranging data and asserting state.</summary>
    public async Task WithScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    /// <summary>Creates a user directly, bypassing the registration flow.</summary>
    public async Task<int> CreateUserAsync(string email, params string[] roles)
    {
        var userId = 0;

        await WithScopeAsync(async provider =>
        {
            var userManager = provider.GetRequiredService<UserManager<User>>();
            var user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = email.Split('@')[0],
                MemberSince = DateTimeOffset.UtcNow
            };

            var result = await userManager.CreateAsync(user, "Password1!");
            result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors.Select(x => x.Description)));

            if (roles.Length > 0)
            {
                await userManager.AddToRolesAsync(user, roles);
            }

            userId = user.Id;
        });

        return userId;
    }

    /// <summary>
    /// Creates a client that authenticates as the given user, and that sends the anti-CSRF header
    /// the real WebAssembly client sends.
    /// </summary>
    /// <param name="userId">The user id, or null for an anonymous client.</param>
    /// <param name="roles">The roles the user holds.</param>
    public HttpClient CreateClientAs(int? userId, params string[] roles)
    {
        var client = CreateClient();

        if (userId is { } id)
        {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, id.ToString());
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, string.Join(",", roles));
        }

        client.DefaultRequestHeaders.Add(ApiHeaders.RequestedWith, ApiHeaders.RequestedWithValue);

        return client;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
