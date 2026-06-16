using UserGroupSiteGpt55.Shared.Users;

namespace UserGroupSiteGpt55.Tests;

public sealed class UserRoleRulesTests
{
    [Fact]
    public void RemovesCurrentUsersAdminRole_ReturnsTrueForSelfAdminRemoval()
    {
        var request = new UserRoleUpdateRequest(42, false, true);

        Assert.True(UserRoleRules.RemovesCurrentUsersAdminRole(42, request));
    }

    [Fact]
    public void RemovesCurrentUsersAdminRole_ReturnsFalseForOtherUser()
    {
        var request = new UserRoleUpdateRequest(42, false, true);

        Assert.False(UserRoleRules.RemovesCurrentUsersAdminRole(7, request));
    }
}
