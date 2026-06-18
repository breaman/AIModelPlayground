using Microsoft.AspNetCore.Components;

using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Server.Components.Pages.Admin;

public partial class EventList : ComponentBase
{
    [Inject]
    public IEventService EventService { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [PersistentState]
    public EventDto[]? Events { get; set; }

    public bool ShowDeleteConfirm { get; set; }
    public EventDto? EventToDelete { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Events ??= await EventService.GetPublishedEventsAsync();
    }

    private void ConfirmDelete(EventDto evt)
    {
        EventToDelete = evt;
        ShowDeleteConfirm = true;
    }

    private void CancelDelete()
    {
        ShowDeleteConfirm = false;
        EventToDelete = null;
    }

    private async Task DeleteConfirmed()
    {
        if (EventToDelete != null)
        {
            try
            {
                await EventService.DeleteEventAsync(EventToDelete.Id);
                ToastService.ShowSuccess("Event deleted successfully");
                Events = await EventService.GetPublishedEventsAsync();
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Error deleting event: {ex.Message}");
            }
        }
        CancelDelete();
    }
}