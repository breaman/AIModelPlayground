using Markdig;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteFable5.Server.Services;

/// <summary>
/// Renders Markdown to HTML for server-side (SSR) pages using the same safety settings
/// as the client preview: raw HTML is disabled to prevent XSS.
/// </summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .Build();

    public static MarkupString ToHtml(string? markdown)
    {
        return (MarkupString)Markdown.ToHtml(markdown ?? string.Empty, Pipeline);
    }
}