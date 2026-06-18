using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteFable5.Data.Interfaces;
using UserGroupSiteFable5.Data.Models;

namespace UserGroupSiteFable5.Tests;

/// <summary>A fixed-identity stand-in for the HTTP-context-based user service.</summary>
public class FakeUserService(int userId) : IUserService
{
    public int UserId => userId;
}

/// <summary>
/// An in-memory SQLite database (relational, so composite keys and unique indexes are
/// enforced like production) holding an <see cref="ApplicationDbContext"/> for tests.
/// The connection must stay open for the lifetime of the database, hence IDisposable.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Context { get; }

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    /// <summary>A second context over the same database, scoped to the given current user.</summary>
    public ApplicationDbContext CreateContext(int currentUserId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ApplicationDbContext(options, new FakeUserService(currentUserId));
    }

    public User AddUser(int id, string email, string? firstName = null, string? lastName = null)
    {
        var user = new User
        {
            Id = id,
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            MemberSince = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        Context.Users.Add(user);
        Context.SaveChanges();

        return user;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}