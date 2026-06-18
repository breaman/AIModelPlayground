using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Client.Components.Pages;

/// <summary>
/// Create/edit an event (WebAssembly). Create is Admin-only at the API; editing is
/// Admin or an assigned speaker — both enforced server-side. The page is
/// <c>[Authorize]</c> for UX gating only.
/// </summary>
public partial class EventEditor : ComponentBase
{
    [Parameter]
    public int? Id { get; set; }

    [Inject]
    private IEventService EventService { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [PersistentState]
    public EventEditDto? Model { get; set; }

    [PersistentState]
    public IReadOnlyList<EventSummaryDto>? ManageList { get; set; }

    private EditContext? editContext;
    private ValidationMessageStore? messageStore;
    private bool isEdit;
    private bool saving;

    protected override async Task OnInitializedAsync()
    {
        isEdit = Id is { } id && id > 0;

        if (isEdit)
        {
            // Dual-mode: only fetch when state was not restored during hydration.
            if (Model is null)
            {
                var result = await EventService.GetForEditAsync(Id!.Value);
                if (result.Succeeded && result.Value is not null)
                {
                    Model = result.Value;
                }
                else
                {
                    ToastService.ShowError(result.Succeeded ? "Event not found." : string.Join(" ", result.Errors), "Unable to load event");
                    NavigationManager.NavigateTo("/admin/events");
                    return;
                }
            }
        }
        else
        {
            Model ??= new EventEditDto();
            if (Model.SpeakerOptions.Count == 0)
            {
                Model.SpeakerOptions = await EventService.GetSpeakerOptionsAsync();
            }

            ManageList ??= await EventService.GetEditableListAsync();
        }

        editContext = new EditContext(Model);
        messageStore = new ValidationMessageStore(editContext);
    }

    /// <summary>Auto-fill the slug from the title on blur, when the slug is empty.</summary>
    private void AutoSlug()
    {
        if (Model is null || editContext is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Model.Slug))
        {
            Model.Slug = SlugHelper.ToSlug(Model.Title);
            editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(EventEditDto.Slug)));
        }
    }

    private void ToggleSpeaker(int id, bool isChecked)
    {
        if (Model is null)
        {
            return;
        }

        if (isChecked && !Model.SpeakerIds.Contains(id))
        {
            Model.SpeakerIds.Add(id);
        }
        else if (!isChecked)
        {
            Model.SpeakerIds.Remove(id);
        }
    }

    private async Task Save()
    {
        if (Model is null || editContext is null)
        {
            return;
        }

        messageStore?.Clear();
        if (!editContext.Validate())
        {
            ToastService.ShowWarning("Please correct the highlighted fields.", "Validation");
            return;
        }

        saving = true;
        try
        {
            var result = isEdit
                ? await EventService.UpdateAsync(Id!.Value, Model)
                : await EventService.CreateAsync(Model);

            if (result.Succeeded && result.Value is not null)
            {
                ToastService.ShowSuccess(isEdit ? "Event updated." : "Event created.", "Saved");
                NavigationManager.NavigateTo($"/events/{result.Value.Slug}");
            }
            else
            {
                ApplyErrors(result);
            }
        }
        finally
        {
            saving = false;
        }
    }

    private void ApplyErrors<T>(ServiceResult<T> result)
    {
        if (result.ValidationErrors.Count > 0 && editContext is not null && Model is not null && messageStore is not null)
        {
            foreach (var kv in result.ValidationErrors)
            {
                messageStore.Add(new FieldIdentifier(Model, kv.Key), kv.Value);
            }

            editContext.NotifyValidationStateChanged();
        }

        var message = result.ValidationErrors.Count > 0
            ? "Some fields need attention — see the messages below."
            : (result.Errors.Count > 0 ? string.Join(" ", result.Errors) : "Could not save the event.");
        ToastService.ShowError(message, "Save failed");
    }
}