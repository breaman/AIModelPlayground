using System.Globalization;

using Microsoft.AspNetCore.Components;

using UserGroupSiteGpt55.Shared.Events;
using UserGroupSiteGpt55.Shared.Markdown;

namespace UserGroupSiteGpt55.Client.Components.Pages.Events;

public partial class EventEditor : ComponentBase
{
    [Parameter]
    public int? EventId { get; set; }

    [Inject]
    private IEventService EventService { get; set; } = default!;

    [Inject]
    private IMarkdownRenderer MarkdownRenderer { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    protected EventEditModel? Model { get; set; }

    protected List<SpeakerOption> Speakers { get; set; } = [];

    protected List<string> Errors { get; set; } = [];

    protected string ActiveTab { get; set; } = "edit";

    protected bool IsSaving { get; set; }

    protected bool IsNew => EventId is null;

    protected MarkupString PreviewHtml => new(MarkdownRenderer.Render(Model?.MarkdownDescription));

    protected string StartsAtInput
    {
        get => Model?.StartsAt?.ToLocalTime().ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture) ?? "";
        set
        {
            if (Model is null)
            {
                return;
            }

            Model.StartsAt = DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
                ? new DateTimeOffset(parsed)
                : null;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        Model = EventId is null
            ? await EventService.CreateDraftAsync()
            : await EventService.GetEventForEditAsync(EventId.Value);

        if (Model is not null)
        {
            await LoadSpeakersAsync();
        }
    }

    protected Task GenerateSlugAsync()
    {
        if (Model is not null && string.IsNullOrWhiteSpace(Model.Slug))
        {
            Model.Slug = SlugGenerator.Generate(Model.Title);
        }

        return Task.CompletedTask;
    }

    protected void SetTab(string tabName)
    {
        ActiveTab = tabName;
    }

    protected void ToggleSpeaker(int speakerId, ChangeEventArgs args)
    {
        if (Model is null)
        {
            return;
        }

        var isSelected = args.Value is true || args.Value?.ToString() == "true";
        if (isSelected && !Model.SpeakerIds.Contains(speakerId))
        {
            Model.SpeakerIds.Add(speakerId);
        }
        else if (!isSelected)
        {
            Model.SpeakerIds.Remove(speakerId);
        }
    }

    protected async Task ValidateAsync()
    {
        if (Model is null)
        {
            return;
        }

        Errors = (await EventService.ValidateEventAsync(Model)).ToList();
    }

    protected async Task SaveAsync()
    {
        if (Model is null)
        {
            return;
        }

        IsSaving = true;
        Errors.Clear();
        var result = await EventService.SaveEventAsync(Model);
        IsSaving = false;

        if (!result.Succeeded)
        {
            Errors = result.Errors.ToList();
            return;
        }

        NavigationManager.NavigateTo($"/admin/events/{result.EventId}/edit");
    }

    private async Task LoadSpeakersAsync()
    {
        Speakers = (await EventService.GetSpeakerOptionsAsync(Model?.SpeakerIds ?? [])).ToList();
    }
}