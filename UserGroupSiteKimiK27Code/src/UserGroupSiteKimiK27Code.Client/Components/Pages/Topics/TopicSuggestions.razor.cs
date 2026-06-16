using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteKimiK27Code.Shared;
using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Client.Components.Pages.Topics;

public partial class TopicSuggestions : ComponentBase
{
    [Inject] private ITopicSuggestionService TopicSuggestionService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private List<TopicSuggestionDto>? _suggestions;
    private CreateTopicSuggestionRequest _newSuggestion = new();
    private bool _isSubmitting;
    private bool _isSpeaker;
    private readonly HashSet<int> _votingIds = [];
    private readonly HashSet<int> _volunteeringIds = [];

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _isSpeaker = authState.User.IsInRole(Roles.Speaker);
        await LoadSuggestionsAsync();
    }

    private async Task LoadSuggestionsAsync()
    {
        _suggestions = await TopicSuggestionService.GetSuggestionsAsync();
    }

    private async Task CreateSuggestionAsync()
    {
        _isSubmitting = true;
        var suggestion = await TopicSuggestionService.CreateSuggestionAsync(_newSuggestion);
        _isSubmitting = false;

        if (suggestion is not null)
        {
            _newSuggestion = new CreateTopicSuggestionRequest();
            await LoadSuggestionsAsync();
            ToastService.ShowSuccess("Topic suggestion submitted.");
        }
        else
        {
            ToastService.ShowError("Failed to submit suggestion.");
        }
    }

    private async Task VoteAsync(TopicSuggestionDto suggestion)
    {
        _votingIds.Add(suggestion.Id);
        var result = await TopicSuggestionService.VoteAsync(suggestion.Id);
        _votingIds.Remove(suggestion.Id);

        if (result is not null)
        {
            suggestion.VoteCount = result.VoteCount;
            suggestion.HasVoted = result.HasVoted;
        }
        else
        {
            ToastService.ShowError("Failed to register vote.");
        }
    }

    private async Task VolunteerAsync(TopicSuggestionDto suggestion)
    {
        _volunteeringIds.Add(suggestion.Id);
        var updated = await TopicSuggestionService.VolunteerAsync(suggestion.Id);
        _volunteeringIds.Remove(suggestion.Id);

        if (updated is not null)
        {
            await LoadSuggestionsAsync();
            ToastService.ShowSuccess("You have volunteered to speak on this topic.");
        }
        else
        {
            ToastService.ShowError("Failed to volunteer.");
        }
    }
}