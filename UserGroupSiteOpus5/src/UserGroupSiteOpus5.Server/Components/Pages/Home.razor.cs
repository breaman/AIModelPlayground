using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteOpus5.Server.Components.Pages;

/// <summary>
/// The public meeting listing.
/// </summary>
/// <remarks>
/// Static server rendering: the page is read-only, reachable anonymously, and the content is worth
/// indexing, so there is nothing to gain from shipping it through the WebAssembly runtime.
/// </remarks>
public partial class Home : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    /// <summary>Published meetings, most recent first. Null while loading.</summary>
    private IReadOnlyList<EventListItem>? Events { get; set; }

    /// <summary>Roles that see the "manage events" affordance.</summary>
    private static string ManagerRoles => $"{RoleNames.Admin},{RoleNames.Speaker}";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Events = await EventService.GetPublishedEventsAsync();
    }
}
