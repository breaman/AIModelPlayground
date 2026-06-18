using Microsoft.AspNetCore.Components;

using UserGroupSiteGpt55.Shared.Topics;

namespace UserGroupSiteGpt55.Client.Components.Pages.Topics;

public partial class TopicSuggestions : ComponentBase
{
    [Inject]
    private ITopicSuggestionService TopicSuggestionService { get; set; } = default!;

    protected TopicSuggestionCreateModel NewTopic { get; set; } = new();

    protected IReadOnlyList<TopicSuggestionItem>? Suggestions { get; set; }

    protected List<string> Errors { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    protected async Task CreateAsync()
    {
        var result = await TopicSuggestionService.CreateSuggestionAsync(NewTopic);
        await HandleResultAsync(result);
        if (result.Succeeded)
        {
            NewTopic = new TopicSuggestionCreateModel();
        }
    }

    protected async Task VoteAsync(int topicSuggestionId)
    {
        await HandleResultAsync(await TopicSuggestionService.VoteAsync(topicSuggestionId));
    }

    protected async Task RemoveVoteAsync(int topicSuggestionId)
    {
        await HandleResultAsync(await TopicSuggestionService.RemoveVoteAsync(topicSuggestionId));
    }

    protected async Task VolunteerAsync(int topicSuggestionId)
    {
        await HandleResultAsync(await TopicSuggestionService.VolunteerAsync(topicSuggestionId));
    }

    private async Task HandleResultAsync(TopicActionResult result)
    {
        Errors = result.Errors.ToList();
        if (result.Succeeded)
        {
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        Suggestions = await TopicSuggestionService.GetSuggestionsAsync();
    }
}