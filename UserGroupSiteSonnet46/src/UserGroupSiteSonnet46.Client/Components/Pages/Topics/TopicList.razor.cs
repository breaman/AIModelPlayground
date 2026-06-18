using Microsoft.AspNetCore.Components;

using UserGroupSiteSonnet46.Shared.Services;

namespace UserGroupSiteSonnet46.Client.Components.Pages.Topics;

/// <summary>Lists all topic suggestions with voting and volunteer actions.</summary>
public partial class TopicList : ComponentBase
{
    [Inject] private ITopicService TopicService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    [PersistentState]
    public List<TopicSuggestionDto>? Topics { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Topics ??= await TopicService.GetTopicsAsync();
    }

    private async Task Vote(TopicSuggestionDto topic)
    {
        try
        {
            await TopicService.VoteAsync(topic.Id);
            ToastService.ShowSuccess($"Vote recorded for \"{topic.Title}\".");
            Topics = await TopicService.GetTopicsAsync();
        }
        catch (Exception)
        {
            ToastService.ShowError("Failed to record vote.");
        }
    }

    private async Task Volunteer(TopicSuggestionDto topic)
    {
        try
        {
            await TopicService.VolunteerAsync(topic.Id);
            ToastService.ShowSuccess($"You volunteered to present \"{topic.Title}\".");
            Topics = await TopicService.GetTopicsAsync();
        }
        catch (Exception)
        {
            ToastService.ShowError("Could not register as volunteer. The topic may already have one.");
        }
    }
}