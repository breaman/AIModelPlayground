using Markdig;

using Microsoft.AspNetCore.Components;

namespace UserGroupSiteSonnet46.Server.Helpers;

/// <summary>
/// Server-side Markdown to HTML converter using Markdig.
/// </summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>Converts Markdown to a <see cref="MarkupString"/> for Blazor rendering.</summary>
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
