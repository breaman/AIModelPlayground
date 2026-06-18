using Microsoft.AspNetCore.Components;

using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Server.Components.Pages;

/// <summary>Public home page: lists published events, newest first.</summary>
public partial class Home : ComponentBase
{
    [Inject]
    private IEventService EventService { get; set; } = default!;

    public IReadOnlyList<EventSummaryDto>? Events { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Static SSR — fetch once during the request. Published events are ordered
        // by EventDate descending by the service.
        Events = await EventService.GetPublishedEventsAsync();
    }
}