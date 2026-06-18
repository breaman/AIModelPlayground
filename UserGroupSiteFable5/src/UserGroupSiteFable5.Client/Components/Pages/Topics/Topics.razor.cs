using Microsoft.AspNetCore.Components;

using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Client.Components.Pages.Topics;

public partial class Topics : ComponentBase
{
    [Inject] private ITopicService TopicService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    [PersistentState]
    public List<TopicSuggestionDto>? TopicList { get; set; }

    private TopicCreateDto NewTopic { get; set; } = new();

    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        TopicList ??= await TopicService.GetTopicsAsync();
    }

    private async Task SubmitSuggestionAsync()
    {
        await RunAsync(async () =>
        {
            var result = await TopicService.CreateTopicAsync(NewTopic);
            if (result.Success)
            {
                ToastService.ShowSuccess($"\"{NewTopic.Title}\" was suggested.");
                NewTopic = new TopicCreateDto();
            }

            return result;
        });
    }

    private async Task VoteAsync(TopicSuggestionDto topic)
    {
        await RunAsync(() => TopicService.VoteAsync(topic.Id));
    }

    private async Task VolunteerAsync(TopicSuggestionDto topic)
    {
        await RunAsync(() => TopicService.VolunteerAsync(topic.Id));
    }

    /// <summary>
    /// Runs a write, surfaces errors as toasts, and reloads the list afterward — also on
    /// failure, since e.g. a vote conflict means our copy of the list is stale.
    /// </summary>
    private async Task RunAsync(Func<Task<ServiceResult>> action)
    {
        _saving = true;
        try
        {
            var result = await action();
            if (!result.Success)
            {
                ToastService.ShowError(result.Error ?? "Something went wrong. Please try again.");
            }

            TopicList = await TopicService.GetTopicsAsync();
        }
        catch (Exception)
        {
            ToastService.ShowError("Something went wrong. Please try again.");
        }
        finally
        {
            _saving = false;
        }
    }
}