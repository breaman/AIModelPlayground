using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Data.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UserGroupSiteMiniMaxM3.Tests;

/// <summary>
/// Spins up an SQLite-backed <see cref="ApplicationDbContext"/> and a real
/// <see cref="UserManager{TUser}"/> over the same context so the data
/// services can be exercised end-to-end without a SQL Server dependency.
/// </summary>
public class SqliteDbFixture : IDisposable
{
    public ApplicationDbContext Db { get; }
    public UserManager<User> UserManager { get; }
    public RoleManager<Role> RoleManager { get; }
    public IEventService EventService { get; }
    public IUserLookupService UserLookupService { get; }
    public TopicService TopicService { get; }
    public EventValidator Validator { get; }

    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    public SqliteDbFixture()
    {
        // SQLite file-based (in-memory across a shared connection) so the
        // UserManager and DbContext see the same database.
        var dbPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.db");
        _connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new ApplicationDbContext(options);
        Db.Database.EnsureCreated();

        Validator = new EventValidator();
        UserManager = BuildUserManager();
        RoleManager = BuildRoleManager();
        EventService = new EventService(Db, Validator);
        UserLookupService = new UserLookupService(Db);
        TopicService = new TopicService(Db, UserManager);
    }

    private UserManager<User> BuildUserManager()
    {
        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<User, Role, ApplicationDbContext, int>(Db);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<User>();
        var userValidators = new IUserValidator<User>[] { new UserValidator<User>() };
        var passwordValidators = new IPasswordValidator<User>[] { new PasswordValidator<User>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();
        var logger = new LoggerFactory().CreateLogger<UserManager<User>>();

        return new UserManager<User>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            normalizer,
            describer,
            null!,
            logger);
    }

    private RoleManager<Role> BuildRoleManager()
    {
        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<Role, ApplicationDbContext, int>(Db);
        var roleValidators = new IRoleValidator<Role>[] { new RoleValidator<Role>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();
        var logger = new LoggerFactory().CreateLogger<RoleManager<Role>>();

        return new RoleManager<Role>(
            store,
            roleValidators,
            normalizer,
            describer,
            logger);
    }

    public void Dispose()
    {
        Db.Dispose();
        UserManager.Dispose();
        RoleManager.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition(nameof(SqliteDbCollection))]
public class SqliteDbCollection : ICollectionFixture<SqliteDbFixture>
{
}
