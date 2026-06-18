using Microsoft.AspNetCore.Components;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;
using UserGroupSiteGlm51.Shared.Services;

namespace UserGroupSiteGlm51.Server.Components.Pages;

/// <summary>
/// Topic suggestions list page — shows all topics with vote/volunteer actions for authenticated users.
/// </summary>
public partial class Topics : ComponentBase
{
    [Inject]
    private ITopicSuggestionService TopicService { get; set; } = default!;

    [Inject]
    private IUserService UserService { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    /// <summary>
    /// List of topic suggestions with their votes and volunteer info.
    /// </summary>
    public IEnumerable<TopicSuggestion>? TopicList { get; set; }

    /// <summary>
    /// The current user's ID for vote/volunteer checks.
    /// </summary>
    public int CurrentUserId => UserService.UserId;

    protected override async Task OnInitializedAsync()
    {
        await LoadTopics();
    }

    private async Task LoadTopics()
    {
        TopicList = await TopicService.GetAllTopicsAsync();
    }

    private async Task VoteTopic(int topicId)
    {
        try
        {
            await TopicService.VoteAsync(topicId, UserService.UserId);
            ToastService.ShowSuccess("Vote recorded!");
            await LoadTopics();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error voting: {ex.Message}");
        }
    }

    private async Task UnvoteTopic(int topicId)
    {
        try
        {
            await TopicService.UnvoteAsync(topicId, UserService.UserId);
            ToastService.ShowInfo("Vote removed.");
            await LoadTopics();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error removing vote: {ex.Message}");
        }
    }

    private async Task VolunteerTopic(int topicId)
    {
        try
        {
            await TopicService.VolunteerAsync(topicId, UserService.UserId);
            ToastService.ShowSuccess("You've volunteered to present this topic!");
            await LoadTopics();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error volunteering: {ex.Message}");
        }
    }

    private async Task UnvolunteerTopic(int topicId)
    {
        try
        {
            await TopicService.UnvolunteerAsync(topicId, UserService.UserId);
            ToastService.ShowInfo("You're no longer volunteering for this topic.");
            await LoadTopics();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error unvolunteering: {ex.Message}");
        }
    }
}