using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Tests.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace UserGroupSiteOpus5.Tests;

public class EventEditorAuthorizationHandlerTests
{
    [Fact]
    public async Task AnAdminCanEditAnyEvent()
    {
        using var db = new TestDatabase();
        var eventId = await CreateEventAsync(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        db.SignIn(admin, RoleNames.Admin);

        (await AuthorizeAsync(db, eventId)).ShouldBeTrue();
    }

    [Fact]
    public async Task AnAssignedSpeakerCanEditTheirOwnEvent()
    {
        using var db = new TestDatabase();
        var speaker = await db.CreateUserAsync("speaker@test.local", "Sam", RoleNames.Speaker);
        var eventId = await CreateEventAsync(db, speaker.Id);
        db.SignIn(speaker, RoleNames.Speaker);

        (await AuthorizeAsync(db, eventId)).ShouldBeTrue();
    }

    [Fact]
    public async Task AnUnassignedSpeakerCannotEditSomeoneElsesEvent()
    {
        using var db = new TestDatabase();
        var assigned = await db.CreateUserAsync("assigned@test.local", "Ann", RoleNames.Speaker);
        var other = await db.CreateUserAsync("other@test.local", "Otto", RoleNames.Speaker);
        var eventId = await CreateEventAsync(db, assigned.Id);
        db.SignIn(other, RoleNames.Speaker);

        (await AuthorizeAsync(db, eventId)).ShouldBeFalse();
    }

    [Fact]
    public async Task AMemberWithNoRolesCannotEdit()
    {
        using var db = new TestDatabase();
        var eventId = await CreateEventAsync(db);
        var member = await db.CreateUserAsync("member@test.local", "Mel");
        db.SignIn(member);

        (await AuthorizeAsync(db, eventId)).ShouldBeFalse();
    }

    [Fact]
    public async Task AnAnonymousVisitorCannotEdit()
    {
        using var db = new TestDatabase();
        var eventId = await CreateEventAsync(db);
        db.SignOut();

        (await AuthorizeAsync(db, eventId)).ShouldBeFalse();
    }

    [Fact]
    public async Task ASpeakerCannotCreateANewEvent()
    {
        // A new event has id 0 and therefore no speaker assignments, so only an admin satisfies
        // the policy.
        using var db = new TestDatabase();
        var speaker = await db.CreateUserAsync("speaker@test.local", "Sam", RoleNames.Speaker);
        db.SignIn(speaker, RoleNames.Speaker);

        (await AuthorizeAsync(db, eventId: 0)).ShouldBeFalse();
    }

    private static async Task<bool> AuthorizeAsync(TestDatabase db, int eventId)
    {
        var authorizationService = db.GetService<IAuthorizationService>();
        var result = await authorizationService.AuthorizeAsync(db.CurrentUser, eventId, PolicyNames.EventEditor);
        return result.Succeeded;
    }

    private static async Task<int> CreateEventAsync(TestDatabase db, int? speakerUserId = null)
    {
        var meeting = new Event { Title = "A meeting", Slug = "a-meeting" };

        if (speakerUserId is { } userId)
        {
            meeting.Speakers.Add(new EventSpeaker { UserId = userId });
        }

        db.DbContext.Events.Add(meeting);
        await db.DbContext.SaveChangesAsync();

        return meeting.Id;
    }
}
