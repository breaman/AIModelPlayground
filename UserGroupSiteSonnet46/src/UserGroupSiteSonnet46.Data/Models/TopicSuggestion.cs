using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteSonnet46.Data.Models;

/// <summary>
/// A topic submitted by a community member for future events.
/// A user may volunteer to present the topic, represented by <see cref="VolunteerUserId"/>.
/// </summary>
public class TopicSuggestion : FingerPrintEntityBase
{
    [MaxLength(200)]
    public required string Title { get; set; }

    public string? Description { get; set; }

    public int SuggestedById { get; set; }

    public User SuggestedBy { get; set; } = null!;

    /// <summary>Null until a user volunteers to present this topic.</summary>
    public int? VolunteerUserId { get; set; }

    public User? Volunteer { get; set; }

    public ICollection<TopicVote> Votes { get; set; } = [];
}