using System.ComponentModel.DataAnnotations;

using UserGroupSiteGpt56Sol.Shared.Models;

namespace UserGroupSiteGpt56Sol.Tests;

public sealed class EventEditRequestTests
{
    [Fact]
    public void DraftRequiresOnlyTitleAndSlug()
    {
        var request = new EventEditRequest { Title = "Draft", Slug = "draft" };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void PublishedEventRequiresAllPublishFields()
    {
        var request = new EventEditRequest { Title = "Published", Slug = "published", IsPublished = true };

        var errors = Validate(request);

        Assert.Contains(errors, result => result.MemberNames.Contains(nameof(request.DescriptionMarkdown)));
        Assert.Contains(errors, result => result.MemberNames.Contains(nameof(request.StartsAtUtc)));
        Assert.Contains(errors, result => result.MemberNames.Contains(nameof(request.Location)));
        Assert.Contains(errors, result => result.MemberNames.Contains(nameof(request.SpeakerIds)));
    }

    [Fact]
    public void CompletePublishedEventPassesValidation()
    {
        var request = new EventEditRequest
        {
            Title = "Published",
            Slug = "published",
            DescriptionMarkdown = "Details",
            StartsAtUtc = DateTime.UtcNow,
            Location = "Online",
            IsPublished = true,
            SpeakerIds = [42]
        };

        Assert.Empty(Validate(request));
    }

    /// <summary>Runs complete DataAnnotations validation for an event request.</summary>
    private static List<ValidationResult> Validate(EventEditRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }
}
