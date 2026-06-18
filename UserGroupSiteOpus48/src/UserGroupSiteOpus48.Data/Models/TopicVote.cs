namespace UserGroupSiteOpus48.Data.Models;

/// <summary>
/// A single up-vote by a <see cref="User"/> for a <see cref="TopicSuggestion"/>.
/// A composite unique index on (TopicSuggestionId, UserId) enforces one vote per user per topic.
/// </summary>
public class TopicVote : FingerPrintEntityBase
{
    /// <summary>FK to the voted topic.</summary>
    public int TopicSuggestionId { get; set; }

    /// <summary>The topic that was voted for.</summary>
    public TopicSuggestion TopicSuggestion { get; set; } = null!;

    /// <summary>FK to the voting user.</summary>
    public int UserId { get; set; }

    /// <summary>The user who cast the vote.</summary>
    public User User { get; set; } = null!;
}