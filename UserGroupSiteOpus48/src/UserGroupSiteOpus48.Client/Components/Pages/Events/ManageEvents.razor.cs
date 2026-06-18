using Microsoft.AspNetCore.Components;

using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Client.Components.Pages.Events;

/// <summary>
/// Editor entry point listing all events including unpublished drafts. Admins can create events;
/// admins and assigned speakers can edit (per-event permission enforced server-side).
/// </summary>
public partial class ManageEvents : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    /// <summary>All events; persisted across pre-render/hydration.</summary>
    [PersistentState]
    public EventListItemDto[]? Events { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Events ??= await EventService.GetAllEventsAsync();
    }
}