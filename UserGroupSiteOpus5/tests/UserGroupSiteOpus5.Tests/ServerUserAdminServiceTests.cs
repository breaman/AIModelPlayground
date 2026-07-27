using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Server.Services;
using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;
using UserGroupSiteOpus5.Tests.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace UserGroupSiteOpus5.Tests;

public class ServerUserAdminServiceTests
{
    [Fact]
    public async Task AnAdminCannotRemoveTheirOwnAdminRole()
    {
        // The stated requirement. The disabled checkbox in the UI is cosmetic; this is the rule,
        // and it must hold against a hand-crafted request that never went near the checkbox.
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        db.SignIn(admin, RoleNames.Admin);

        var result = await service.UpdateUserRolesAsync(new UserRoleUpdateModel(admin.Id, IsAdmin: false, IsSpeaker: false));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("You cannot remove your own Admin role.");

        var userManager = db.GetService<UserManager<User>>();
        (await userManager.IsInRoleAsync(admin, RoleNames.Admin)).ShouldBeTrue();
    }

    [Fact]
    public async Task AnAdminCanChangeTheirOwnSpeakerRole()
    {
        // Only the Admin removal is guarded; giving up Speaker locks nobody out.
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        db.SignIn(admin, RoleNames.Admin);

        var result = await service.UpdateUserRolesAsync(new UserRoleUpdateModel(admin.Id, IsAdmin: true, IsSpeaker: true));

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));

        var userManager = db.GetService<UserManager<User>>();
        (await userManager.IsInRoleAsync(admin, RoleNames.Speaker)).ShouldBeTrue();
    }

    [Fact]
    public async Task AnAdminCanRemoveAnotherAdminsRole()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var actor = await db.CreateUserAsync("actor@test.local", "Ada", RoleNames.Admin);
        var target = await db.CreateUserAsync("target@test.local", "Tom", RoleNames.Admin);
        db.SignIn(actor, RoleNames.Admin);

        var result = await service.UpdateUserRolesAsync(new UserRoleUpdateModel(target.Id, IsAdmin: false, IsSpeaker: false));

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));

        var userManager = db.GetService<UserManager<User>>();
        (await userManager.IsInRoleAsync(target, RoleNames.Admin)).ShouldBeFalse();
    }

    [Fact]
    public async Task AnAdminCanGrantTheSpeakerRole()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        db.SignIn(admin, RoleNames.Admin);

        var result = await service.UpdateUserRolesAsync(new UserRoleUpdateModel(member.Id, IsAdmin: false, IsSpeaker: true));

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));

        var userManager = db.GetService<UserManager<User>>();
        (await userManager.IsInRoleAsync(member, RoleNames.Speaker)).ShouldBeTrue();
    }

    [Fact]
    public async Task ANonAdminCannotChangeRoles()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        var target = await db.CreateUserAsync("target@test.local", "Tom");
        db.SignIn(member);

        var result = await service.UpdateUserRolesAsync(new UserRoleUpdateModel(target.Id, IsAdmin: true, IsSpeaker: true));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Only an administrator can change roles.");
    }

    [Fact]
    public async Task ANonAdminSeesNoMembers()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        db.SignIn(member);

        (await service.GetUsersAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task TheMemberListReportsRoleAssignments()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        await db.CreateUserAsync("speaker@test.local", "Sam", RoleNames.Speaker);
        await db.CreateUserAsync("member@test.local", "Mel");
        db.SignIn(admin, RoleNames.Admin);

        var users = await service.GetUsersAsync();

        users.Count.ShouldBe(3);
        users.Single(x => x.FirstName == "Ada").IsAdmin.ShouldBeTrue();
        users.Single(x => x.FirstName == "Sam").IsSpeaker.ShouldBeTrue();
        users.Single(x => x.FirstName == "Sam").IsAdmin.ShouldBeFalse();
        users.Single(x => x.FirstName == "Mel").IsAdmin.ShouldBeFalse();
        users.Single(x => x.FirstName == "Mel").IsSpeaker.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangingRolesRefreshesTheTargetsSecurityStamp()
    {
        // Without this the target's existing auth cookie keeps the old roles until they sign in
        // again, so a granted role would appear not to work.
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        db.SignIn(admin, RoleNames.Admin);

        var userManager = db.GetService<UserManager<User>>();
        var before = await userManager.GetSecurityStampAsync(member);

        await service.UpdateUserRolesAsync(new UserRoleUpdateModel(member.Id, IsAdmin: false, IsSpeaker: true));

        (await userManager.GetSecurityStampAsync(member)).ShouldNotBe(before);
    }

    private static IUserAdminService CreateService(TestDatabase db)
    {
        return new ServerUserAdminService(
            db.DbContext,
            db.GetService<UserManager<User>>(),
            db.GetService<IHttpContextAccessor>(),
            NullLogger<ServerUserAdminService>.Instance);
    }
}
