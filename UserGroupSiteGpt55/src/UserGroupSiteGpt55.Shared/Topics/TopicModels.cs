using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGpt55.Shared.Topics;

public enum TopicSuggestionStatus
{
    Open = 0,
    Planned = 1,
    Archived = 2
}

/// <summary>
/// Provides topic suggestion data for list views.
/// </summary>
public sealed record TopicSuggestionItem(
    int Id,
    string Title,
    string Description,
    string SuggestedBy,
    string? VolunteerSpeaker,
    int VoteCount,
    bool CurrentUserVoted,
    bool CurrentUserIsVolunteer,
    TopicSuggestionStatus Status,
    DateTimeOffset CreatedOn);

/// <summary>
/// Provides editable topic suggestion input.
/// </summary>
public sealed class TopicSuggestionCreateModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = "";
}

/// <summary>
/// Provides the result of topic mutations.
/// </summary>
public sealed record TopicActionResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static TopicActionResult Success()
    {
        return new TopicActionResult(true, []);
    }

    public static TopicActionResult Failure(params string[] errors)
    {
        return new TopicActionResult(false, errors);
    }
}

/// <summary>
/// Defines shared topic suggestion operations.
/// </summary>
public interface ITopicSuggestionService
{
    Task<IReadOnlyList<TopicSuggestionItem>> GetSuggestionsAsync(CancellationToken cancellationToken = default);

    Task<TopicActionResult> CreateSuggestionAsync(TopicSuggestionCreateModel model, CancellationToken cancellationToken = default);

    Task<TopicActionResult> VoteAsync(int topicSuggestionId, CancellationToken cancellationToken = default);

    Task<TopicActionResult> RemoveVoteAsync(int topicSuggestionId, CancellationToken cancellationToken = default);

    Task<TopicActionResult> VolunteerAsync(int topicSuggestionId, CancellationToken cancellationToken = default);
}
