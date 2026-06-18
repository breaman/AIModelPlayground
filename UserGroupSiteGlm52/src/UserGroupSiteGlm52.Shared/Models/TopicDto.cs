using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGlm52.Shared.Models;

/// <summary>Topic suggestion as seen by the current user, including their vote/volunteer state.</summary>
public sealed record TopicDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string SuggestedByName { get; init; } = "";
    public int VoteCount { get; init; }
    public bool HasCurrentUserVoted { get; init; }

    /// <summary>Display name of the volunteer, if any.</summary>
    public string? VolunteerName { get; init; }

    public bool HasCurrentUserVolunteered { get; init; }
}

/// <summary>Mutable input for suggesting a topic.</summary>
public sealed class TopicInputDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    /// <summary>Optional Markdown description.</summary>
    public string? Description { get; set; }
}