using Microsoft.AspNetCore.Components;

using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Server.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    private List<EventListItemDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        _events = await EventService.GetPublishedEventsAsync();
    }
}