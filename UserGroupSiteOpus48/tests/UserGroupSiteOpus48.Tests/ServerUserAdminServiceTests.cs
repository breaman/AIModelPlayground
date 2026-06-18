using Microsoft.AspNetCore.Identity;

using NSubstitute;

using UserGroupSiteOpus48.Data.Models;
using UserGroupSiteOpus48.Server.Services;
using UserGroupSiteOpus48.Shared.Authorization;
using UserGroupSiteOpus48.Shared.Dtos;

using Xunit;

namespace UserGroupSiteOpus48.Tests;

public class ServerUserAdminServiceTests
{
    private static UserManager<User> MockUserManager() =>
        Substitute.For<UserManager<User>>(
            Substitute.For<IUserStore<User>>(), null, null, null, null, null, null, null, null);

    [Fact]
    public async Task AdminCannotRemoveTheirOwnAdminRole()
    {
        const int currentUserId = 5;
        var service = new ServerUserAdminService(MockUserManager(), new TestUserService(currentUserId));

        var result = await service.SetRoleAsync(new SetRoleRequest(currentUserId, RoleNames.Admin, InRole: false));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("own Admin role"));
    }

    [Fact]
    public async Task UnknownRoleIsRejected()
    {
        var service = new ServerUserAdminService(MockUserManager(), new TestUserService(1));

        var result = await service.SetRoleAsync(new SetRoleRequest(2, "Wizard", InRole: true));

        Assert.False(result.Success);
    }
}