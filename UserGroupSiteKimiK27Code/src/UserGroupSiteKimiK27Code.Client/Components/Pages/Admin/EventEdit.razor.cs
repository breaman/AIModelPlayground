using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using UserGroupSiteKimiK27Code.Shared;
using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Client.Components.Pages.Admin;

public partial class EventEdit : ComponentBase
{
    [Parameter] public int? Id { get; set; }

    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    private EventEditDto? _event;
    private List<SpeakerDto>? _speakers;
    private bool _isLoading = true;
    private bool _isSaving;
    private bool _slugManuallyEdited;

    private bool IsNew => Id is null;

    protected override async Task OnInitializedAsync()
    {
        _speakers = await EventService.GetSpeakersAsync();

        if (IsNew)
        {
            _event = new EventEditDto
            {
                EventDate = DateTime.UtcNow.AddDays(30)
            };
        }
        else
        {
            _event = await EventService.GetEventForEditAsync(Id!.Value);
            if (_event is null)
            {
                ToastService.ShowError("Event not found.");
                NavigationManager.NavigateTo("/admin/events");
                return;
            }

            _slugManuallyEdited = true;
        }

        _isLoading = false;
    }

    private async Task GenerateSlugAsync(FocusEventArgs e)
    {
        if (_event is null) return;
        if (!string.IsNullOrWhiteSpace(_event.Slug) && _slugManuallyEdited) return;

        _event.Slug = SlugHelper.ToSlug(_event.Title);
        _slugManuallyEdited = false;
        await Task.CompletedTask;
    }

    private void ToggleSpeaker(int speakerId, object? checkedValue)
    {
        if (_event is null) return;

        var isChecked = checkedValue is bool b && b;
        if (isChecked && !_event.SpeakerIds.Contains(speakerId))
        {
            _event.SpeakerIds.Add(speakerId);
        }
        else if (!isChecked && _event.SpeakerIds.Contains(speakerId))
        {
            _event.SpeakerIds.Remove(speakerId);
        }
    }

    private async Task SaveAsync()
    {
        if (_event is null) return;

        _isSaving = true;
        EventEditResult result;

        if (IsNew)
        {
            result = await EventService.CreateEventAsync(_event);
        }
        else
        {
            result = await EventService.UpdateEventAsync(Id!.Value, _event);
        }

        _isSaving = false;

        if (result.Success)
        {
            ToastService.ShowSuccess("Event saved successfully.");
            NavigationManager.NavigateTo("/admin/events");
        }
        else
        {
            ToastService.ShowError(string.Join(" ", result.Errors));
        }
    }
}