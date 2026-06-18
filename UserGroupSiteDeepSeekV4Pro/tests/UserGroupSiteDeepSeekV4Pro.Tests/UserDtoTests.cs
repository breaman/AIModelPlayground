using UserGroupSiteDeepSeekV4Pro.Shared.Models;

namespace UserGroupSiteDeepSeekV4Pro.Tests;

public class UserDtoTests
{
    [Fact]
    public void FullName_WithFirstAndLastName_ReturnsFullName()
    {
        var dto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        Assert.Equal("John Doe", dto.FullName);
    }

    [Fact]
    public void FullName_WithOnlyFirstName_ReturnsFirstName()
    {
        var dto = new UserDto
        {
            FirstName = "John",
            LastName = null,
            Email = "john@example.com"
        };

        Assert.Equal("John", dto.FullName);
    }

    [Fact]
    public void FullName_WithOnlyLastName_ReturnsLastName()
    {
        var dto = new UserDto
        {
            FirstName = null,
            LastName = "Doe",
            Email = "john@example.com"
        };

        Assert.Equal("Doe", dto.FullName);
    }

    [Fact]
    public void FullName_WithNoName_ReturnsEmail()
    {
        var dto = new UserDto
        {
            FirstName = null,
            LastName = null,
            Email = "john@example.com"
        };

        Assert.Equal("john@example.com", dto.FullName);
    }

    [Fact]
    public void FullName_WithEmptyStrings_ReturnsEmail()
    {
        var dto = new UserDto
        {
            FirstName = "",
            LastName = "",
            Email = "john@example.com"
        };

        Assert.Equal("john@example.com", dto.FullName);
    }
}