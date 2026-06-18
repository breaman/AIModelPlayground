using Microsoft.AspNetCore.Components;

using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Client.Components.Pages.Events;

public partial class EventList : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected List<EventListItemDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        _events ??= await EventService.GetAllEventsAsync();
    }

    private async Task DeleteEvent(int id)
    {
        try
        {
            await EventService.DeleteEventAsync(id);
            _events = await EventService.GetAllEventsAsync();
            ToastService.ShowSuccess("Event deleted successfully.");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to delete event: {ex.Message}");
        }
    }
}