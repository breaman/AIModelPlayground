using UserGroupSiteKimiK27Code.Shared.Dtos;

namespace UserGroupSiteKimiK27Code.Shared.Services;

public interface ITopicSuggestionService
{
    Task<List<TopicSuggestionDto>> GetSuggestionsAsync();
    Task<TopicSuggestionDto?> CreateSuggestionAsync(CreateTopicSuggestionRequest request);
    Task<TopicVoteDto?> VoteAsync(int topicId);
    Task<TopicSuggestionDto?> VolunteerAsync(int topicId);
}

public sealed class CreateTopicSuggestionRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MaxLength(200)]
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";
}