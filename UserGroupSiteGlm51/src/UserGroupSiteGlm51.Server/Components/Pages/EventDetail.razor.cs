using Microsoft.AspNetCore.Components;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;

namespace UserGroupSiteGlm51.Server.Components.Pages;

/// <summary>
/// Event detail page — publicly visible, shows event information, speakers, and
/// an edit link for authorized users (Admin or assigned Speaker).
/// </summary>
public partial class EventDetail : ComponentBase
{
    [Parameter]
    public string Slug { get; set; } = string.Empty;

    [Inject]
    private IEventService EventService { get; set; } = default!;

    [Inject]
    private IUserService UserService { get; set; } = default!;

    /// <summary>
    /// The event being displayed.
    /// </summary>
    public Event? EventItem { get; set; }

    /// <summary>
    /// Whether the current user can edit this event (Admin or assigned Speaker).
    /// </summary>
    public bool CanEdit { get; set; }

    protected override async Task OnInitializedAsync()
    {
        EventItem = await EventService.GetEventBySlugAsync(Slug);

        if (EventItem is not null)
        {
            try
            {
                CanEdit = await EventService.IsEditorAsync(EventItem.Id, UserService.UserId);
            }
            catch
            {
                // Non-authenticated users can't edit
                CanEdit = false;
            }
        }
    }
}