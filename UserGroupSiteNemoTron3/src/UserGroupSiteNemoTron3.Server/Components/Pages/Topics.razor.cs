using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Server.Components.Pages;

public partial class Topics : ComponentBase
{
    [Inject]
    public ITopicSuggestionService TopicService { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    [PersistentState]
    public TopicSuggestionDto[]? TopicList { get; set; }

    public CreateTopicSuggestionDto NewTopic { get; set; } = new CreateTopicSuggestionDto { Title = "", Description = null };
    public bool IsCreating { get; set; }
    public bool IsVoting { get; set; }
    public bool IsVolunteering { get; set; }

    protected override async Task OnInitializedAsync()
    {
        TopicList ??= await TopicService.GetAllAsync();
    }

    private async Task CreateTopic()
    {
        IsCreating = true;
        try
        {
            var topic = await TopicService.CreateAsync(NewTopic, 0); // userId will be determined by server
            ToastService.ShowSuccess("Topic suggested successfully!");
            NewTopic = new CreateTopicSuggestionDto { Title = "", Description = null };
            TopicList = await TopicService.GetAllAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error creating topic: {ex.Message}");
        }
        finally
        {
            IsCreating = false;
        }
    }

    private async Task ToggleVote(TopicSuggestionDto topic)
    {
        IsVoting = true;
        try
        {
            if (topic.HasCurrentUserVoted)
            {
                await TopicService.RemoveVoteAsync(topic.Id, 0);
                ToastService.ShowSuccess("Vote removed");
            }
            else
            {
                await TopicService.VoteAsync(topic.Id, 0);
                ToastService.ShowSuccess("Thanks for voting!");
            }
            TopicList = await TopicService.GetAllAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error voting: {ex.Message}");
        }
        finally
        {
            IsVoting = false;
        }
    }

    private async Task Volunteer(int topicId)
    {
        IsVolunteering = true;
        try
        {
            await TopicService.VolunteerAsync(topicId, 0);
            ToastService.ShowSuccess("You've volunteered to speak!");
            TopicList = await TopicService.GetAllAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error volunteering: {ex.Message}");
        }
        finally
        {
            IsVolunteering = false;
        }
    }
}