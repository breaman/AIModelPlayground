namespace UserGroupSiteOpus5.Data.Models;

/// <summary>
/// A topic any member would like to see presented at a future meeting. Members vote on suggestions
/// and one member may volunteer to present it.
/// </summary>
public class TopicSuggestion : FingerPrintEntityBase
{
    /// <summary>Short title of the topic.</summary>
    public string Title { get; set; } = "";

    /// <summary>Optional plain-text elaboration of what the member would like covered.</summary>
    public string? Description { get; set; }

    /// <summary>The member who suggested the topic.</summary>
    public int SuggestedByUserId { get; set; }

    /// <summary>Navigation to the suggesting member.</summary>
    public User SuggestedByUser { get; set; } = null!;

    /// <summary>The member who volunteered to present it, or null while the slot is open.</summary>
    public int? VolunteerUserId { get; set; }

    /// <summary>Navigation to the volunteering member, if any.</summary>
    public User? VolunteerUser { get; set; }

    /// <summary>Votes cast for this suggestion, at most one per member.</summary>
    public ICollection<TopicSuggestionVote> Votes { get; set; } = [];
}
