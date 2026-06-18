using Microsoft.AspNetCore.Components;

using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Client.Components;

/// <summary>
/// Reusable Markdown editor with Bootstrap Edit/Preview tabs. The Preview tab
/// renders the editor's own typing via <see cref="IMarkdownService"/> as a
/// <see cref="MarkupString"/>. Stored/public content is sanitized server-side,
/// so the WASM preview renders Markdig output directly (no sanitizer shipped to the browser).
/// </summary>
public partial class MarkdownEditor : ComponentBase
{
    [Inject]
    private IMarkdownService MarkdownService { get; set; } = default!;

    /// <summary>The Markdown source, two-way bound by the parent.</summary>
    [Parameter]
    public string Value { get; set; } = "";

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public string Id { get; set; } = "markdown-editor";

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string Placeholder { get; set; } = "Write your Markdown here…";

    private bool showPreview;
    private MarkupString previewHtml;

    /// <summary>Wrapper that propagates edits to the parent via <see cref="ValueChanged"/>.</summary>
    private string CurrentValue
    {
        get => Value;
        set
        {
            if (Value != value)
            {
                Value = value;
                _ = ValueChanged.InvokeAsync(value);
            }
        }
    }

    private void ShowEdit() => showPreview = false;

    private void ShowPreview()
    {
        showPreview = true;
        previewHtml = new MarkupString(MarkdownService.ToHtml(Value));
    }
}