using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteOpus5.Client.Components.Pages;

/// <summary>
/// Lists the meetings the signed-in user may edit: all of them for an administrator, or only their
/// own assignments for a speaker.
/// </summary>
/// <remarks>
/// Interactive WebAssembly, pre-rendered on the server. <see cref="Events"/> carries
/// <c>[PersistentState]</c> so the list serialised during pre-rendering is reused on hydration
/// instead of being fetched a second time.
/// </remarks>
public partial class ManageEvents : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    /// <summary>Meetings the user may manage. Null while loading.</summary>
    [PersistentState]
    public IReadOnlyList<EventListItem>? Events { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        // Only fetch when pre-rendered state was not restored.
        if (Events is not null)
        {
            return;
        }

        try
        {
            Events = await EventService.GetManageableEventsAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Events = [];
            ToastService.ShowError("Could not load your meetings. Try refreshing the page.");
        }
    }
}
