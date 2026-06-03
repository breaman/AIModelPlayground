using UserGroupSiteSonnet46.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteSonnet46.Server.Components.Pages;

/// <summary>
/// Public home page rendered server-side (SSR). No InteractiveWebAssembly needed
/// since the page is read-only and benefits from fast initial load.
/// </summary>
public partial class Home : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    public List<EventDto>? Events { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Events = await EventService.GetPublishedEventsAsync();
    }
}
