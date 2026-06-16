using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteKimiK27Code.Data.Models;

public class TopicSuggestion : FingerPrintEntityBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public int SuggestedById { get; set; }
    public User SuggestedBy { get; set; } = default!;

    public int? VolunteerSpeakerId { get; set; }
    public User? VolunteerSpeaker { get; set; }

    public ICollection<TopicVote> Votes { get; set; } = [];
}