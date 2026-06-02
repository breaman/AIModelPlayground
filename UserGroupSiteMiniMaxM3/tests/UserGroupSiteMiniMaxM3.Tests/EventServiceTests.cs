using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Shared.Models.Events;

using FluentAssertions;

namespace UserGroupSiteMiniMaxM3.Tests;

[Collection(nameof(SqliteDbCollection))]
public class EventServiceTests
{
    private readonly SqliteDbFixture _fx;

    public EventServiceTests(SqliteDbFixture fx) => _fx = fx;

    [Fact]
    public async Task CreateAsync_stores_event_and_returns_id()
    {
        var input = new EventEditDto
        {
            Title = "Test event",
            Slug = "test-event",
            ShortDescription = "short",
            Description = "long",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            Location = "online",
            IsPublished = false,
        };

        var id = await CreateEventAsAdminAsync(input);

        id.Should().BeGreaterThan(0);
        var ev = await _fx.Db.Events.FindAsync(id);
        ev.Should().NotBeNull();
        ev!.Title.Should().Be("Test event");
        ev.Slug.Should().Be("test-event");
        ev.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task ListPublishedAsync_hides_unpublished_events()
    {
        var speaker = await TestHelpers.CreateUserAsync(_fx, "speaker");

        await CreateEventAsAdminAsync(new EventEditDto
        {
            Title = "Published",
            Slug = "published",
            IsPublished = true,
            EventDateTime = DateTime.UtcNow.AddDays(1),
            Location = "x",
            Description = "d",
            SpeakerIds = [speaker.Id],
        });
        await CreateEventAsAdminAsync(new EventEditDto
        {
            Title = "Draft",
            Slug = "draft",
            IsPublished = false,
            EventDateTime = DateTime.UtcNow.AddDays(2),
            Location = "x",
            Description = "d",
            SpeakerIds = [speaker.Id],
        });

        var published = await _fx.EventService.ListPublishedAsync();

        published.Should().HaveCount(1);
        published[0].Title.Should().Be("Published");
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_slug()
    {
        await CreateEventAsAdminAsync(new EventEditDto
        {
            Title = "First",
            Slug = "shared",
            IsPublished = false,
        });

        var act = () => CreateEventAsAdminAsync(new EventEditDto
        {
            Title = "Second",
            Slug = "shared",
            IsPublished = false,
        });

        await act.Should().ThrowAsync<EventValidationException>();
    }

    private Task<int> CreateEventAsAdminAsync(EventEditDto dto)
    {
        var principal = TestHelpers.BuildAdminPrincipal(_fx);
        return _fx.EventService.CreateAsync(dto, principal);
    }
}
