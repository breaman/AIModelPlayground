using UserGroupSiteFable5.Shared.Helpers;

using Xunit;

namespace UserGroupSiteFable5.Tests;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Intro to Blazor", "intro-to-blazor")]
    [InlineData("Hello, World!", "hello-world")]
    [InlineData("  Leading and trailing  ", "leading-and-trailing")]
    [InlineData("Multiple---separators___here", "multiple-separators-here")]
    [InlineData("C# 14 & .NET 10", "c-14-net-10")]
    [InlineData("already-a-slug", "already-a-slug")]
    [InlineData("UPPERCASE", "uppercase")]
    [InlineData("123 numbers first", "123-numbers-first")]
    public void GenerateSlug_KebabCasesTitles(string input, string expected)
    {
        Assert.Equal(expected, SlugHelper.GenerateSlug(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void GenerateSlug_ReturnsEmptyForNoAlphanumerics(string? input)
    {
        Assert.Equal(string.Empty, SlugHelper.GenerateSlug(input));
    }
}
