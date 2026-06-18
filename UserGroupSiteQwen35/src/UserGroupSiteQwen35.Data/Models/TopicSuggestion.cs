using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteQwen35.Data.Models;

public class TopicSuggestion : EntityBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public int SuggestedByUserId { get; set; }

    public User? SuggestedByUser { get; set; }

    public int? VolunteerSpeakerId { get; set; }

    public Speaker? VolunteerSpeaker { get; set; }

    public DateTime CreatedOn { get; set; }

    public int CreatedBy { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<TopicVote>? TopicVotes { get; set; }
}