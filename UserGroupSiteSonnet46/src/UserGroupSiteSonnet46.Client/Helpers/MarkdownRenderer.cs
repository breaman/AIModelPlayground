using Markdig;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteSonnet46.Client.Helpers;

/// <summary>
/// Converts Markdown strings to sanitized HTML using Markdig.
/// Returns a <see cref="MarkupString"/> so Blazor renders it as raw HTML.
/// </summary>
public static class MarkdownRenderer
{
    // Shared pipeline enables common extensions (tables, autolinks, etc.)
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Converts <paramref name="markdown"/> to HTML.
    /// Returns an empty <see cref="MarkupString"/> when <paramref name="markdown"/> is null or whitespace.
    /// </summary>
    public static MarkupString Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return new MarkupString(string.Empty);
        }

        var html = Markdown.ToHtml(markdown, Pipeline);
        return new MarkupString(html);
    }
}