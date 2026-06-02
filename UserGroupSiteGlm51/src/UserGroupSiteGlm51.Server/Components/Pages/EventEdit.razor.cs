using System.ComponentModel.DataAnnotations;
using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;
using UserGroupSiteGlm51.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteGlm51.Server.Components.Pages;

/// <summary>
/// Event create/edit page. Admin users can create new events; Admin or assigned speakers can edit.
/// </summary>
public partial class EventEdit : ComponentBase
{
    [Parameter]
    public int? Id { get; set; }

    [Inject]
    private IEventService EventService { get; set; } = default!;

    [Inject]
    private IUserService UserService { get; set; } = default!;

    [Inject]
    private IUserManagementService UserManagementService { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Whether this is a new event (vs editing an existing one).
    /// </summary>
    public bool IsNew => Id is null || Id == 0;

    /// <summary>
    /// Whether the page is currently loading data.
    /// </summary>
    public bool IsLoading { get; set; } = true;

    /// <summary>
    /// The edit model bound to the form.
    /// </summary>
    public EventEditModel EditModel { get; set; } = new();

    /// <summary>
    /// Users in the Speaker role available to assign.
    /// </summary>
    public List<SpeakerInfo> AvailableSpeakers { get; set; } = [];

    /// <summary>
    /// Validation errors specifically for publishing requirements.
    /// </summary>
    public List<string> PublishErrors { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        // Load available speakers
        var allUsers = await UserManagementService.GetAllUsersAsync();
        foreach (var user in allUsers)
        {
            var roles = await UserManagementService.GetUserRolesAsync(user.Id);
            if (roles.Contains("Speaker"))
            {
                AvailableSpeakers.Add(new SpeakerInfo(user.Id, user.FirstName, user.LastName));
            }
        }

        if (!IsNew)
        {
            var eventEntity = await EventService.GetEventByIdAsync(Id!.Value);
            if (eventEntity is not null)
            {
                EditModel = new EventEditModel
                {
                    Title = eventEntity.Title,
                    Slug = eventEntity.Slug,
                    ShortDescription = eventEntity.ShortDescription ?? string.Empty,
                    Description = eventEntity.Description ?? string.Empty,
                    EventDateString = eventEntity.EventDate?.ToString("yyyy-MM-ddTHH:mm"),
                    Location = eventEntity.Location ?? string.Empty,
                    IsPublished = eventEntity.IsPublished,
                    SpeakerIds = eventEntity.EventSpeakers.Select(es => es.UserId).ToList()
                };
            }
        }

        IsLoading = false;
    }

    private async Task HandleValidSubmit()
    {
        PublishErrors.Clear();

        // If publishing, validate that all required fields are present
        if (EditModel.IsPublished)
        {
            if (string.IsNullOrWhiteSpace(EditModel.Description))
            {
                PublishErrors.Add("Description is required for published events.");
            }

            if (string.IsNullOrWhiteSpace(EditModel.EventDateString))
            {
                PublishErrors.Add("Event date is required for published events.");
            }

            if (string.IsNullOrWhiteSpace(EditModel.Location))
            {
                PublishErrors.Add("Location is required for published events.");
            }

            if (!EditModel.SpeakerIds.Any())
            {
                PublishErrors.Add("At least one speaker is required for published events.");
            }

            if (PublishErrors.Any())
            {
                return;
            }
        }

        DateTimeOffset? eventDate = null;
        if (!string.IsNullOrWhiteSpace(EditModel.EventDateString) && DateTimeOffset.TryParse(EditModel.EventDateString, out var parsedDate))
        {
            eventDate = parsedDate;
        }

        var eventEntity = new Event
        {
            Title = EditModel.Title,
            Slug = EditModel.Slug,
            ShortDescription = string.IsNullOrWhiteSpace(EditModel.ShortDescription) ? null : EditModel.ShortDescription,
            Description = string.IsNullOrWhiteSpace(EditModel.Description) ? null : EditModel.Description,
            EventDate = eventDate,
            Location = string.IsNullOrWhiteSpace(EditModel.Location) ? null : EditModel.Location,
            IsPublished = EditModel.IsPublished
        };

        try
        {
            if (IsNew)
            {
                eventEntity.EventSpeakers = EditModel.SpeakerIds.Select(id => new EventSpeaker { UserId = id }).ToList();
                var created = await EventService.CreateEventAsync(eventEntity);
                ToastService.ShowSuccess("Event created successfully.");
                Navigation.NavigateTo("/admin/events");
            }
            else
            {
                eventEntity.Id = Id!.Value;
                eventEntity.EventSpeakers = EditModel.SpeakerIds.Select(id => new EventSpeaker { UserId = id, EventId = Id.Value }).ToList();
                await EventService.UpdateEventAsync(eventEntity);
                ToastService.ShowSuccess("Event updated successfully.");
                Navigation.NavigateTo("/admin/events");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error saving event: {ex.Message}");
        }
    }

    private void AutoGenerateSlug()
    {
        if (string.IsNullOrWhiteSpace(EditModel.Slug) && !string.IsNullOrWhiteSpace(EditModel.Title))
        {
            EditModel.Slug = EditModel.Title.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("--", "-")
                .Trim('-');
        }
    }

    private void ToggleSpeaker(int userId, bool isChecked)
    {
        if (isChecked && !EditModel.SpeakerIds.Contains(userId))
        {
            EditModel.SpeakerIds.Add(userId);
        }
        else if (!isChecked)
        {
            EditModel.SpeakerIds.Remove(userId);
        }
    }
}

/// <summary>
/// Form model for event create/edit.
/// </summary>
public class EventEditModel
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ShortDescription { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Date string in datetime-local format (yyyy-MM-ddTHH:mm).
    /// </summary>
    public string? EventDateString { get; set; }

    [MaxLength(500)]
    public string Location { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public List<int> SpeakerIds { get; set; } = [];
}

/// <summary>
/// Simple record to hold speaker info for the checkbox list.
/// </summary>
public record SpeakerInfo(int UserId, string? FirstName, string? LastName);