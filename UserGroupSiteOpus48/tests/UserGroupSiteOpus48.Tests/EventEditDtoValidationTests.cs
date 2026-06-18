using System.ComponentModel.DataAnnotations;

using UserGroupSiteOpus48.Shared.Dtos;

using Xunit;

namespace UserGroupSiteOpus48.Tests;

public class EventEditDtoValidationTests
{
    private static List<ValidationResult> Validate(EventEditDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void UnpublishedEventWithOnlyTitleAndSlugIsValid()
    {
        var dto = new EventEditDto { Title = "Draft", Slug = "draft", IsPublished = false };

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void PublishedEventMissingRequiredFieldsReportsEachRule()
    {
        var dto = new EventEditDto { Title = "T", Slug = "t", IsPublished = true };

        var members = Validate(dto).SelectMany(r => r.MemberNames).ToList();

        Assert.Contains(nameof(EventEditDto.Description), members);
        Assert.Contains(nameof(EventEditDto.EventDateTime), members);
        Assert.Contains(nameof(EventEditDto.Location), members);
        Assert.Contains(nameof(EventEditDto.SpeakerUserIds), members);
    }

    [Fact]
    public void PublishedEventWithAllRequiredFieldsIsValid()
    {
        var dto = new EventEditDto
        {
            Title = "Conf",
            Slug = "conf",
            Description = "Details",
            EventDateTime = DateTime.UtcNow,
            Location = "HQ",
            IsPublished = true,
            SpeakerUserIds = [1]
        };

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void MissingTitleAndSlugAreRequired()
    {
        var dto = new EventEditDto { Title = "", Slug = "" };

        var members = Validate(dto).SelectMany(r => r.MemberNames).ToList();

        Assert.Contains(nameof(EventEditDto.Title), members);
        Assert.Contains(nameof(EventEditDto.Slug), members);
    }
}