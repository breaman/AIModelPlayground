using Microsoft.AspNetCore.Components;

using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Client.Components.Pages.Topics;

public partial class TopicList : ComponentBase
{
    [Inject] private ITopicSuggestionService TopicService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    protected List<TopicSuggestionListItemDto>? _topics;

    protected TopicSuggestionDto _newTopic = new();
    protected bool _showSuggestForm;

    protected override async Task OnInitializedAsync()
    {
        _topics ??= await TopicService.GetAllAsync();
    }

    private async Task ToggleVote(TopicSuggestionListItemDto topic)
    {
        try
        {
            if (topic.CurrentUserHasVoted)
            {
                await TopicService.RemoveVoteAsync(topic.Id);
            }
            else
            {
                await TopicService.VoteAsync(topic.Id);
            }
            _topics = await TopicService.GetAllAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to vote: {ex.Message}");
        }
    }

    private async Task Volunteer(int topicId)
    {
        try
        {
            await TopicService.VolunteerAsync(topicId);
            _topics = await TopicService.GetAllAsync();
            ToastService.ShowSuccess("You volunteered to speak on this topic!");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to volunteer: {ex.Message}");
        }
    }

    private async Task RemoveVolunteer(int topicId)
    {
        try
        {
            await TopicService.RemoveVolunteerAsync(topicId);
            _topics = await TopicService.GetAllAsync();
            ToastService.ShowSuccess("Volunteer removed.");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to remove volunteer: {ex.Message}");
        }
    }

    private async Task HandleSuggest()
    {
        if (string.IsNullOrWhiteSpace(_newTopic.Title))
        {
            ToastService.ShowError("Title is required.");
            return;
        }

        try
        {
            await TopicService.CreateAsync(_newTopic);
            _newTopic = new TopicSuggestionDto();
            _showSuggestForm = false;
            _topics = await TopicService.GetAllAsync();
            ToastService.ShowSuccess("Topic suggestion submitted!");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to suggest topic: {ex.Message}");
        }
    }
}
