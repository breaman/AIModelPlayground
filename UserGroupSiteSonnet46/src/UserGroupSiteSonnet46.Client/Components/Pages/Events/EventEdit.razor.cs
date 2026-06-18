using Microsoft.AspNetCore.Components;

using UserGroupSiteSonnet46.Client.Helpers;
using UserGroupSiteSonnet46.Shared.Services;

namespace UserGroupSiteSonnet46.Client.Components.Pages.Events;

/// <summary>
/// Create/edit form for events. Auto-populates Slug from Title on blur when Slug is empty.
/// Published events require description, date, location, and at least one speaker (validated via <see cref="EventEditModel"/>).
/// </summary>
public partial class EventEdit : ComponentBase
{
    [Parameter] public int? Id { get; set; }

    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    public EventEditModel? Model { get; set; }

    public List<UserDto>? AvailableSpeakers { get; set; }

    private string _descriptionTab = "edit";
    private bool _saving;

    protected Microsoft.AspNetCore.Components.MarkupString DescriptionPreview =>
        MarkdownRenderer.Render(Model?.Description);

    protected override async Task OnInitializedAsync()
    {
        AvailableSpeakers = await EventService.GetSpeakersAsync();

        if (Id.HasValue)
        {
            var ev = await EventService.GetEventByIdAsync(Id.Value);
            if (ev is null)
            {
                Navigation.NavigateTo("/not-found");
                return;
            }

            Model = new EventEditModel
            {
                Title = ev.Title,
                Slug = ev.Slug,
                ShortDescription = ev.ShortDescription,
                Description = ev.Description,
                EventDate = ev.EventDateTime.HasValue
                    ? DateOnly.FromDateTime(ev.EventDateTime.Value.LocalDateTime)
                    : null,
                EventTime = ev.EventDateTime.HasValue
                    ? TimeOnly.FromDateTime(ev.EventDateTime.Value.LocalDateTime)
                    : null,
                Location = ev.Location,
                IsPublished = ev.IsPublished,
                SpeakerIds = ev.Speakers.Select(s => s.Id).ToList()
            };
        }
        else
        {
            Model = new EventEditModel();
        }
    }

    private void OnTitleBlur()
    {
        if (Model is not null && string.IsNullOrWhiteSpace(Model.Slug) && !string.IsNullOrWhiteSpace(Model.Title))
        {
            Model.Slug = ToSlug(Model.Title);
        }
    }

    private void OnSpeakerToggled(int speakerId, Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        if (Model is null)
        {
            return;
        }

        var selected = e.Value is bool b ? b : bool.Parse(e.Value?.ToString() ?? "false");
        if (selected)
        {
            if (!Model.SpeakerIds.Contains(speakerId))
            {
                Model.SpeakerIds.Add(speakerId);
            }
        }
        else
        {
            Model.SpeakerIds.Remove(speakerId);
        }
    }

    private async Task HandleSubmit()
    {
        if (Model is null)
        {
            return;
        }

        _saving = true;

        try
        {
            var dto = new EventSaveDto(
                Id,
                Model.Title,
                Model.Slug,
                Model.ShortDescription,
                Model.Description,
                Model.CombinedEventDateTime,
                Model.Location,
                Model.IsPublished,
                Model.SpeakerIds);

            await EventService.SaveEventAsync(dto);
            ToastService.ShowSuccess("Event saved successfully.");
            Navigation.NavigateTo("/events");
        }
        catch (Exception)
        {
            ToastService.ShowError("Failed to save event. Please try again.");
            _saving = false;
        }
    }

    /// <summary>Converts a title string to a URL-friendly kebab-case slug.</summary>
    private static string ToSlug(string title) =>
        System.Text.RegularExpressions.Regex
            .Replace(title.ToLowerInvariant(), @"[^a-z0-9\s-]", string.Empty)
            .Trim()
            .Replace(' ', '-');
}