using UserGroupSiteKimiK27Code.Shared.Dtos;

namespace UserGroupSiteKimiK27Code.Tests;

public class EventEditDtoTests
{
    [Fact]
    public void CanPublish_Returns_True_When_All_Fields_Present()
    {
        var dto = new EventEditDto
        {
            Title = "My Event",
            Slug = "my-event",
            Description = "A great event.",
            EventDate = DateTime.UtcNow,
            Location = "Main Hall",
            SpeakerIds = [1]
        };

        Assert.True(dto.CanPublish());
    }

    [Theory]
    [InlineData("", "my-event", "desc", "loc", true)]
    [InlineData("title", "", "desc", "loc", true)]
    [InlineData("title", "slug", "", "loc", true)]
    [InlineData("title", "slug", "desc", "", true)]
    [InlineData("title", "slug", "desc", "loc", false)]
    public void CanPublish_Returns_False_When_Required_Field_Missing(string title, string slug, string desc, string loc, bool hasSpeakers)
    {
        var dto = new EventEditDto
        {
            Title = title,
            Slug = slug,
            Description = desc,
            EventDate = DateTime.UtcNow,
            Location = loc,
            SpeakerIds = hasSpeakers ? [1] : []
        };

        Assert.False(dto.CanPublish());
    }
}