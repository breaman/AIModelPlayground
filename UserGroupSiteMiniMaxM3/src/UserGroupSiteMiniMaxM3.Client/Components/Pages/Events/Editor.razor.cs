using System.Net.Http.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteMiniMaxM3.Shared.Models;
using UserGroupSiteMiniMaxM3.Shared.Models.Events;
using UserGroupSiteMiniMaxM3.Shared.Services;

namespace UserGroupSiteMiniMaxM3.Client.Components.Pages.Events;

public partial class Editor : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    private EventEditDto Model { get; set; } = new();
    private EventDto? _event;
    private List<SpeakerOption>? _availableSpeakers;
    private bool _saving;
    private string? _loadError;
    private Dictionary<string, string[]> _errors = new();
    private string _previewHtml = string.Empty;
    private string ActiveTab { get; set; } = "edit";
    private bool _slugManuallyEdited;

    /// <summary>Pre-rendered event data when editing an existing event.</summary>
    [PersistentState]
    public EventDto? InitialEvent { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        // Load the existing event if we have an id, otherwise stay on the new model.
        if (Id > 0)
        {
            if (InitialEvent is not null)
            {
                _event = InitialEvent;
            }
            else
            {
                await LoadEventAsync();
            }
        }
        else
        {
            _event = null;
            Model = new EventEditDto { EventDateTime = DateTime.Now.AddDays(7) };
        }

        // The "speakers" list of available users (admins can pick any user; speakers
        // get a curated list of admins/speakers). For v1 we let anyone pick anyone.
        if (_availableSpeakers is null)
        {
            await LoadSpeakersAsync();
        }

        UpdatePreview();
    }

    private async Task LoadEventAsync()
    {
        try
        {
            // Fetch the manage list, find the one with our id, then load its full body by slug.
            var list = await Http.GetFromJsonAsync<List<EventSummaryDto>>("api/events/manage") ?? [];
            var found = list.FirstOrDefault(e => e.Id == Id);
            if (found is null)
            {
                _loadError = "Event not found or you don't have permission to edit it.";
                return;
            }

            _event = await Http.GetFromJsonAsync<EventDto>($"api/events/by-slug/{found.Slug}");
            if (_event is null)
            {
                _loadError = "Event not found.";
                return;
            }

            Model = new EventEditDto
            {
                Id = _event.Id,
                Title = _event.Title,
                Slug = _event.Slug,
                ShortDescription = _event.ShortDescription,
                Description = _event.Description,
                EventDateTime = _event.EventDateTime,
                Location = _event.Location,
                IsPublished = _event.IsPublished,
                SpeakerIds = _event.Speakers.Select(s => s.Id).ToList(),
            };
            _slugManuallyEdited = true; // already set; don't auto-fill.
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
        }
    }

    private async Task LoadSpeakersAsync()
    {
        try
        {
            _availableSpeakers = await Http.GetFromJsonAsync<List<SpeakerOption>>("api/users/speakers") ?? [];
        }
        catch
        {
            _availableSpeakers = [];
        }
    }

    private void ToggleSpeaker(int id, bool isOn)
    {
        if (isOn && !Model.SpeakerIds.Contains(id))
        {
            Model.SpeakerIds.Add(id);
        }
        else if (!isOn)
        {
            Model.SpeakerIds.Remove(id);
        }
    }

    private void OnSlugAfter()
    {
        _slugManuallyEdited = true;
    }

    /// <summary>Generates a kebab-case slug from the title when the user hasn't typed one.</summary>
    private string? DeriveSlug()
    {
        if (string.IsNullOrWhiteSpace(Model.Title)) return null;
        var lower = Model.Title.ToLowerInvariant();
        var slug = Regex.Replace(lower, "[^a-z0-9]+", "-").Trim('-');
        return slug;
    }

    private void UpdatePreview()
    {
        // Client-side preview: a deliberately simple HTML escape + paragraph split.
        // The server's Markdig + sanitizer is the authoritative rendering on save.
        var desc = Model.Description ?? string.Empty;
        _previewHtml = desc
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => $"<p>{System.Net.WebUtility.HtmlEncode(p).Replace("\n", "<br/>")}</p>")
            .Aggregate("", (acc, p) => acc + p);
    }

    private string TabClass(string tab) => ActiveTab == tab ? "active" : "";

    private void SetTab(string tab) => ActiveTab = tab;

    private async Task Save()
    {
        _saving = true;
        _errors.Clear();
        try
        {
            // Auto-fill the slug if the user hasn't typed one.
            if (!_slugManuallyEdited && string.IsNullOrWhiteSpace(Model.Slug))
            {
                Model.Slug = DeriveSlug() ?? string.Empty;
            }

            HttpResponseMessage response;
            if (Model.Id == 0)
            {
                response = await Http.PostAsJsonAsync("api/events", Model);
            }
            else
            {
                response = await Http.PutAsJsonAsync($"api/events/{Model.Id}", Model);
            }

            if (response.IsSuccessStatusCode)
            {
                ToastService.ShowSuccess("Event saved");
                Nav.NavigateTo("/events/manage");
                return;
            }

            // Try to read RFC 9457 problem details.
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
            if (problem?.Errors is { Count: > 0 })
            {
                _errors = problem.Errors;
                ToastService.ShowError("Please correct the highlighted fields.");
            }
            else
            {
                var text = await response.Content.ReadAsStringAsync();
                _errors = new() { ["_"] = new[] { string.IsNullOrEmpty(text) ? "Save failed." : text } };
            }
        }
        catch (Exception ex)
        {
            _errors = new() { ["_"] = new[] { ex.Message } };
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>RFC 9457 problem details with a per-field Errors map.</summary>
    private record ValidationProblem(Dictionary<string, string[]>? Errors);
}