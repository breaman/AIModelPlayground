using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Server.Components.Pages.Admin;

public partial class EventEditor : ComponentBase
{
    [Inject]
    public IEventService EventService { get; set; } = default!;

    [Inject]
    public IUserManagementService UserManagementService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    [Parameter]
    public int? Id { get; set; }

    public bool IsEdit => Id.HasValue;
    public bool IsSaving { get; set; }
    public string EventTime { get; set; } = "18:00";
    public UserDto[] AvailableSpeakers { get; set; } = [];

    public CreateEventDto Model { get; set; } = new CreateEventDto
    {
        Title = "",
        Slug = null,
        ShortDescription = null,
        Description = null,
        EventDateTime = DateTime.Today.AddDays(7).AddHours(18),
        Location = null,
        IsPublished = false,
        SpeakerUserIds = []
    };

    protected override async Task OnInitializedAsync()
    {
        // Load speakers for the multi-select
        var users = await UserManagementService.GetAllUsersAsync();
        AvailableSpeakers = users.Where(u => u.IsSpeaker).ToArray();

        if (IsEdit && Id.HasValue)
        {
            var existingEvent = await EventService.GetEventByIdAsync(Id.Value);
            if (existingEvent != null)
            {
                Model = new CreateEventDto
                {
                    Title = existingEvent.Title,
                    Slug = existingEvent.Slug,
                    ShortDescription = existingEvent.ShortDescription,
                    Description = existingEvent.Description,
                    EventDateTime = existingEvent.EventDateTime,
                    Location = existingEvent.Location,
                    IsPublished = existingEvent.IsPublished,
                    SpeakerUserIds = existingEvent.Speakers.Select(s => s.UserId).ToArray()
                };

                EventTime = existingEvent.EventDateTime.ToString("HH:mm");
            }
        }
        else
        {
            // Set default date/time to next week at 6 PM
            var defaultDate = DateTime.Today.AddDays(7);
            var defaultTime = TimeSpan.Parse(EventTime);
            Model = new CreateEventDto
            {
                Title = "",
                Slug = null,
                ShortDescription = null,
                Description = null,
                EventDateTime = defaultDate + defaultTime,
                Location = null,
                IsPublished = false,
                SpeakerUserIds = []
            };
        }
    }

    private async Task GenerateSlug()
    {
        if (!string.IsNullOrWhiteSpace(Model.Title))
        {
            var slug = await EventService.GenerateSlugAsync(Model.Title);
            Model.Slug = slug;
        }
    }

    private void UpdateEventDateTime(ChangeEventArgs e)
    {
        if (TimeSpan.TryParse(e.Value?.ToString(), out var time))
        {
            EventTime = time.ToString(@"hh\:mm");
            Model.EventDateTime = Model.EventDateTime.Date + time;
        }
    }

    private async Task HandleValidSubmit()
    {
        IsSaving = true;

        try
        {
            // Validate published requirements
            if (Model.IsPublished)
            {
                if (string.IsNullOrWhiteSpace(Model.Description))
                {
                    ToastService.ShowError("Published events require a description");
                    return;
                }
                if (string.IsNullOrWhiteSpace(Model.Location))
                {
                    ToastService.ShowError("Published events require a location");
                    return;
                }
                if (Model.EventDateTime == default)
                {
                    ToastService.ShowError("Published events require a date/time");
                    return;
                }
                if (Model.SpeakerUserIds.Length == 0)
                {
                    ToastService.ShowError("Published events require at least one speaker");
                    return;
                }
            }

            if (IsEdit && Id.HasValue)
            {
                var updateDto = new UpdateEventDto
                {
                    Title = Model.Title,
                    Slug = Model.Slug,
                    ShortDescription = Model.ShortDescription,
                    Description = Model.Description,
                    EventDateTime = Model.EventDateTime,
                    Location = Model.Location,
                    IsPublished = Model.IsPublished,
                    SpeakerUserIds = Model.SpeakerUserIds
                };

                await EventService.UpdateEventAsync(Id.Value, updateDto);
                ToastService.ShowSuccess("Event updated successfully");
                NavigationManager.NavigateTo($"/event/{Model.Slug}");
            }
            else
            {
                var createdEvent = await EventService.CreateEventAsync(Model);
                ToastService.ShowSuccess("Event created successfully");
                NavigationManager.NavigateTo($"/event/{createdEvent.Slug}");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error saving event: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }
}