using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Client.Components.Pages;

[Authorize]
public partial class Topics : ComponentBase
{
    [Inject] private ITopicService TopicService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    [PersistentState]
    public IReadOnlyList<TopicSuggestionDto>? TopicsList { get; set; }

    protected CreateTopicRequest NewTopic { get; set; } = new();
    protected bool Busy { get; set; }
    protected bool LoadFailed { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            TopicsList ??= await TopicService.GetAsync();
        }
        catch (HttpRequestException)
        {
            LoadFailed = true;
        }
    }

    /// <summary>Creates a topic and refreshes the list.</summary>
    protected async Task CreateAsync()
    {
        await RunAsync(async () =>
        {
            var result = await TopicService.CreateAsync(NewTopic);
            ShowResult(result.Succeeded, result.Message);
            if (result.Succeeded)
            {
                NewTopic = new CreateTopicRequest();
                await RefreshAsync();
            }
        });
    }

    /// <summary>Adds or removes the current user's vote.</summary>
    protected Task ToggleVoteAsync(TopicSuggestionDto topic) => RunAsync(async () =>
    {
        var result = topic.HasCurrentUserVote
            ? await TopicService.UnvoteAsync(topic.Id)
            : await TopicService.VoteAsync(topic.Id);
        ShowResult(result.Succeeded, result.Message);
        await RefreshAsync();
    });

    /// <summary>Claims an unclaimed topic for the current user.</summary>
    protected Task VolunteerAsync(int topicId) => RunAsync(async () =>
    {
        var result = await TopicService.VolunteerAsync(topicId);
        ShowResult(result.Succeeded, result.Message);
        await RefreshAsync();
    });

    /// <summary>Serializes commands and surfaces unexpected HTTP failures.</summary>
    private async Task RunAsync(Func<Task> action)
    {
        if (Busy)
        {
            return;
        }

        Busy = true;
        try
        {
            await action();
        }
        catch (HttpRequestException)
        {
            ToastService.ShowError("The request could not be completed.");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Reloads vote counts and volunteer state.</summary>
    private async Task RefreshAsync() => TopicsList = await TopicService.GetAsync();

    /// <summary>Displays a service result through the shared toast system.</summary>
    private void ShowResult(bool succeeded, string? message)
    {
        var text = message ?? (succeeded ? "Done." : "The request failed.");
        if (succeeded)
        {
            ToastService.ShowSuccess(text);
        }
        else
        {
            ToastService.ShowError(text);
        }
    }

    /// <summary>Formats a stored UTC timestamp in the visitor's local timezone.</summary>
    protected static string FormatDate(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime().ToString("g");
}