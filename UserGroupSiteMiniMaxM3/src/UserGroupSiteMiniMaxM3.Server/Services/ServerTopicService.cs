using UserGroupSiteMiniMaxM3.Shared.Models.Topics;
using UserGroupSiteMiniMaxM3.Shared.Services;

namespace UserGroupSiteMiniMaxM3.Server.Services;

/// <summary>
/// Server-side implementation of <see cref="ITopicService"/>. Wraps the
/// data-layer <see cref="Data.Services.TopicService"/> and resolves the
/// current user id and admin flag from the request context.
/// </summary>
public class ServerTopicService(
    Data.Services.TopicService inner,
    Data.Interfaces.IUserService userService) : ITopicService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<TopicSummaryDto>> ListAsync(string? currentUserId, CancellationToken cancellationToken = default)
        => inner.ListAsync(ResolveUserIdString(), cancellationToken);

    /// <inheritdoc />
    public Task<TopicSummaryDto> CreateAsync(TopicCreateDto input, string currentUserId, CancellationToken cancellationToken = default)
        => inner.CreateAsync(input, ResolveUserIdString() ?? currentUserId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ToggleVoteAsync(int topicId, string currentUserId, CancellationToken cancellationToken = default)
        => inner.ToggleVoteAsync(topicId, ResolveUserIdString() ?? currentUserId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ToggleVolunteerAsync(int topicId, string currentUserId, CancellationToken cancellationToken = default)
        => inner.ToggleVolunteerAsync(topicId, ResolveUserIdString() ?? currentUserId, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(int topicId, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
        => inner.DeleteAsync(topicId, ResolveUserIdString() ?? currentUserId, isAdmin, cancellationToken);

    /// <summary>
    /// Translates the int identity stored in <see cref="Data.Interfaces.IUserService"/>
    /// into the string id that the data-layer service expects. The int-id user
    /// is the same physical row — the string form is just <c>ToString()</c>.
    /// </summary>
    private string? ResolveUserIdString()
    {
        var id = userService.UserId;
        return id == 0 ? null : id.ToString();
    }
}