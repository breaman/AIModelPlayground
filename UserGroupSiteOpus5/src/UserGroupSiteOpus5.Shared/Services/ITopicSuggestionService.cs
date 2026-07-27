using UserGroupSiteOpus5.Shared.Models;

namespace UserGroupSiteOpus5.Shared.Services;

/// <summary>
/// Reads and writes member topic suggestions. Implemented on both the server and the client.
/// </summary>
public interface ITopicSuggestionService
{
    /// <summary>
    /// Returns all suggestions, most-voted first, with the current member's vote state resolved.
    /// </summary>
    Task<IReadOnlyList<TopicSuggestionListItem>> GetSuggestionsAsync();

    /// <summary>Records a new suggestion attributed to the current member.</summary>
    /// <param name="model">The suggestion to create.</param>
    /// <returns>The new suggestion's id on success.</returns>
    Task<SaveResult<int>> CreateSuggestionAsync(TopicSuggestionCreateModel model);

    /// <summary>
    /// Casts the current member's vote for a suggestion. Rejects a second vote from the same
    /// member.
    /// </summary>
    /// <param name="suggestionId">The suggestion to vote for.</param>
    Task<SaveResult> VoteAsync(int suggestionId);

    /// <summary>
    /// Claims the presenter slot for a suggestion on behalf of the current member. Rejects the
    /// claim when someone else got there first.
    /// </summary>
    /// <param name="suggestionId">The suggestion to volunteer for.</param>
    Task<SaveResult> VolunteerAsync(int suggestionId);
}
