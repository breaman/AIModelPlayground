using UserGroupSiteOpus48.Data.Interfaces;

namespace UserGroupSiteOpus48.Tests;

/// <summary>Minimal <see cref="IUserService"/> stub returning a fixed current-user id for tests.</summary>
public sealed class TestUserService(int userId) : IUserService
{
    public int UserId { get; } = userId;
}