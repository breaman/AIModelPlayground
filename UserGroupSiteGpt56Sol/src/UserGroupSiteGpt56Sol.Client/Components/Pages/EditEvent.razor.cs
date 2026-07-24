using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using UserGroupSiteGpt56Sol.Shared.Authorization;
using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;
using UserGroupSiteGpt56Sol.Shared.Utilities;

namespace UserGroupSiteGpt56Sol.Client.Components.Pages;

public partial class EditEvent : ComponentBase
{
    [Parameter] public int? Id { get; set; }
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [PersistentState]
    public EventEditRequest? Model { get; set; }

    [PersistentState]
    public IReadOnlyList<SpeakerOptionDto>? Speakers { get; set; }

    protected bool IsAdmin { get; set; }
    protected bool Previewing { get; set; }
    protected bool Saving { get; set; }
    protected bool LoadFailed { get; set; }
    protected List<string> ServerErrors { get; set; } = [];
    protected string PreviewHtml => MarkdownRenderer.ToSafeHtml(Model?.DescriptionMarkdown);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (AuthenticationStateTask is not null)
            {
                IsAdmin = (await AuthenticationStateTask).User.IsInRole(AppRoles.Admin);
            }

            if (!Id.HasValue && !IsAdmin)
            {
                LoadFailed = true;
                return;
            }

            Model ??= Id.HasValue ? await EventService.GetForEditAsync(Id.Value) : new EventEditRequest();
            Speakers ??= await EventService.GetSpeakerOptionsAsync();
            LoadFailed = Model is null;
        }
        catch (Exception exception) when (exception is HttpRequestException or UnauthorizedAccessException)
        {
            LoadFailed = true;
        }
    }

    /// <summary>Generates a slug on title blur only while the slug remains empty.</summary>
    protected void GenerateSlug()
    {
        if (Model is not null && string.IsNullOrWhiteSpace(Model.Slug))
        {
            Model.Slug = SlugUtility.Generate(Model.Title);
        }
    }

    /// <summary>Adds or removes an administrator-selected speaker without duplicates.</summary>
    protected void ToggleSpeaker(int speakerId, bool selected)
    {
        if (Model is null || !IsAdmin)
        {
            return;
        }

        if (selected && !Model.SpeakerIds.Contains(speakerId))
        {
            Model.SpeakerIds.Add(speakerId);
        }
        else if (!selected)
        {
            Model.SpeakerIds.Remove(speakerId);
        }
    }

    /// <summary>Saves the draft or published event and displays server validation feedback.</summary>
    protected async Task SaveAsync()
    {
        if (Model is null || Saving)
        {
            return;
        }

        Saving = true;
        ServerErrors.Clear();
        try
        {
            var result = await EventService.SaveAsync(Id, Model);
            if (result.Succeeded)
            {
                ToastService.ShowSuccess(result.Message ?? "Event saved.");
                Navigation.NavigateTo("/events/manage");
                return;
            }

            ServerErrors.AddRange(result.ValidationErrors?.SelectMany(item => item.Value) ?? []);
            if (!string.IsNullOrWhiteSpace(result.Message) && ServerErrors.Count == 0)
            {
                ServerErrors.Add(result.Message);
            }
        }
        catch (HttpRequestException)
        {
            ServerErrors.Add("The event could not be saved.");
        }
        finally
        {
            Saving = false;
        }
    }
}