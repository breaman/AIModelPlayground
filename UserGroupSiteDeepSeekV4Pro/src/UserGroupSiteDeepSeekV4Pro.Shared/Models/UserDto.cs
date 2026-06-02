namespace UserGroupSiteDeepSeekV4Pro.Shared.Models;

public class UserDto
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Email { get; set; } = "";
    public List<string> Roles { get; set; } = [];

    public string FullName =>
        string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? Email
            : $"{FirstName} {LastName}".Trim();
}
