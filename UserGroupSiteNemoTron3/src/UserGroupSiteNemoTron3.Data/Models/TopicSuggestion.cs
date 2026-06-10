using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteNemoTron3.Data.Models;

public class TopicSuggestion : FingerPrintEntityBase
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? VolunteerSpeakerId { get; set; }
    public User? VolunteerSpeaker { get; set; }

    public ICollection<TopicVote> Votes { get; set; } = new List<TopicVote>();
}