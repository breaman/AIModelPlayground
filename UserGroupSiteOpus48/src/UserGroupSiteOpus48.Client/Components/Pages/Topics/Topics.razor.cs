using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteOpus48.Client.Components.Pages.Topics;

/// <summary>
/// Authenticated page where members suggest topics, cast a single vote per topic, and volunteer to
/// present a topic that has no volunteer yet. All rules are enforced authoritatively on the server.
/// </summary>
public partial class Topics : ComponentBase
{
    [Inject] private ITopicService TopicService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    /// <summary>The current list of topics; persisted across pre-render/hydration.</summary>
    [PersistentState]
    public TopicSuggestionDto[]? TopicList { get; set; }

    /// <summary>Bound model for the suggest form.</summary>
    public TopicCreateDto NewTopic { get; set; } = new();

    private bool _submitting;

    /// <summary>Id of the topic with an in-flight vote/volunteer action (disables its buttons).</summary>
    private int? _busyTopicId;

    protected override async Task OnInitializedAsync()
    {
        TopicList ??= await TopicService.GetTopicsAsync();
    }

    private async Task SuggestAsync()
    {
        _submitting = true;
        try
        {
            var result = await TopicService.SuggestTopicAsync(NewTopic);
            if (result.Success)
            {
                ToastService.ShowSuccess("Topic suggested.");
                NewTopic = new TopicCreateDto();
                await ReloadAsync();
            }
            else
            {
                ToastService.ShowError(string.Join(" ", result.Errors));
            }
        }
        finally
        {
            _submitting = false;
        }
    }

    private async Task VoteAsync(int topicId)
    {
        _busyTopicId = topicId;
        try
        {
            var result = await TopicService.VoteAsync(topicId);
            if (result.Success)
            {
                await ReloadAsync();
            }
            else
            {
                ToastService.ShowError(string.Join(" ", result.Errors));
            }
        }
        finally
        {
            _busyTopicId = null;
        }
    }

    private async Task VolunteerAsync(int topicId)
    {
        _busyTopicId = topicId;
        try
        {
            var result = await TopicService.VolunteerAsync(topicId);
            if (result.Success)
            {
                ToastService.ShowSuccess("Thanks for volunteering!");
                await ReloadAsync();
            }
            else
            {
                ToastService.ShowError(string.Join(" ", result.Errors));
            }
        }
        finally
        {
            _busyTopicId = null;
        }
    }

    private async Task ReloadAsync()
    {
        TopicList = await TopicService.GetTopicsAsync();
    }
}
