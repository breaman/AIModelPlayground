using Microsoft.AspNetCore.Components;

using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Server.Components.Pages.Events;

public partial class EventDetail : ComponentBase
{
    [Parameter] public string Slug { get; set; } = "";

    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private EventDetailDto? _event;

    protected override async Task OnInitializedAsync()
    {
        _event = await EventService.GetEventBySlugAsync(Slug);
        if (_event is null)
        {
            NavigationManager.NavigateTo("/not-found");
        }
    }
}