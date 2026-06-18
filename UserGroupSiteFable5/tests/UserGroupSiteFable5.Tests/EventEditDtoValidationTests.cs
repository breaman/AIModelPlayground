using System.ComponentModel.DataAnnotations;

using UserGroupSiteFable5.Shared.Dtos;

using Xunit;

namespace UserGroupSiteFable5.Tests;

public class EventEditDtoValidationTests
{
    private static List<ValidationResult> Validate(EventEditDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);

        return results;
    }

    private static EventEditDto CreateValidDraft()
    {
        return new EventEditDto { Title = "Intro to Blazor", Slug = "intro-to-blazor" };
    }

    [Fact]
    public void Draft_RequiresOnlyTitleAndSlug()
    {
        var results = Validate(CreateValidDraft());

        Assert.Empty(results);
    }

    [Fact]
    public void Draft_MissingTitleAndSlug_Fails()
    {
        var results = Validate(new EventEditDto());

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventEditDto.Title)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventEditDto.Slug)));
    }

    [Theory]
    [InlineData("Has Spaces")]
    [InlineData("UPPER-case")]
    [InlineData("trailing-")]
    [InlineData("-leading")]
    public void Slug_RejectsInvalidFormats(string slug)
    {
        var dto = CreateValidDraft();
        dto.Slug = slug;

        var results = Validate(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventEditDto.Slug)));
    }

    [Fact]
    public void Published_RequiresDescriptionDateLocationAndSpeaker()
    {
        var dto = CreateValidDraft();
        dto.IsPublished = true;

        var results = Validate(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventEditDto.Description)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventEditDto.StartsAt)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventEditDto.Location)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventEditDto.SpeakerUserIds)));
    }

    [Fact]
    public void Published_WithAllRequirements_Passes()
    {
        var dto = CreateValidDraft();
        dto.IsPublished = true;
        dto.Description = "A talk about Blazor.";
        dto.StartsAt = new DateTime(2026, 7, 1, 18, 0, 0);
        dto.Location = "Community Hall";
        dto.SpeakerUserIds = [42];

        var results = Validate(dto);

        Assert.Empty(results);
    }
}