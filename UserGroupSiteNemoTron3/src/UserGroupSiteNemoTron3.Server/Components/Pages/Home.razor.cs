using Microsoft.AspNetCore.Components;

using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Server.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject]
    public IEventService EventService { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    [PersistentState]
    public EventDto[]? Events { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Events ??= await EventService.GetPublishedEventsAsync();
    }
}