using Microsoft.AspNetCore.Components;

using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Client.Components.Pages;

public partial class EventDetail : ComponentBase
{
    [Parameter] public string Slug { get; set; } = string.Empty;
    [Inject] private IEventService EventService { get; set; } = default!;

    [PersistentState]
    public EventDetailDto? Event { get; set; }

    protected bool NotFound { get; set; }
    protected bool LoadFailed { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        try
        {
            Event ??= await EventService.GetPublishedBySlugAsync(Slug);
            NotFound = Event is null;
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