using System.Net.Http.Json;

using UserGroupSiteGpt55.Shared.Events;

namespace UserGroupSiteGpt55.Client.Services.Events;

/// <summary>
/// Implements event operations for WebAssembly by calling server APIs.
/// </summary>
public sealed class ClientEventService(HttpClient httpClient) : IEventService
{
    public async Task<IReadOnlyList<EventListItem>> GetPublishedEventsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<EventListItem>>("api/events/published", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<EventListItem>> GetEditableEventsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<EventListItem>>("api/events/editable", cancellationToken) ?? [];
    }

    public async Task<EventDetail?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<EventDetail>($"api/events/by-slug/{Uri.EscapeDataString(slug)}", cancellationToken);
    }

    public async Task<EventEditModel?> GetEventForEditAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<EventEditModel>($"api/events/{eventId}/edit", cancellationToken);
    }

    public async Task<EventEditModel> CreateDraftAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<EventEditModel>("api/events/draft", cancellationToken) ?? new EventEditModel();
    }

    public async Task<IReadOnlyList<SpeakerOption>> GetSpeakerOptionsAsync(
        IReadOnlyCollection<int> selectedSpeakerIds,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/events/speakers", selectedSpeakerIds, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeakerOption>>(cancellationToken) ?? [];
    }

    public async Task<EventSaveResult> SaveEventAsync(EventEditModel model, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/events", model, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventSaveResult>(cancellationToken) ??
               EventSaveResult.Failure(["The server returned an empty response."]);
    }

    public async Task<IReadOnlyList<string>> ValidateEventAsync(EventEditModel model, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/events/validate", model, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>(cancellationToken) ?? [];
    }
}
