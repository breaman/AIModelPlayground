using Markdig;

namespace UserGroupSiteGpt55.Shared.Markdown;

/// <summary>
/// Renders markdown with raw HTML disabled so saved descriptions and previews share safe behavior.
/// </summary>
public class MarkdigMarkdownRenderer : IMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    /// <summary>
    /// Converts markdown into HTML with unsafe raw HTML suppressed by Markdig.
    /// </summary>
    public string Render(string? markdown)
    {
        return string.IsNullOrWhiteSpace(markdown) ? "" : global::Markdig.Markdown.ToHtml(markdown, Pipeline);
    }
}