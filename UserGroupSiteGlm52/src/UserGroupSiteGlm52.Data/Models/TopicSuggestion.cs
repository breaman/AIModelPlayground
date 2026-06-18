using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGlm52.Data.Models;

/// <summary>
/// A community-suggested topic. Any logged-in user may suggest one, vote once,
/// and (if no one else has) volunteer to present it.
/// </summary>
public class TopicSuggestion : FingerPrintEntityBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    /// <summary>Optional Markdown description of the suggested topic.</summary>
    public string? Description { get; set; }

    public int SuggestedByUserId { get; set; }

    public User SuggestedByUser { get; set; } = null!;

    public ICollection<TopicVote> Votes { get; set; } = [];

    /// <summary>At most one volunteer per topic (1:1).</summary>
    public TopicVolunteer? Volunteer { get; set; }
}