using Microsoft.AspNetCore.Components;

using UserGroupSiteMiniMaxM3.Data.Services;
using UserGroupSiteMiniMaxM3.Shared.Models.Events;

namespace UserGroupSiteMiniMaxM3.Server.Components.Pages;

/// <summary>
/// Landing page. Shows a hero card for the next upcoming event plus a list
/// of any further events. Server-rendered so it works without authentication.
/// </summary>
public partial class Home : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    private IReadOnlyList<EventSummaryDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        // Service returns upcoming (now or future) events ordered ascending.
        _events = await EventService.ListPublishedAsync();
    }
}