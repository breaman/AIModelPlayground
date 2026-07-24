using Microsoft.AspNetCore.Components;

using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Client.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;

    [PersistentState]
    public IReadOnlyList<EventSummaryDto>? Events { get; set; }

    protected bool LoadFailed { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            Events ??= await EventService.GetPublishedAsync();
        }
        catch (HttpRequestException)
        {
            LoadFailed = true;
        }
    }

    /// <summary>Formats a stored UTC timestamp in the visitor's local timezone.</summary>
    protected static string FormatDate(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime().ToString("f");
}