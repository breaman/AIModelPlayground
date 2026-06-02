using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserGroupSiteMiniMaxM3.Data.Models;

/// <summary>
/// A topic proposed by a user. Other users can upvote and/or volunteer
/// to present the topic at a future event. One vote / one volunteer
/// per user per topic.
/// </summary>
public class TopicSuggestion : FingerPrintEntityBase
{
    /// <summary>Title shown in the list.</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer markdown body describing the topic.</summary>
    public string? Description { get; set; }

    /// <summary>The user who proposed this topic.</summary>
    [Required]
    public int SuggestedByUserId { get; set; }

    /// <summary>Navigation to the suggesting user.</summary>
    [ForeignKey(nameof(SuggestedByUserId))]
    public User? SuggestedByUser { get; set; }

    /// <summary>All votes cast on this topic (one per user).</summary>
    public ICollection<TopicVote> Votes { get; set; } = [];

    /// <summary>All volunteers willing to present this topic (one per user).</summary>
    public ICollection<TopicVolunteer> Volunteers { get; set; } = [];
}