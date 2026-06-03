using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;
using UserGroupSiteOpus48.Shared.Text;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteOpus48.Client.Components.Pages.Events;

/// <summary>
/// Create/edit form for an event. Reachable at <c>/events/new</c> (create, admins) and
/// <c>/events/edit/{id}</c> (edit, admins or assigned speakers). Client-side validation mirrors the
/// server publish rules via <see cref="EventEditDto"/>'s IValidatableObject; the server re-validates.
/// </summary>
public partial class EditEvent : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Event id when editing; null when creating.</summary>
    [Parameter] public int? Id { get; set; }

    /// <summary>The editable model; persisted across pre-render/hydration.</summary>
    [PersistentState]
    public EventEditDto? Model { get; set; }

    /// <summary>Users assignable as speakers; persisted across pre-render/hydration.</summary>
    [PersistentState]
    public SpeakerDto[]? Speakers { get; set; }

    /// <summary>True once initial load has run, so a missing edit target can show "not found".</summary>
    private bool _loaded;

    /// <summary>Active Markdown tab: "edit" or "preview".</summary>
    private string _descriptionTab = "edit";

    private bool _saving;

    private bool IsCreate => Id is null;

    protected override async Task OnInitializedAsync()
    {
        Speakers ??= await EventService.GetAssignableSpeakersAsync();

        if (Model is null)
        {
            Model = IsCreate
                ? new EventEditDto()
                : await EventService.GetEventForEditAsync(Id!.Value);
        }

        _loaded = true;
    }

    /// <summary>Auto-fills the slug with the kebab-cased title when the slug is still empty.</summary>
    private void OnTitleBlur()
    {
        if (Model is not null && string.IsNullOrWhiteSpace(Model.Slug))
        {
            Model.Slug = SlugGenerator.ToKebabCase(Model.Title);
        }
    }

    private void ShowEditTab() => _descriptionTab = "edit";

    private void ShowPreviewTab() => _descriptionTab = "preview";

    /// <summary>Toggles a speaker's membership in the model's selection.</summary>
    private void ToggleSpeaker(int userId, bool selected)
    {
        if (Model is null)
        {
            return;
        }

        if (selected)
        {
            if (!Model.SpeakerUserIds.Contains(userId))
            {
                Model.SpeakerUserIds.Add(userId);
            }
        }
        else
        {
            Model.SpeakerUserIds.Remove(userId);
        }
    }

    /// <summary>Saves via create or update, shows a toast, and navigates to the saved event on success.</summary>
    private async Task OnValidSubmitAsync()
    {
        if (Model is null)
        {
            return;
        }

        _saving = true;
        try
        {
            var result = IsCreate
                ? await EventService.CreateEventAsync(Model)
                : await EventService.UpdateEventAsync(Model);

            if (result.Success && result.Value is not null)
            {
                ToastService.ShowSuccess($"Event '{Model.Title}' saved.");
                Navigation.NavigateTo($"/events/{result.Value.Slug}");
            }
            else
            {
                ToastService.ShowError(string.Join(" ", result.Errors));
            }
        }
        finally
        {
            _saving = false;
        }
    }
}
