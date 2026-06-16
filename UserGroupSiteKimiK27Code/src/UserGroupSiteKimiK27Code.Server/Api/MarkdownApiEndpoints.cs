using Markdig;

using Microsoft.AspNetCore.Authorization;

namespace UserGroupSiteKimiK27Code.Server.Api;

public static class MarkdownApiEndpoints
{
    public static IEndpointRouteBuilder MapMarkdownApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/markdown/preview", [Authorize] (PreviewRequest request) =>
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            var html = Markdown.ToHtml(request.Markdown ?? "", pipeline);
            return Results.Ok(new PreviewResponse { Html = html });
        }).WithTags("Markdown").DisableAntiforgery();

        return app;
    }
}

public sealed class PreviewRequest
{
    public string? Markdown { get; set; }
}

public sealed class PreviewResponse
{
    public string Html { get; set; } = "";
}