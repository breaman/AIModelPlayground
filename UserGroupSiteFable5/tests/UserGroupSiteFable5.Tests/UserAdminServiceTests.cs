using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using UserGroupSiteFable5.Data.Models;
using UserGroupSiteFable5.Server.Services;
using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

using Xunit;

namespace UserGroupSiteFable5.Tests;

public class UserAdminServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly UserManager<User> _userManager;

    private const int ActingAdminId = 1;
    private const int OtherUserId = 2;

    public UserAdminServiceTests()
    {
        var store = new UserStore<User, Role, ApplicationDbContext, int>(_database.Context);
        _userManager = new UserManager<User>(store, Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(), [], [], new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, NullLogger<UserManager<User>>.Instance);

        _database.AddUser(ActingAdminId, "admin@example.com");
        _database.AddUser(OtherUserId, "member@example.com");
        _userManager.AddToRoleAsync(_database.Context.Users.Find(ActingAdminId)!, "Admin")
            .GetAwaiter().GetResult();
    }

    private UserAdminService CreateService(int actingUserId)
    {
        return new UserAdminService(_database.Context, _userManager, new FakeUserService(actingUserId));
    }

    [Fact]
    public async Task RemovingOwnAdminRole_IsRejected()
    {
        var service = CreateService(ActingAdminId);

        var result = await service.UpdateUserRolesAsync(ActingAdminId,
            new UpdateUserRolesDto { IsAdmin = false, IsSpeaker = false });

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);

        var user = await _userManager.FindByIdAsync(ActingAdminId.ToString());
        Assert.True(await _userManager.IsInRoleAsync(user!, "Admin"));
    }

    [Fact]
    public async Task KeepingOwnAdminRole_WhileTogglingSpeaker_Succeeds()
    {
        var service = CreateService(ActingAdminId);

        var result = await service.UpdateUserRolesAsync(ActingAdminId,
            new UpdateUserRolesDto { IsAdmin = true, IsSpeaker = true });

        Assert.True(result.Success);

        var user = await _userManager.FindByIdAsync(ActingAdminId.ToString());
        Assert.True(await _userManager.IsInRoleAsync(user!, "Admin"));
        Assert.True(await _userManager.IsInRoleAsync(user!, "Speaker"));
    }

    [Fact]
    public async Task GrantingAndRevokingRolesOnAnotherUser_Succeeds()
    {
        var service = CreateService(ActingAdminId);

        var grant = await service.UpdateUserRolesAsync(OtherUserId,
            new UpdateUserRolesDto { IsAdmin = true, IsSpeaker = true });
        Assert.True(grant.Success);

        var revoke = await service.UpdateUserRolesAsync(OtherUserId,
            new UpdateUserRolesDto { IsAdmin = false, IsSpeaker = false });
        Assert.True(revoke.Success);

        var user = await _userManager.FindByIdAsync(OtherUserId.ToString());
        Assert.False(await _userManager.IsInRoleAsync(user!, "Admin"));
        Assert.False(await _userManager.IsInRoleAsync(user!, "Speaker"));
    }

    [Fact]
    public async Task UnknownUser_ReturnsNotFound()
    {
        var service = CreateService(ActingAdminId);

        var result = await service.UpdateUserRolesAsync(999,
            new UpdateUserRolesDto { IsAdmin = true, IsSpeaker = false });

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    public void Dispose()
    {
        _userManager.Dispose();
        _database.Dispose();
    }
}