using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteGpt56Sol.Shared.Authorization;
using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Client.Components.Pages;

public partial class ManageEvents : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [PersistentState]
    public IReadOnlyList<EventAdminListItemDto>? Events { get; set; }

    protected bool IsAdmin { get; set; }
    protected bool LoadFailed { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            Events ??= await EventService.GetManageListAsync();
            if (AuthenticationStateTask is not null)
            {
                IsAdmin = (await AuthenticationStateTask).User.IsInRole(AppRoles.Admin);
            }
        }
        catch (HttpRequestException)
        {
            LoadFailed = true;
        }
    }

    /// <summary>Formats a stored UTC timestamp in the visitor's local timezone.</summary>
    protected static string FormatDate(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime().ToString("g");
}