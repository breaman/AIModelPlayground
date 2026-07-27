namespace UserGroupSiteOpus5.Data.Models;

/// <summary>
/// A single member's vote for a topic suggestion.
/// </summary>
/// <remarks>
/// The unique index over (<see cref="TopicSuggestionId"/>, <see cref="UserId"/>) is what actually
/// enforces "one vote per user per topic"; the service-layer duplicate check exists only to return
/// a friendly message instead of surfacing a constraint violation.
/// </remarks>
public class TopicSuggestionVote : FingerPrintEntityBase
{
    /// <summary>The suggestion being voted for.</summary>
    public int TopicSuggestionId { get; set; }

    /// <summary>Navigation to the suggestion.</summary>
    public TopicSuggestion TopicSuggestion { get; set; } = null!;

    /// <summary>The voting member.</summary>
    public int UserId { get; set; }

    /// <summary>Navigation to the voting member.</summary>
    public User User { get; set; } = null!;
}
