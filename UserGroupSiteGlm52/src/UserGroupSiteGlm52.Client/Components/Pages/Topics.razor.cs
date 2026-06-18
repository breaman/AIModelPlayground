using Microsoft.AspNetCore.Components;

using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Client.Components.Pages;

/// <summary>Community topic suggestions: suggest, vote (once), and volunteer (once). Login required.</summary>
public partial class Topics : ComponentBase
{
    [Inject]
    private ITopicService TopicService { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    [PersistentState]
    public IReadOnlyList<TopicDto>? TopicList { get; set; }

    // The suggest-form input is fresh on each load (no need to persist across hydration).
    public TopicInputDto NewTopic { get; set; } = new();

    private bool suggesting;

    protected override async Task OnInitializedAsync()
    {
        // Dual-mode: only fetch when state was not restored during hydration.
        TopicList ??= await TopicService.GetTopicsAsync();
    }

    private async Task Suggest()
    {
        if (string.IsNullOrWhiteSpace(NewTopic.Title))
        {
            ToastService.ShowWarning("Please give the topic a title.", "Almost there");
            return;
        }

        suggesting = true;
        try
        {
            var result = await TopicService.SuggestAsync(NewTopic);
            if (result.Succeeded)
            {
                ToastService.ShowSuccess("Topic suggested.", "Thanks!");
                NewTopic = new TopicInputDto();
                await RefreshAsync();
            }
            else
            {
                ShowErrors(result);
            }
        }
        finally
        {
            suggesting = false;
        }
    }

    private async Task VoteAsync(TopicDto topic)
    {
        var result = topic.HasCurrentUserVoted
            ? await TopicService.UnvoteAsync(topic.Id)
            : await TopicService.VoteAsync(topic.Id);

        if (result.Succeeded)
        {
            await RefreshAsync();
        }
        else
        {
            ShowErrors(result);
        }
    }

    private async Task VolunteerAsync(TopicDto topic)
    {
        var result = await TopicService.VolunteerAsync(topic.Id);
        if (result.Succeeded)
        {
            await RefreshAsync();
        }
        else
        {
            ShowErrors(result);
        }
    }

    private async Task RefreshAsync()
    {
        TopicList = await TopicService.GetTopicsAsync();
    }

    private void ShowErrors(ServiceResult result)
    {
        var message = result.ValidationErrors.Count > 0
            ? string.Join(" ", result.ValidationErrors.SelectMany(kv => kv.Value))
            : (result.Errors.Count > 0 ? string.Join(" ", result.Errors) : "Something went wrong.");
        ToastService.ShowError(message, "Action failed");
    }

    private void ShowErrors<T>(ServiceResult<T> result)
    {
        var message = result.ValidationErrors.Count > 0
            ? string.Join(" ", result.ValidationErrors.SelectMany(kv => kv.Value))
            : (result.Errors.Count > 0 ? string.Join(" ", result.Errors) : "Something went wrong.");
        ToastService.ShowError(message, "Action failed");
    }
}