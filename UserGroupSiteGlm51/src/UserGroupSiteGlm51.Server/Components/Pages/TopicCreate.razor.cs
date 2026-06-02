using System.ComponentModel.DataAnnotations;
using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;
using UserGroupSiteGlm51.Shared.Services;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteGlm51.Server.Components.Pages;

/// <summary>
/// Page for creating a new topic suggestion.
/// </summary>
public partial class TopicCreate : ComponentBase
{
    [Inject]
    private ITopicSuggestionService TopicService { get; set; } = default!;

    [Inject]
    private IUserService UserService { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Form model for creating a topic suggestion.
    /// </summary>
    public TopicCreateModel Model { get; set; } = new();

    private async Task HandleValidSubmit()
    {
        try
        {
            var topic = new TopicSuggestion
            {
                Title = Model.Title,
                Description = string.IsNullOrWhiteSpace(Model.Description) ? null : Model.Description,
                SuggestedById = UserService.UserId
            };

            await TopicService.CreateTopicAsync(topic);
            ToastService.ShowSuccess("Topic suggestion created!");
            Navigation.NavigateTo("/topics");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error creating topic: {ex.Message}");
        }
    }
}

/// <summary>
/// Form model for creating a topic suggestion.
/// </summary>
public class TopicCreateModel
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}