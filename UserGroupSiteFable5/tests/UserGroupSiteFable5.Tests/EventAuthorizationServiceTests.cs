using System.Security.Claims;

using UserGroupSiteFable5.Data.Models;
using UserGroupSiteFable5.Server.Services;

using Xunit;

namespace UserGroupSiteFable5.Tests;

public class EventAuthorizationServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly EventAuthorizationService _service;

    private const int AssignedSpeakerId = 10;
    private const int OtherSpeakerId = 11;
    private const int AdminId = 12;
    private const int EventId = 1;

    public EventAuthorizationServiceTests()
    {
        _service = new EventAuthorizationService(_database.Context);

        _database.AddUser(AssignedSpeakerId, "assigned@example.com");
        _database.AddUser(OtherSpeakerId, "other@example.com");
        _database.AddUser(AdminId, "admin@example.com");

        _database.Context.Events.Add(new Event
        {
            Id = EventId,
            Title = "Test Event",
            Slug = "test-event",
            Speakers = [new EventSpeaker { UserId = AssignedSpeakerId }]
        });
        _database.Context.SaveChanges();
    }

    private static ClaimsPrincipal CreatePrincipal(int userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public async Task Admin_CanEditAnyEvent()
    {
        var admin = CreatePrincipal(AdminId, "Admin");

        Assert.True(await _service.CanEditEventAsync(admin, EventId));
    }

    [Fact]
    public async Task AssignedSpeaker_CanEditTheirEvent()
    {
        var speaker = CreatePrincipal(AssignedSpeakerId, "Speaker");

        Assert.True(await _service.CanEditEventAsync(speaker, EventId));
    }

    [Fact]
    public async Task UnassignedSpeaker_CannotEditEvent()
    {
        var speaker = CreatePrincipal(OtherSpeakerId, "Speaker");

        Assert.False(await _service.CanEditEventAsync(speaker, EventId));
    }

    [Fact]
    public async Task UserWithoutRoles_CannotEditEvent()
    {
        var member = CreatePrincipal(AssignedSpeakerId);

        Assert.False(await _service.CanEditEventAsync(member, EventId));
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}
