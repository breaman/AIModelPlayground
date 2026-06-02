using Markdig;

namespace UserGroupSiteGlm51.Server.Endpoints;

/// <summary>
/// Minimal API endpoint for rendering Markdown to HTML for the Markdown editor preview.
/// </summary>
public static class MarkdownEndpoints
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static void MapMarkdownEndpoints(this WebApplication app)
    {
        app.MapPost("/api/markdown/preview", (MarkdownRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Markdown))
            {
                return Results.Ok(string.Empty);
            }

            var html = Markdig.Markdown.ToHtml(request.Markdown, Pipeline);
            return Results.Ok(html);
        }).RequireAuthorization();
    }
}

/// <summary>
/// Request body for the Markdown preview endpoint.
/// </summary>
public record MarkdownRequest(string Markdown);