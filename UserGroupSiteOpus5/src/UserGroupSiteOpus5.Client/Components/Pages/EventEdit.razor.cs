using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteOpus5.Client.Components.Pages;

/// <summary>
/// Creates and edits a meeting.
/// </summary>
/// <remarks>
/// Interactive WebAssembly: the form has a live Markdown preview, slug generation on blur, and a
/// running readiness hint, none of which work under static rendering.
/// </remarks>
public partial class EventEdit : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The meeting id from the route; null when creating.</summary>
    [Parameter]
    public int? Id { get; set; }

    /// <summary>The form-bound meeting. Null while loading.</summary>
    [PersistentState]
    public EventEditModel? Model { get; set; }

    /// <summary>Members holding the Speaker role. Null while loading.</summary>
    [PersistentState]
    public IReadOnlyList<EventSpeakerInfo>? AvailableSpeakers { get; set; }

    /// <summary>Whether the meeting could not be loaded, or is not editable by this user.</summary>
    private bool NotFound { get; set; }

    /// <summary>Whether the description pane is showing the rendered preview.</summary>
    private bool IsPreviewing { get; set; }

    /// <summary>Whether a save is in flight.</summary>
    private bool IsSaving { get; set; }

    /// <summary>Whether this is a create rather than an edit.</summary>
    private bool IsNew => Id is null or 0;

    /// <summary>
    /// The title as it was when the slug was last generated, used so a second blur without an
    /// edit does not re-query the server.
    /// </summary>
    private string _slugSourceTitle = "";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (AvailableSpeakers is null)
        {
            try
            {
                AvailableSpeakers = await EventService.GetAvailableSpeakersAsync();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                AvailableSpeakers = [];
                ToastService.ShowError("Could not load the speaker list.");
            }
        }

        if (Model is not null)
        {
            return;
        }

        if (IsNew)
        {
            Model = new EventEditModel();
            return;
        }

        try
        {
            Model = await EventService.GetEventForEditAsync(Id!.Value);
            NotFound = Model is null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            NotFound = true;
            ToastService.ShowError("Could not load this meeting.");
        }
    }

    /// <summary>
    /// Fills the slug from the title when the title loses focus.
    /// </summary>
    /// <remarks>
    /// Only runs while the slug field is empty. Overwriting a slug the user typed would silently
    /// change a published meeting's address, and existing links would break.
    /// </remarks>
    private async Task OnTitleBlurAsync()
    {
        if (Model is null || !string.IsNullOrWhiteSpace(Model.Slug) || string.IsNullOrWhiteSpace(Model.Title))
        {
            return;
        }

        if (Model.Title == _slugSourceTitle)
        {
            return;
        }

        try
        {
            Model.Slug = await EventService.GenerateUniqueSlugAsync(Model.Title, IsNew ? null : Id);
            _slugSourceTitle = Model.Title;
            StateHasChanged();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Fall back to the local generator: a slug the user can edit beats an empty field, and
            // the server checks uniqueness on save regardless.
            Model.Slug = SlugGenerator.Generate(Model.Title);
            StateHasChanged();
        }
    }

    /// <summary>Clears the slug-generation memo so a re-edited title can regenerate the slug.</summary>
    private void OnTitleChanged()
    {
        if (Model is not null && Model.Title != _slugSourceTitle)
        {
            _slugSourceTitle = "";
        }
    }

    /// <summary>Adds or removes a speaker assignment.</summary>
    private void ToggleSpeaker(int userId, bool isSelected)
    {
        if (Model is null)
        {
            return;
        }

        if (isSelected)
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

    /// <summary>
    /// Lists what is still missing before the meeting can be published, so the hint next to the
    /// toggle tells the user before they submit rather than after.
    /// </summary>
    private List<string> MissingForPublish()
    {
        if (Model is null)
        {
            return [];
        }

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Model.Description))
        {
            missing.Add("a description");
        }

        if (Model.EventDateTime is null)
        {
            missing.Add("a date and time");
        }

        if (string.IsNullOrWhiteSpace(Model.Location))
        {
            missing.Add("a location");
        }

        if (Model.SpeakerUserIds.Count == 0)
        {
            missing.Add("at least one speaker");
        }

        return missing;
    }

    /// <summary>Saves the meeting and navigates to its page on success.</summary>
    private async Task SaveAsync()
    {
        if (Model is null || IsSaving)
        {
            return;
        }

        IsSaving = true;

        try
        {
            var result = await EventService.SaveEventAsync(Model);

            if (!result.Succeeded)
            {
                ToastService.ShowError(string.Join(" ", result.Errors), "Could not save");
                return;
            }

            ToastService.ShowSuccess($"Saved “{Model.Title}”.");
            Navigation.NavigateTo($"/events/{Model.Slug}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ToastService.ShowError("The meeting could not be saved. Check your connection and try again.");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
