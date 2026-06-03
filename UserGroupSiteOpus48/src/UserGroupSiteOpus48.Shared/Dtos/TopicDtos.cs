using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteOpus48.Shared.Dtos;

/// <summary>A topic suggestion with aggregated vote/volunteer info for the current user.</summary>
public record TopicSuggestionDto(
    int Id,
    string Title,
    string? Description,
    string? DescriptionHtml,
    int VoteCount,
    bool CurrentUserHasVoted,
    int? VolunteerUserId,
    string? VolunteerName,
    string SuggestedByName);

/// <summary>Payload for suggesting a new topic.</summary>
public class TopicCreateDto
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional Markdown details.</summary>
    public string? Description { get; set; }
}
