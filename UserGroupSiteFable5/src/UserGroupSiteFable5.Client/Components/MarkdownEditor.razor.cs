using Markdig;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteFable5.Client.Components;

/// <summary>
/// Reusable Markdown editor with Bootstrap Edit/Preview tabs. The preview renders the
/// current (unsaved) text using Markdig with raw HTML disabled to prevent XSS.
/// </summary>
public partial class MarkdownEditor : ComponentBase
{
    // DisableHtml ensures any raw HTML in the Markdown is escaped rather than rendered.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .Build();

    [Parameter] public string? Value { get; set; }

    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Element id for the textarea so an external label can target it.</summary>
    [Parameter] public string Id { get; set; } = "markdown-editor";

    [Parameter] public int Rows { get; set; } = 12;

    private bool _showPreview;

    private MarkupString PreviewHtml => (MarkupString)Markdown.ToHtml(Value ?? string.Empty, Pipeline);

    private void ShowEdit()
    {
        _showPreview = false;
    }

    private void ShowPreview()
    {
        _showPreview = true;
    }

    private async Task OnInputAsync(ChangeEventArgs e)
    {
        Value = e.Value?.ToString();
        await ValueChanged.InvokeAsync(Value);
    }
}
