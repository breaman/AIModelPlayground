using UserGroupSiteGpt55.Shared.Events;

namespace UserGroupSiteGpt55.Tests;

public sealed class SlugGeneratorTests
{
    [Theory]
    [InlineData("Intro to .NET and AI", "intro-to-net-and-ai")]
    [InlineData("  Blazor/WebAssembly 101!  ", "blazor-webassembly-101")]
    [InlineData("Café con Leche", "cafe-con-leche")]
    public void Generate_ReturnsKebabCaseSlug(string title, string expected)
    {
        var slug = SlugGenerator.Generate(title);

        Assert.Equal(expected, slug);
    }
}
