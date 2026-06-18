using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Components;

using UserGroupSiteSonnet46.Shared.Services;

namespace UserGroupSiteSonnet46.Client.Components.Pages.Topics;

/// <summary>Form for submitting a new topic suggestion.</summary>
public partial class TopicNew : ComponentBase
{
    [Inject] private ITopicService TopicService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    public TopicFormModel Model { get; set; } = new();

    private bool _submitting;

    private async Task HandleSubmit()
    {
        _submitting = true;

        try
        {
            await TopicService.SuggestTopicAsync(Model.Title, Model.Description);
            ToastService.ShowSuccess("Topic suggestion submitted!");
            Navigation.NavigateTo("/topics");
        }
        catch (Exception)
        {
            ToastService.ShowError("Failed to submit topic suggestion.");
            _submitting = false;
        }
    }

    /// <summary>Form model for a new topic suggestion.</summary>
    public class TopicFormModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}