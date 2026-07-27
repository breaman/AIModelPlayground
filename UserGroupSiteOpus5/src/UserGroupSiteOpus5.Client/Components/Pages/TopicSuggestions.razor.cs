using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteOpus5.Client.Components.Pages;

/// <summary>
/// Lists member topic suggestions and lets members suggest, vote, and volunteer.
/// </summary>
/// <remarks>
/// Interactive WebAssembly, pre-rendered on the server so the list is visible before the runtime
/// finishes downloading.
/// </remarks>
public partial class TopicSuggestions : ComponentBase
{
    [Inject] private ITopicSuggestionService SuggestionService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    /// <summary>Suggestions, most-voted first. Null while loading.</summary>
    [PersistentState]
    public IReadOnlyList<TopicSuggestionListItem>? Suggestions { get; set; }

    /// <summary>The suggestion form's bound model.</summary>
    private TopicSuggestionCreateModel NewSuggestion { get; set; } = new();

    /// <summary>Whether the suggestion form is showing.</summary>
    private bool IsSuggesting { get; set; }

    /// <summary>Whether the suggestion form is submitting.</summary>
    private bool IsSubmitting { get; set; }

    /// <summary>
    /// Suggestion ids with an action in flight, so a row's buttons disable individually rather
    /// than the whole list locking up.
    /// </summary>
    private readonly HashSet<int> _busySuggestionIds = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (Suggestions is null)
        {
            await ReloadAsync();
        }
    }

    /// <summary>Whether an action is in flight for the given suggestion.</summary>
    private bool IsBusy(int suggestionId)
    {
        return _busySuggestionIds.Contains(suggestionId);
    }

    /// <summary>Records a new suggestion and refreshes the list.</summary>
    private async Task CreateAsync()
    {
        if (IsSubmitting)
        {
            return;
        }

        IsSubmitting = true;

        try
        {
            var result = await SuggestionService.CreateSuggestionAsync(NewSuggestion);

            if (!result.Succeeded)
            {
                ToastService.ShowError(string.Join(" ", result.Errors), "Could not save");
                return;
            }

            ToastService.ShowSuccess($"Suggested “{NewSuggestion.Title}”.");
            NewSuggestion = new TopicSuggestionCreateModel();
            IsSuggesting = false;
            await ReloadAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ToastService.ShowError("Your suggestion could not be saved. Try again.");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    /// <summary>Casts the current member's vote.</summary>
    private async Task VoteAsync(int suggestionId)
    {
        await RunRowActionAsync(
            suggestionId,
            () => SuggestionService.VoteAsync(suggestionId),
            // Optimistic: bump the count and flip the button immediately, then reconcile from the
            // server. A vote is cheap to get wrong for a moment and slow to wait for.
            current => current with
            {
                VoteCount = current.VoteCount + 1,
                HasCurrentUserVoted = true
            });
    }

    /// <summary>Claims the presenter slot for the current member.</summary>
    private async Task VolunteerAsync(int suggestionId)
    {
        await RunRowActionAsync(
            suggestionId,
            () => SuggestionService.VolunteerAsync(suggestionId),
            // The volunteer's own name is not known client-side, so show a neutral placeholder
            // until the reload replaces it.
            current => current with
            {
                VolunteerUserId = -1,
                VolunteerName = "You"
            });
    }

    /// <summary>
    /// Applies an optimistic update, runs the server action, and reconciles the list from the
    /// server afterwards. On failure the optimistic change is rolled back with the original row.
    /// </summary>
    private async Task RunRowActionAsync(
        int suggestionId,
        Func<Task<SaveResult>> action,
        Func<TopicSuggestionListItem, TopicSuggestionListItem> optimisticUpdate)
    {
        if (Suggestions is null || IsBusy(suggestionId))
        {
            return;
        }

        var original = Suggestions.FirstOrDefault(x => x.Id == suggestionId);

        if (original is null)
        {
            return;
        }

        _busySuggestionIds.Add(suggestionId);
        Suggestions = Replace(Suggestions, suggestionId, optimisticUpdate(original));

        try
        {
            var result = await action();

            if (!result.Succeeded)
            {
                Suggestions = Replace(Suggestions, suggestionId, original);
                ToastService.ShowError(string.Join(" ", result.Errors));
                return;
            }

            await ReloadAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Suggestions = Replace(Suggestions, suggestionId, original);
            ToastService.ShowError("That did not go through. Check your connection and try again.");
        }
        finally
        {
            _busySuggestionIds.Remove(suggestionId);
        }
    }

    /// <summary>Returns the list with one row swapped out.</summary>
    private static IReadOnlyList<TopicSuggestionListItem> Replace(
        IReadOnlyList<TopicSuggestionListItem> source,
        int suggestionId,
        TopicSuggestionListItem replacement)
    {
        return source.Select(x => x.Id == suggestionId ? replacement : x).ToList();
    }

    /// <summary>Refetches the list from the server.</summary>
    private async Task ReloadAsync()
    {
        try
        {
            Suggestions = await SuggestionService.GetSuggestionsAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Suggestions ??= [];
            ToastService.ShowError("Could not load topic suggestions.");
        }
    }
}
