using UserGroupSiteDeepSeekV4Pro.Shared.Models;

namespace UserGroupSiteDeepSeekV4Pro.Tests;

public class EventValidationTests
{
    [Fact]
    public void ValidateEvent_TitleRequired_ReturnsError()
    {
        var dto = new EventDto
        {
            Title = "",
            Slug = "test-event",
            IsPublished = false
        };

        var (isValid, error) = ValidateEvent(dto);
        Assert.False(isValid);
        Assert.Contains("Title", error);
    }

    [Fact]
    public void ValidateEvent_SlugRequired_ReturnsError()
    {
        var dto = new EventDto
        {
            Title = "Test Event",
            Slug = "",
            IsPublished = false
        };

        var (isValid, error) = ValidateEvent(dto);
        Assert.False(isValid);
        Assert.Contains("Slug", error);
    }

    [Fact]
    public void ValidateEvent_PublishedEventRequiresDescription()
    {
        var dto = new EventDto
        {
            Title = "Test Event",
            Slug = "test-event",
            Description = null,
            IsPublished = true,
            EventDateTime = DateTimeOffset.Now,
            Location = "Room A",
            SpeakerUserIds = [1]
        };

        var (isValid, error) = ValidateEvent(dto);
        Assert.False(isValid);
        Assert.Contains("Description", error);
    }

    [Fact]
    public void ValidateEvent_PublishedEventRequiresDateTime()
    {
        var dto = new EventDto
        {
            Title = "Test Event",
            Slug = "test-event",
            Description = "Test description",
            IsPublished = true,
            EventDateTime = default,
            Location = "Room A",
            SpeakerUserIds = [1]
        };

        var (isValid, error) = ValidateEvent(dto);
        Assert.False(isValid);
        Assert.Contains("date/time", error);
    }

    [Fact]
    public void ValidateEvent_PublishedEventRequiresLocation()
    {
        var dto = new EventDto
        {
            Title = "Test Event",
            Slug = "test-event",
            Description = "Test description",
            IsPublished = true,
            EventDateTime = DateTimeOffset.Now,
            Location = null,
            SpeakerUserIds = [1]
        };

        var (isValid, error) = ValidateEvent(dto);
        Assert.False(isValid);
        Assert.Contains("Location", error);
    }

    [Fact]
    public void ValidateEvent_PublishedEventRequiresSpeakers()
    {
        var dto = new EventDto
        {
            Title = "Test Event",
            Slug = "test-event",
            Description = "Test description",
            IsPublished = true,
            EventDateTime = DateTimeOffset.Now,
            Location = "Room A",
            SpeakerUserIds = []
        };

        var (isValid, error) = ValidateEvent(dto);
        Assert.False(isValid);
        Assert.Contains("speaker", error);
    }

    [Fact]
    public void ValidateEvent_ValidPublishedEvent_ReturnsSuccess()
    {
        var dto = new EventDto
        {
            Title = "Test Event",
            Slug = "test-event",
            Description = "Test description",
            IsPublished = true,
            EventDateTime = DateTimeOffset.Now,
            Location = "Room A",
            SpeakerUserIds = [1]
        };

        var (isValid, error) = ValidateEvent(dto);
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateEvent_ValidDraftEvent_ReturnsSuccess()
    {
        var dto = new EventDto
        {
            Title = "Test Event",
            Slug = "test-event",
            IsPublished = false
        };

        var (isValid, error) = ValidateEvent(dto);
        Assert.True(isValid);
        Assert.Null(error);
    }

    private static (bool IsValid, string? ErrorMessage) ValidateEvent(EventDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return (false, "Title is required.");

        if (string.IsNullOrWhiteSpace(dto.Slug))
            return (false, "Slug is required.");

        if (dto.IsPublished)
        {
            if (string.IsNullOrWhiteSpace(dto.Description))
                return (false, "Description is required when publishing.");

            if (dto.EventDateTime == default)
                return (false, "Event date/time is required when publishing.");

            if (string.IsNullOrWhiteSpace(dto.Location))
                return (false, "Location is required when publishing.");

            if (dto.SpeakerUserIds.Count == 0)
                return (false, "At least one speaker is required when publishing.");
        }

        return (true, null);
    }
}
