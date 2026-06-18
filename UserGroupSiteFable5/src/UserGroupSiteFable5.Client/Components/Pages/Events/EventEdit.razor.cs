using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Helpers;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Client.Components.Pages.Events;

public partial class EventEdit : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Parameter] public int? Id { get; set; }

    [PersistentState]
    public EventEditDto? Model { get; set; }

    [PersistentState]
    public List<SpeakerDto>? Speakers { get; set; }

    [PersistentState]
    public bool IsAdmin { get; set; }

    [PersistentState]
    public bool NotFound { get; set; }

    private bool _saving;

    private bool IsNew => Id is null;

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
        {
            var authState = await AuthenticationStateTask;
            IsAdmin = authState.User.IsInRole("Admin");
        }

        // Only admins may create events; speakers are sent back to their list.
        if (IsNew && !IsAdmin)
        {
            NavigationManager.NavigateTo("/events");

            return;
        }

        Speakers ??= await EventService.GetSpeakersAsync();

        if (Model is null && !NotFound)
        {
            if (IsNew)
            {
                Model = new EventEditDto();
            }
            else
            {
                Model = await EventService.GetEventForEditAsync(Id!.Value);
                NotFound = Model is null;
            }
        }
    }

    /// <summary>Auto-fills an empty slug from the title when the title field loses focus.</summary>
    private void OnTitleBlur()
    {
        if (Model is not null && string.IsNullOrWhiteSpace(Model.Slug))
        {
            Model.Slug = SlugHelper.GenerateSlug(Model.Title);
        }
    }

    private void ToggleSpeaker(int speakerId, bool selected)
    {
        if (Model is null)
        {
            return;
        }

        if (selected && !Model.SpeakerUserIds.Contains(speakerId))
        {
            Model.SpeakerUserIds.Add(speakerId);
        }
        else if (!selected)
        {
            Model.SpeakerUserIds.Remove(speakerId);
        }
    }

    private async Task SaveAsync()
    {
        if (Model is null)
        {
            return;
        }

        _saving = true;
        try
        {
            var result = IsNew
                ? await EventService.CreateEventAsync(Model)
                : await EventService.UpdateEventAsync(Model);

            if (result.Success)
            {
                ToastService.ShowSuccess($"\"{Model.Title}\" was saved.");
                NavigationManager.NavigateTo("/events");
            }
            else
            {
                ToastService.ShowError(result.Error ?? "Failed to save the event.");
            }
        }
        catch (Exception)
        {
            ToastService.ShowError("Something went wrong while saving. Please try again.");
        }
        finally
        {
            _saving = false;
        }
    }
}