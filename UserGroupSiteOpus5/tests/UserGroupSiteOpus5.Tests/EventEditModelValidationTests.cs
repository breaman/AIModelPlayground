using System.ComponentModel.DataAnnotations;

using UserGroupSiteOpus5.Shared.Models;

namespace UserGroupSiteOpus5.Tests;

public class EventEditModelValidationTests
{
    [Fact]
    public void ADraftNeedsOnlyATitleAndSlug()
    {
        var model = new EventEditModel { Title = "Draft meeting", Slug = "draft-meeting" };

        Validate(model).ShouldBeEmpty();
    }

    [Fact]
    public void TitleIsRequired()
    {
        var model = new EventEditModel { Title = "", Slug = "some-slug" };

        Validate(model).ShouldContain("Title is required.");
    }

    [Fact]
    public void SlugIsRequired()
    {
        var model = new EventEditModel { Title = "Some title", Slug = "" };

        Validate(model).ShouldContain("Slug is required.");
    }

    [Fact]
    public void PublishingRequiresADescription()
    {
        var model = PublishableModel();
        model.Description = null;

        Validate(model).ShouldContain("A description is required to publish an event.");
    }

    [Fact]
    public void PublishingRequiresADate()
    {
        var model = PublishableModel();
        model.EventDateTime = null;

        Validate(model).ShouldContain("A date and time is required to publish an event.");
    }

    [Fact]
    public void PublishingRequiresALocation()
    {
        var model = PublishableModel();
        model.Location = null;

        Validate(model).ShouldContain("A location is required to publish an event.");
    }

    [Fact]
    public void PublishingRequiresAtLeastOneSpeaker()
    {
        var model = PublishableModel();
        model.SpeakerUserIds = [];

        Validate(model).ShouldContain("At least one speaker is required to publish an event.");
    }

    [Fact]
    public void ACompletePublishedEventPasses()
    {
        Validate(PublishableModel()).ShouldBeEmpty();
    }

    [Fact]
    public void PublishPreconditionsDoNotApplyToDrafts()
    {
        var model = PublishableModel();
        model.IsPublished = false;
        model.Description = null;
        model.EventDateTime = null;
        model.Location = null;
        model.SpeakerUserIds = [];

        Validate(model).ShouldBeEmpty();
    }

    private static EventEditModel PublishableModel()
    {
        return new EventEditModel
        {
            Title = "Complete meeting",
            Slug = "complete-meeting",
            Description = "All about the thing.",
            EventDateTime = DateTimeOffset.UtcNow.AddDays(7),
            Location = "Community Hall",
            IsPublished = true,
            SpeakerUserIds = [1]
        };
    }

    /// <summary>
    /// Runs the same validation Blazor performs on submit: every property attribute plus the
    /// model's own IValidatableObject rules.
    /// </summary>
    private static List<string> Validate(EventEditModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.Select(x => x.ErrorMessage ?? "").ToList();
    }
}
