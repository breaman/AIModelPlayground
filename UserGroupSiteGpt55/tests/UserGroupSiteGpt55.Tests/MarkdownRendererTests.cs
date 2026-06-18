using UserGroupSiteGpt55.Shared.Markdown;

namespace UserGroupSiteGpt55.Tests;

public sealed class MarkdownRendererTests
{
    [Fact]
    public void Render_FormatsMarkdownAndSuppressesRawHtml()
    {
        var renderer = new MarkdigMarkdownRenderer();

        var html = renderer.Render("**Hello** <script>alert('x')</script>");

        Assert.Contains("<strong>Hello</strong>", html);
        Assert.DoesNotContain("<script>", html);
    }
}