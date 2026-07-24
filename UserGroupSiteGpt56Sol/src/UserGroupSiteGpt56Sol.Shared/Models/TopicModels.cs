using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGpt56Sol.Shared.Models;

/// <summary>Displays a topic suggestion and the current user's relationship to it.</summary>
public sealed record TopicSuggestionDto(int Id, string Title, string? SupportingDetails,
    string CreatorName, DateTime CreatedOnUtc, int VoteCount, bool HasCurrentUserVote,
    string? VolunteerName);

/// <summary>Represents a request to suggest a topic.</summary>
public sealed class CreateTopicRequest
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4_000)]
    public string? SupportingDetails { get; set; }
}
