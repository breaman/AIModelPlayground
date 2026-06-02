using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGlm51.Data.Models;

/// <summary>
/// Represents a topic suggestion submitted by a user for potential future events.
/// Other users can vote on topics and volunteer to present them.
/// </summary>
public class TopicSuggestion : FingerPrintEntityBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the topic in Markdown format.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The user who suggested this topic.
    /// </summary>
    public int SuggestedById { get; set; }

    /// <summary>
    /// The user who volunteered to present this topic, or null if no one has volunteered yet.
    /// First-come-first-served: only one volunteer per topic.
    /// </summary>
    public int? VolunteerId { get; set; }

    /// <summary>
    /// Navigation property to the user who suggested the topic.
    /// </summary>
    public User SuggestedBy { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user who volunteered to present.
    /// </summary>
    public User? Volunteer { get; set; }

    /// <summary>
    /// Navigation property for votes on this topic.
    /// </summary>
    public ICollection<TopicVote> Votes { get; set; } = [];
}