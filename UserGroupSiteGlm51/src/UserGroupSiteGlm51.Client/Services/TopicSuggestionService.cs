using System.Net.Http.Json;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;
using UserGroupSiteGlm51.Shared.Models;

namespace UserGroupSiteGlm51.Client.Services;

/// <summary>
/// Client-side implementation of <see cref="ITopicSuggestionService"/> that calls
/// the server's minimal API endpoints via HttpClient.
/// </summary>
public class TopicSuggestionService(HttpClient http) : ITopicSuggestionService
{
    public async Task<IEnumerable<TopicSuggestion>> GetAllTopicsAsync()
    {
        var dtos = await http.GetFromJsonAsync<IEnumerable<TopicSuggestionDto>>("api/topics") ?? [];
        return dtos.Select(MapFromDto);
    }

    public async Task<TopicSuggestion?> GetTopicByIdAsync(int id)
    {
        var dto = await http.GetFromJsonAsync<TopicSuggestionDto>($"api/topics/{id}");
        return dto is null ? null : MapFromDto(dto);
    }

    public async Task<TopicSuggestion> CreateTopicAsync(TopicSuggestion topic)
    {
        var request = new CreateTopicRequest(topic.Title, topic.Description);
        var response = await http.PostAsJsonAsync("api/topics", request);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<TopicSuggestionDto>();
        return MapFromDto(dto!);
    }

    public async Task VoteAsync(int topicSuggestionId, int userId)
    {
        var response = await http.PostAsync($"api/topics/{topicSuggestionId}/vote", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnvoteAsync(int topicSuggestionId, int userId)
    {
        var response = await http.DeleteAsync($"api/topics/{topicSuggestionId}/vote");
        response.EnsureSuccessStatusCode();
    }

    public async Task VolunteerAsync(int topicSuggestionId, int userId)
    {
        var response = await http.PostAsync($"api/topics/{topicSuggestionId}/volunteer", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnvolunteerAsync(int topicSuggestionId, int userId)
    {
        var response = await http.DeleteAsync($"api/topics/{topicSuggestionId}/volunteer");
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> HasVotedAsync(int topicSuggestionId, int userId)
    {
        // This is checked server-side via the DTO; fallback implementation
        var topics = await GetAllTopicsAsync();
        var topic = topics.FirstOrDefault(t => t.Id == topicSuggestionId);
        return topic?.Votes.Any(v => v.UserId == userId) ?? false;
    }

    private static TopicSuggestion MapFromDto(TopicSuggestionDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Description = dto.Description,
        SuggestedById = dto.SuggestedById,
        SuggestedBy = new User { Id = dto.SuggestedById, FirstName = dto.SuggestedByName },
        VolunteerId = dto.VolunteerId,
        Volunteer = dto.VolunteerId.HasValue
            ? new User { Id = dto.VolunteerId.Value, FirstName = dto.VolunteerName }
            : null,
        Votes = [] // Vote count is in the DTO, not mapped back to entities
    };
}