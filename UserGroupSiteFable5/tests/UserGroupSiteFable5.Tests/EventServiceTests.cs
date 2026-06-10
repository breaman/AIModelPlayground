using System.Security.Claims;

using UserGroupSiteFable5.Data.Models;
using UserGroupSiteFable5.Server.Services;
using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Xunit;

namespace UserGroupSiteFable5.Tests;

public class EventServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    private const int AdminId = 1;
    private const int AssignedSpeakerId = 2;
    private const int OtherSpeakerId = 3;
    private const int EventId = 1;

    public EventServiceTests()
    {
        _database.AddUser(AdminId, "admin@example.com");
        _database.AddUser(AssignedSpeakerId, "assigned@example.com");
        _database.AddUser(OtherSpeakerId, "other@example.com");

        _database.Context.Events.Add(new Event
        {
            Id = EventId,
            Title = "Existing Event",
            Slug = "existing-event",
            Speakers = [new EventSpeaker { UserId = AssignedSpeakerId }]
        });
        _database.Context.SaveChanges();
    }

    private EventService CreateService(int userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } };
        var context = _database.CreateContext(userId);

        return new EventService(context, accessor, new EventAuthorizationService(context));
    }

    private static EventEditDto CreateDraftDto(int id = EventId, string slug = "existing-event",
        List<int>? speakerIds = null)
    {
        return new EventEditDto
        {
            Id = id,
            Title = "Existing Event",
            Slug = slug,
            SpeakerUserIds = speakerIds ?? [AssignedSpeakerId]
        };
    }

    [Fact]
    public async Task GetEditableEvents_AdminSeesAll_SpeakerSeesAssignedOnly()
    {
        var adminEvents = await CreateService(AdminId, "Admin").GetEditableEventsAsync();
        var assignedEvents = await CreateService(AssignedSpeakerId, "Speaker").GetEditableEventsAsync();
        var otherEvents = await CreateService(OtherSpeakerId, "Speaker").GetEditableEventsAsync();

        Assert.Single(adminEvents);
        Assert.Single(assignedEvents);
        Assert.Empty(otherEvents);
    }

    [Fact]
    public async Task Update_ByUnassignedSpeaker_IsForbidden()
    {
        var result = await CreateService(OtherSpeakerId, "Speaker").UpdateEventAsync(CreateDraftDto());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Update_SpeakerChangingSpeakerList_IsForbidden()
    {
        var dto = CreateDraftDto(speakerIds: [AssignedSpeakerId, OtherSpeakerId]);

        var result = await CreateService(AssignedSpeakerId, "Speaker").UpdateEventAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Update_AdminChangingSpeakerList_Succeeds()
    {
        var dto = CreateDraftDto(speakerIds: [OtherSpeakerId]);

        var result = await CreateService(AdminId, "Admin").UpdateEventAsync(dto);

        Assert.True(result.Success);
        var speakerIds = await _database.Context.EventSpeakers
            .Where(es => es.EventId == EventId)
            .Select(es => es.UserId)
            .ToListAsync();
        Assert.Equal([OtherSpeakerId], speakerIds);
    }

    [Fact]
    public async Task Create_WithDuplicateSlug_FailsValidation()
    {
        var dto = CreateDraftDto(id: 0, slug: "existing-event");

        var result = await CreateService(AdminId, "Admin").CreateEventAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Contains("already in use", result.Error);
    }

    [Fact]
    public async Task Create_NormalizesSlugBeforeSaving()
    {
        var dto = CreateDraftDto(id: 0, slug: "My Fancy Event!");
        dto.Title = "My Fancy Event";

        var result = await CreateService(AdminId, "Admin").CreateEventAsync(dto);

        Assert.True(result.Success);
        Assert.True(await _database.Context.Events.AnyAsync(e => e.Slug == "my-fancy-event"));
    }

    [Fact]
    public async Task Create_ByNonAdmin_IsForbidden()
    {
        var dto = CreateDraftDto(id: 0, slug: "speaker-event");

        var result = await CreateService(AssignedSpeakerId, "Speaker").CreateEventAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}
