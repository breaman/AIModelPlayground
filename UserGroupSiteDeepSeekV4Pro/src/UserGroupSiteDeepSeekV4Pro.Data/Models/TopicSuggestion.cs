using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public class TopicSuggestion : FingerPrintEntityBase
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }

    public int? SuggestedByUserId { get; set; }
    public User? SuggestedByUser { get; set; }
    public int? VolunteerUserId { get; set; }
    public User? VolunteerUser { get; set; }
    public ICollection<TopicVote> Votes { get; set; } = [];
}
