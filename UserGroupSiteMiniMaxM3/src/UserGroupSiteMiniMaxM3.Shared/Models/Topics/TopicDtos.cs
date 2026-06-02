using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteMiniMaxM3.Shared.Models.Topics;

/// <summary>
/// Read-only topic view for the listing page. Includes aggregate counts
/// and the requesting user's vote/volunteer state.
/// </summary>
/// <param name="Id">Topic id.</param>
/// <param name="Title">Topic title.</param>
/// <param name="Description">Optional longer markdown description.</param>
/// <param name="SuggestedByUserId">User who proposed the topic.</param>
/// <param name="SuggestedByDisplayName">Display name of the suggester.</param>
/// <param name="CreatedAt">UTC timestamp of the suggestion.</param>
/// <param name="VoteCount">Total number of votes.</param>
/// <param name="VolunteerCount">Total number of volunteer offers.</param>
/// <param name="UserHasVoted">True if the requesting user has voted.</param>
/// <param name="UserHasVolunteered">True if the requesting user has volunteered.</param>
public record TopicSummaryDto(
    int Id,
    string Title,
    string? Description,
    int SuggestedByUserId,
    string SuggestedByDisplayName,
    DateTime CreatedAt,
    int VoteCount,
    int VolunteerCount,
    bool UserHasVoted,
    bool UserHasVolunteered);

/// <summary>Edit-shape DTO used by the create form.</summary>
public class TopicCreateDto
{
    /// <summary>Topic title.</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer markdown description.</summary>
    public string? Description { get; set; }
}