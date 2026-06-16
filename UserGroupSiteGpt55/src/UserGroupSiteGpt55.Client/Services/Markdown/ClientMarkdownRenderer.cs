using UserGroupSiteGpt55.Shared.Markdown;

namespace UserGroupSiteGpt55.Client.Services.Markdown;

/// <summary>
/// Renders markdown in WebAssembly with the same Markdig pipeline used on the server.
/// </summary>
public sealed class ClientMarkdownRenderer : MarkdigMarkdownRenderer;
