using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteFable5.Shared.Dtos;

/// <summary>A topic suggestion with vote/volunteer state for the current user.</summary>
public class TopicSuggestionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SuggestedByName { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public bool CurrentUserVoted { get; set; }
    public string? VolunteerName { get; set; }
}

/// <summary>Payload for creating a new topic suggestion.</summary>
public class TopicCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}