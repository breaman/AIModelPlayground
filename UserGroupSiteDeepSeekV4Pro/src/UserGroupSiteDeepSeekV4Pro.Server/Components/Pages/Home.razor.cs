using Microsoft.AspNetCore.Components;

using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Server.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    private List<EventListItemDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        _events = await EventService.GetPublishedEventsAsync();
    }
}