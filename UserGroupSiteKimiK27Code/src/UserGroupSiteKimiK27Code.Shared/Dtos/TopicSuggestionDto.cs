using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteKimiK27Code.Shared.Dtos;

public class TopicSuggestionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public int SuggestedById { get; set; }
    public string SuggestedByName { get; set; } = "";

    public int? VolunteerSpeakerId { get; set; }
    public string? VolunteerSpeakerName { get; set; }

    public int VoteCount { get; set; }
    public bool HasVoted { get; set; }
}