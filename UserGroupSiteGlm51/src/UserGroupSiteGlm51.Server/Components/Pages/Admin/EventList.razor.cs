using Microsoft.AspNetCore.Components;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;
using UserGroupSiteGlm51.Shared.Services;

namespace UserGroupSiteGlm51.Server.Components.Pages.Admin;

/// <summary>
/// Admin page listing all events (published and unpublished) with edit/delete actions.
/// </summary>
public partial class EventList : ComponentBase
{
    [Inject]
    private IEventService EventService { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// All events (published and unpublished) for admin management.
    /// </summary>
    public IEnumerable<Event>? AllEvents { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AllEvents = await EventService.GetAllEventsAsync();
    }

    private async Task DeleteEvent(int eventId)
    {
        await EventService.DeleteEventAsync(eventId);
        ToastService.ShowSuccess("Event deleted successfully.");
        AllEvents = await EventService.GetAllEventsAsync();
    }
}