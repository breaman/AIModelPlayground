using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteGlm51.Client.Components;

/// <summary>
/// A reusable Markdown editor with Edit/Preview tabs.
/// Uses the server-side Markdig API endpoint for rendering preview.
/// </summary>
public partial class MarkdownEditor : ComponentBase
{
    [Inject]
    private HttpClient Http { get; set; } = default!;

    /// <summary>
    /// The Markdown content, two-way bound.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// HTML id attribute for the textarea element.
    /// </summary>
    [Parameter]
    public string Id { get; set; } = "markdown-editor";

    /// <summary>
    /// Currently active tab: "edit" or "preview".
    /// </summary>
    public string ActiveTab { get; set; } = "edit";

    /// <summary>
    /// Rendered HTML preview content.
    /// </summary>
    public string PreviewHtml { get; set; } = string.Empty;

    /// <summary>
    /// Whether the preview is currently loading.
    /// </summary>
    public bool IsLoadingPreview { get; set; }

    private async Task OnValueChanged()
    {
        await ValueChanged.InvokeAsync(Value);
    }

    private void SwitchToEdit()
    {
        ActiveTab = "edit";
    }

    private async Task ShowPreview()
    {
        ActiveTab = "preview";

        if (string.IsNullOrWhiteSpace(Value))
        {
            PreviewHtml = string.Empty;
            return;
        }

        IsLoadingPreview = true;

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { Markdown = Value }),
                Encoding.UTF8,
                "application/json");

            var response = await Http.PostAsync("/api/markdown/preview", content);
            if (response.IsSuccessStatusCode)
            {
                PreviewHtml = await response.Content.ReadAsStringAsync();
            }
            else
            {
                PreviewHtml = $"<p class=\"text-danger\">Error rendering preview: {response.StatusCode}</p>";
            }
        }
        catch (Exception ex)
        {
            PreviewHtml = $"<p class=\"text-danger\">Error rendering preview: {ex.Message}</p>";
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }
}