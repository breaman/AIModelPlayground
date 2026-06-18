using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteFable5.Data.Models;

/// <summary>
/// A community-suggested talk topic. Members vote via <see cref="TopicVote"/> and at most
/// one user may volunteer to present the topic (<see cref="VolunteerUserId"/>).
/// </summary>
public class TopicSuggestion : FingerPrintEntityBase
{
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int SuggestedByUserId { get; set; }
    public User SuggestedByUser { get; set; } = null!;

    /// <summary>The single user who volunteered to present this topic, if anyone has.</summary>
    public int? VolunteerUserId { get; set; }
    public User? VolunteerUser { get; set; }

    public ICollection<TopicVote> Votes { get; set; } = [];
}