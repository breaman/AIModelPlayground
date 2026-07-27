using System.ComponentModel.DataAnnotations;

using UserGroupSiteOpus5.Shared.Common;

namespace UserGroupSiteOpus5.Shared.Models;

/// <summary>A topic suggestion as shown in the list.</summary>
/// <param name="Id">The suggestion id.</param>
/// <param name="Title">The topic title.</param>
/// <param name="Description">Optional elaboration.</param>
/// <param name="SuggestedByName">Display name of the member who suggested it.</param>
/// <param name="VolunteerUserId">The volunteering member's id, or null while the slot is open.</param>
/// <param name="VolunteerName">Display name of the volunteering member, if any.</param>
/// <param name="VoteCount">How many members have voted for it.</param>
/// <param name="HasCurrentUserVoted">
/// Whether the requesting member has already voted. Carried on the DTO so the list can disable the
/// vote button without a second round trip per row.
/// </param>
public record TopicSuggestionListItem(
    int Id,
    string Title,
    string? Description,
    string SuggestedByName,
    int? VolunteerUserId,
    string? VolunteerName,
    int VoteCount,
    bool HasCurrentUserVoted);

/// <summary>The form-bound model for suggesting a new topic.</summary>
public class TopicSuggestionCreateModel
{
    /// <summary>The topic title.</summary>
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(FieldLengths.TopicTitle)]
    [Display(Name = "Title")]
    public string Title { get; set; } = "";

    /// <summary>Optional plain-text elaboration.</summary>
    [MaxLength(FieldLengths.Description)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}
