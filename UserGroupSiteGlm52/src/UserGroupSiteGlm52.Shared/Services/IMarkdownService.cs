namespace UserGroupSiteGlm52.Shared.Services;

/// <summary>
/// Renders Markdown to HTML using Markdig. A single implementation
/// (<see cref="MarkdownService"/>) is shared by the Server (public detail page)
/// and the Client (live preview). Stored/public content is sanitized server-side
/// after rendering; the WASM preview renders Markdig output directly because it
/// is the editor's own typing.
/// </summary>
public interface IMarkdownService
{
    /// <summary>Render <paramref name="markdown"/> to an HTML string.</summary>
    string ToHtml(string? markdown);
}