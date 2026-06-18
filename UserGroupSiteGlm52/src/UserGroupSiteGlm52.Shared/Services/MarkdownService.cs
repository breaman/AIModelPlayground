using Markdig;

namespace UserGroupSiteGlm52.Shared.Services;

/// <summary>
/// Markdig-backed <see cref="IMarkdownService"/>. The pipeline is built once and
/// reused; Markdig is thread-safe for rendering with a fixed pipeline.
/// </summary>
public sealed class MarkdownService : IMarkdownService
{
    // Common extensions covering headings, lists, code, links, tables, and task lists.
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <inheritdoc />
    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return Markdown.ToHtml(markdown, _pipeline);
    }
}