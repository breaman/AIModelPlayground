using UserGroupSiteOpus48.Shared.Text;

using Xunit;

namespace UserGroupSiteOpus48.Tests;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("Hello, World!", "hello-world")]
    [InlineData("  Multiple   Spaces--and__punct!! ", "multiple-spaces-and-punct")]
    [InlineData("C# .NET 10", "c-net-10")]
    [InlineData("Already-Kebab-Case", "already-kebab-case")]
    public void ToKebabCaseProducesExpectedSlug(string input, string expected)
    {
        Assert.Equal(expected, SlugGenerator.ToKebabCase(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void ToKebabCaseReturnsEmptyForNoUsableCharacters(string? input)
    {
        Assert.Equal(string.Empty, SlugGenerator.ToKebabCase(input));
    }
}
