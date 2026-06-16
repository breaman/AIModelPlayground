using UserGroupSiteGpt55.Shared.Events;

namespace UserGroupSiteGpt55.Tests;

public sealed class EventValidationTests
{
    [Fact]
    public void Validate_DraftRequiresTitleAndSlugOnly()
    {
        var errors = EventValidation.Validate(new EventEditModel());

        Assert.Contains("Title is required.", errors);
        Assert.Contains("Slug is required.", errors);
        Assert.DoesNotContain("Location is required before publishing.", errors);
    }

    [Fact]
    public void Validate_PublishedEventRequiresDescriptionDateLocationAndSpeaker()
    {
        var errors = EventValidation.Validate(new EventEditModel
        {
            Title = "Monthly Meetup",
            Slug = "monthly-meetup",
            IsPublished = true
        });

        Assert.Contains("Description is required before publishing.", errors);
        Assert.Contains("Date and time are required before publishing.", errors);
        Assert.Contains("Location is required before publishing.", errors);
        Assert.Contains("At least one speaker is required before publishing.", errors);
    }
}
