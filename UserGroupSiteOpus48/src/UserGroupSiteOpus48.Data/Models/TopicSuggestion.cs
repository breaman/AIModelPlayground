using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteOpus48.Data.Models;

/// <summary>
/// A topic a member suggests for a future meeting. Members vote on topics and a single
/// member may volunteer to speak on it. Auditable via <see cref="FingerPrintEntityBase"/>.
/// </summary>
public class TopicSuggestion : FingerPrintEntityBase
{
    /// <summary>Title of the suggested topic. Required.</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional details stored as Markdown.</summary>
    public string? Description { get; set; }

    /// <summary>The single volunteer speaker, if any has stepped forward.</summary>
    public int? VolunteerUserId { get; set; }

    /// <summary>The user who volunteered to present this topic.</summary>
    public User? Volunteer { get; set; }

    /// <summary>FK to the user who suggested the topic.</summary>
    public int SuggestedByUserId { get; set; }

    /// <summary>The user who suggested the topic.</summary>
    public User SuggestedBy { get; set; } = null!;

    /// <summary>Votes cast for this topic (one per user, enforced by a unique index).</summary>
    public ICollection<TopicVote> Votes { get; set; } = new List<TopicVote>();
}