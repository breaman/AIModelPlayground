using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK27Code.Data.Models;
using UserGroupSiteKimiK27Code.Server.Services;
using UserGroupSiteKimiK27Code.Shared.Dtos;

namespace UserGroupSiteKimiK27Code.Tests;

public class ServerEventServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ServerEventService _service;
    private readonly UserManager<User> _userManager;

    public ServerEventServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _userManager = TestHelpers.CreateUserManager(_dbContext);
        _service = new ServerEventService(_dbContext, _userManager);
    }

    [Fact]
    public async Task CreateEventAsync_Saves_Published_Event_With_All_Fields()
    {
        var speaker = await TestHelpers.CreateSpeakerAsync(_dbContext, _userManager, "Jane", "Doe");

        var dto = new EventEditDto
        {
            Title = "Blazor Deep Dive",
            Slug = "blazor-deep-dive",
            ShortDescription = "A deep dive into Blazor.",
            Description = "# Blazor Deep Dive\n\nJoin us!",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Room A",
            IsPublished = true,
            SpeakerIds = [speaker.Id]
        };

        var result = await _service.CreateEventAsync(dto);

        Assert.True(result.Success);
        var evt = await _dbContext.Events.FindAsync(result.EventId);
        Assert.NotNull(evt);
        Assert.True(evt.IsPublished);
        Assert.Single(evt.EventSpeakers);
    }

    [Fact]
    public async Task CreateEventAsync_Fails_When_Slug_Exists()
    {
        var speaker = await TestHelpers.CreateSpeakerAsync(_dbContext, _userManager, "John", "Smith");
        await _service.CreateEventAsync(new EventEditDto
        {
            Title = "First",
            Slug = "same-slug",
            Description = "First event.",
            EventDate = DateTime.UtcNow,
            Location = "Room A",
            IsPublished = false,
            SpeakerIds = [speaker.Id]
        });

        var result = await _service.CreateEventAsync(new EventEditDto
        {
            Title = "Second",
            Slug = "same-slug",
            Description = "Second event.",
            EventDate = DateTime.UtcNow,
            Location = "Room B",
            IsPublished = false,
            SpeakerIds = [speaker.Id]
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Slug", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateEventAsync_Fails_When_Published_Without_Speakers()
    {
        var speaker = await TestHelpers.CreateSpeakerAsync(_dbContext, _userManager, "Alice", "Anderson");
        var createResult = await _service.CreateEventAsync(new EventEditDto
        {
            Title = "Draft Event",
            Slug = "draft-event",
            Description = "Draft.",
            EventDate = DateTime.UtcNow,
            Location = "Room A",
            IsPublished = false,
            SpeakerIds = [speaker.Id]
        });

        var updateResult = await _service.UpdateEventAsync(createResult.EventId!.Value, new EventEditDto
        {
            Id = createResult.EventId.Value,
            Title = "Draft Event",
            Slug = "draft-event",
            Description = "Draft.",
            EventDate = DateTime.UtcNow,
            Location = "Room A",
            IsPublished = true,
            SpeakerIds = []
        });

        Assert.False(updateResult.Success);
        Assert.Contains(updateResult.Errors, e => e.Contains("speaker", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }
}