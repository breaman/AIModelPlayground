using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Client.Components.Pages.Events;

public partial class EventDetail : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public string Slug { get; set; } = "";

    protected EventDto? _event;
    protected bool _isLoading = true;
    protected string? _renderedMarkdown;

    protected override async Task OnInitializedAsync()
    {
        _event ??= await EventService.GetEventBySlugAsync(Slug);
        _isLoading = false;

        if (_event?.Description is not null)
        {
            _renderedMarkdown = await JS.InvokeAsync<string>("renderMarkdown", _event.Description);
        }
    }
}
