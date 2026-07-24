using UserGroupSiteGpt56Sol.Shared.Models;

namespace UserGroupSiteGpt56Sol.Shared.Services;

/// <summary>Provides public and authorized event operations.</summary>
public interface IEventService
{
    Task<IReadOnlyList<EventSummaryDto>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<EventDetailDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventAdminListItemDto>> GetManageListAsync(CancellationToken cancellationToken = default);
    Task<EventEditRequest?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpeakerOptionDto>> GetSpeakerOptionsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> SaveAsync(int? id, EventEditRequest request,
        CancellationToken cancellationToken = default);
}