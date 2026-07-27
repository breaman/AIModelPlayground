using System.Net;
using System.Net.Http.Json;

using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

namespace UserGroupSiteOpus5.Client.Services;

/// <summary>
/// WebAssembly implementation of <see cref="IEventService"/>, calling the server's HTTP API.
/// </summary>
/// <remarks>
/// The server-side implementation of the same interface runs during pre-rendering, so a component
/// behaves identically before and after hydration.
/// </remarks>
public class ClientEventService(HttpClient http) : IEventService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EventListItem>> GetPublishedEventsAsync()
    {
        return await http.GetFromJsonAsync<List<EventListItem>>("api/events") ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventListItem>> GetManageableEventsAsync()
    {
        return await http.GetFromJsonAsync<List<EventListItem>>("api/events/manage") ?? [];
    }

    /// <inheritdoc />
    public async Task<EventDetail?> GetEventBySlugAsync(string slug)
    {
        var response = await http.GetAsync($"api/events/{Uri.EscapeDataString(slug)}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventDetail>();
    }

    /// <inheritdoc />
    public async Task<EventEditModel?> GetEventForEditAsync(int id)
    {
        var response = await http.GetAsync($"api/events/{id}/edit");

        // A 403 means the caller may not edit this event; both that and a missing event are
        // surfaced as "nothing to edit" so the page shows one not-found state rather than two.
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventEditModel>();
    }

    /// <inheritdoc />
    public async Task<SaveResult<int>> SaveEventAsync(EventEditModel model)
    {
        var response = model.Id == 0
            ? await http.PostAsJsonAsync("api/events", model)
            : await http.PutAsJsonAsync($"api/events/{model.Id}", model);

        return await response.ReadSaveResultAsync<int>();
    }

    /// <inheritdoc />
    public async Task<string> GenerateUniqueSlugAsync(string title, int? excludeEventId)
    {
        var url = $"api/events/slug?title={Uri.EscapeDataString(title)}";

        if (excludeEventId is { } id)
        {
            url += $"&excludeEventId={id}";
        }

        return await http.GetFromJsonAsync<string>(url) ?? "";
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSpeakerInfo>> GetAvailableSpeakersAsync()
    {
        return await http.GetFromJsonAsync<List<EventSpeakerInfo>>("api/speakers") ?? [];
    }

    /// <inheritdoc />
    public async Task<bool> CanEditEventAsync(int eventId)
    {
        var response = await http.GetAsync($"api/events/{eventId}/can-edit");

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        return await response.Content.ReadFromJsonAsync<bool>();
    }
}
