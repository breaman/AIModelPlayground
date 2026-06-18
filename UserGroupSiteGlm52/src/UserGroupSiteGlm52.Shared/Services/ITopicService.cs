using UserGroupSiteGlm52.Shared.Models;

namespace UserGroupSiteGlm52.Shared.Services;

/// <summary>
/// Community topic suggestions: suggest, vote (once per user), and volunteer
/// (once per topic). All actions require a logged-in user.
/// </summary>
public interface ITopicService
{
    /// <summary>All topics with the current user's vote/volunteer state.</summary>
    Task<IReadOnlyList<TopicDto>> GetTopicsAsync();

    Task<ServiceResult<TopicDto>> SuggestAsync(TopicInputDto dto);

    /// <summary>Vote on a topic. Fails if the user has already voted.</summary>
    Task<ServiceResult> VoteAsync(int id);

    /// <summary>Remove the current user's vote from a topic.</summary>
    Task<ServiceResult> UnvoteAsync(int id);

    /// <summary>Volunteer to present a topic. Fails if a volunteer already exists.</summary>
    Task<ServiceResult> VolunteerAsync(int id);
}