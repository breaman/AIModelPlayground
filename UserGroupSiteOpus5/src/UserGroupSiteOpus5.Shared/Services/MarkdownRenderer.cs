using Markdig;

namespace UserGroupSiteOpus5.Shared.Services;

/// <summary>
/// Renders user-authored Markdown (event descriptions) to HTML.
/// </summary>
/// <remarks>
/// <para>
/// Uses the Markdig library. The pipeline is built once and shared: constructing it per call is
/// measurably expensive, and the live preview in the event editor re-renders on every keystroke.
/// </para>
/// <para>
/// <b>Security:</b> <c>DisableHtml()</c> is the control that makes the output safe to emit through
/// a <c>MarkupString</c>. It escapes raw HTML embedded in the Markdown source, so a description
/// containing <c>&lt;script&gt;</c> renders as visible text rather than executing. Removing it
/// turns every event description into a stored-XSS vector. If richer HTML is ever required, add a
/// server-side sanitiser instead of relaxing this setting.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// @((MarkupString)MarkdownRenderer.ToHtml(model.Description))
/// </code>
/// </example>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    /// <summary>
    /// Converts Markdown source to HTML.
    /// </summary>
    /// <param name="markdown">The Markdown source. May be null or whitespace.</param>
    /// <returns>The rendered HTML, or an empty string when there is nothing to render.</returns>
    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return "";
        }

        return Markdown.ToHtml(markdown, Pipeline);
    }

    /// <summary>
    /// Converts Markdown source to plain text, for use where markup cannot be rendered such as a
    /// meta description or a listing teaser.
    /// </summary>
    /// <param name="markdown">The Markdown source. May be null or whitespace.</param>
    /// <returns>The plain-text rendering, or an empty string when there is nothing to render.</returns>
    public static string ToPlainText(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return "";
        }

        return Markdown.ToPlainText(markdown, Pipeline);
    }
}
