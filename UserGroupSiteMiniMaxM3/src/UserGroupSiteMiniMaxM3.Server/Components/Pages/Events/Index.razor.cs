using Microsoft.AspNetCore.Components;

using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Shared.Models.Events;

namespace UserGroupSiteMiniMaxM3.Server.Components.Pages.Events;

public partial class Index : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    private IReadOnlyList<EventSummaryDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        _events = await EventService.ListPublishedAsync();
    }
}