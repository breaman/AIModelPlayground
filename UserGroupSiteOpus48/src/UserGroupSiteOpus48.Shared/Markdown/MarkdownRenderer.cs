using Markdig;

namespace UserGroupSiteOpus48.Shared.Markdown;

/// <summary>
/// Shared Markdown-to-HTML helper wrapping <see href="https://github.com/xoofx/markdig">Markdig</see>.
/// Used by the Client (live Preview tab) and the Server (rendering stored descriptions) so both
/// produce identical output. Raw inline HTML is disabled to keep rendered user content safe.
/// </summary>
public static class MarkdownRenderer
{
    // A single shared pipeline is thread-safe and cheap to reuse. We enable a conservative set of
    // advanced extensions (tables, lists, links) but deliberately do NOT enable raw HTML pass-through,
    // so user-supplied <script>/<iframe> markup is encoded rather than executed.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    /// <summary>
    /// Converts the supplied Markdown text to sanitized HTML. Returns an empty string for null/blank input.
    /// </summary>
    /// <param name="markdown">The raw Markdown text to render.</param>
    /// <returns>HTML markup safe to render with <c>MarkupString</c>.</returns>
    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return Markdig.Markdown.ToHtml(markdown, Pipeline);
    }
}