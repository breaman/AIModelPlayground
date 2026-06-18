using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteKimiK26.Data.Models;

public class TopicSuggestion : FingerPrintEntityBase
{
    [MaxLength(200)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public int SuggestedByUserId { get; set; }
    public User SuggestedByUser { get; set; } = null!;

    public int? VolunteerUserId { get; set; }
    public User? VolunteerUser { get; set; }

    public ICollection<TopicVote> Votes { get; set; } = [];
}