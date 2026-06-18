namespace UserGroupSiteGlm52.Data.Models;

/// <summary>
/// Records the (single) user who volunteered to present a topic suggestion.
/// A unique index on TopicSuggestionId enforces "at most one volunteer per topic".
/// </summary>
public class TopicVolunteer : EntityBase
{
    public int TopicSuggestionId { get; set; }

    public TopicSuggestion Topic { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime VolunteeredOn { get; set; }
}