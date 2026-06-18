namespace UserGroupSiteGpt55.Shared.Markdown;

/// <summary>
/// Renders user-authored markdown into safe HTML.
/// </summary>
public interface IMarkdownRenderer
{
    string Render(string? markdown);
}