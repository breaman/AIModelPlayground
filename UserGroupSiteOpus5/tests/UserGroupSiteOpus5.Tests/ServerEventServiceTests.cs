using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Server.Services;
using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;
using UserGroupSiteOpus5.Tests.Infrastructure;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace UserGroupSiteOpus5.Tests;

public class ServerEventServiceTests
{
    [Fact]
    public async Task PublishedEventsAreReturnedNewestFirstAndExcludeDrafts()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);

        db.DbContext.Events.AddRange(
            new Event { Title = "Oldest", Slug = "oldest", IsPublished = true, EventDateTime = Days(-30) },
            new Event { Title = "Newest", Slug = "newest", IsPublished = true, EventDateTime = Days(-1) },
            new Event { Title = "Middle", Slug = "middle", IsPublished = true, EventDateTime = Days(-10) },
            new Event { Title = "Draft", Slug = "draft", IsPublished = false, EventDateTime = Days(-2) });
        await db.DbContext.SaveChangesAsync();

        var results = await service.GetPublishedEventsAsync();

        results.Select(x => x.Title).ShouldBe(["Newest", "Middle", "Oldest"]);
    }

    [Fact]
    public async Task ADraftIsNotVisibleToAnAnonymousVisitor()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);

        db.DbContext.Events.Add(new Event { Title = "Draft", Slug = "draft", IsPublished = false });
        await db.DbContext.SaveChangesAsync();
        db.SignOut();

        (await service.GetEventBySlugAsync("draft")).ShouldBeNull();
    }

    [Fact]
    public async Task ADraftIsVisibleToAnAdmin()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);

        db.DbContext.Events.Add(new Event { Title = "Draft", Slug = "draft", IsPublished = false });
        await db.DbContext.SaveChangesAsync();
        db.SignIn(admin, RoleNames.Admin);

        var detail = await service.GetEventBySlugAsync("draft");

        detail.ShouldNotBeNull();
        detail.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public async Task ASpeakerSeesOnlyTheirOwnEventsInTheManageList()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var speaker = await db.CreateUserAsync("speaker@test.local", "Sam", RoleNames.Speaker);

        var mine = new Event { Title = "Mine", Slug = "mine" };
        mine.Speakers.Add(new EventSpeaker { UserId = speaker.Id });
        db.DbContext.Events.AddRange(mine, new Event { Title = "Theirs", Slug = "theirs" });
        await db.DbContext.SaveChangesAsync();
        db.SignIn(speaker, RoleNames.Speaker);

        var results = await service.GetManageableEventsAsync();

        results.Select(x => x.Title).ShouldBe(["Mine"]);
    }

    [Fact]
    public async Task AnAdminSeesEveryEventInTheManageList()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);

        db.DbContext.Events.AddRange(
            new Event { Title = "One", Slug = "one" },
            new Event { Title = "Two", Slug = "two" });
        await db.DbContext.SaveChangesAsync();
        db.SignIn(admin, RoleNames.Admin);

        (await service.GetManageableEventsAsync()).Count.ShouldBe(2);
    }

    [Fact]
    public async Task SlugCollisionsProduceUniqueSlugs()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);

        db.DbContext.Events.AddRange(
            new Event { Title = "Test Meeting", Slug = "test-meeting" },
            new Event { Title = "Test Meeting", Slug = "test-meeting-2" });
        await db.DbContext.SaveChangesAsync();

        (await service.GenerateUniqueSlugAsync("Test Meeting", null)).ShouldBe("test-meeting-3");
    }

    [Fact]
    public async Task AnEventDoesNotCollideWithItsOwnSlug()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);

        var existing = new Event { Title = "Test Meeting", Slug = "test-meeting" };
        db.DbContext.Events.Add(existing);
        await db.DbContext.SaveChangesAsync();

        (await service.GenerateUniqueSlugAsync("Test Meeting", existing.Id)).ShouldBe("test-meeting");
    }

    [Fact]
    public async Task SavingADuplicateSlugIsRejectedRatherThanThrowing()
    {
        // The unique index is the real guard; the service must turn its violation into a message.
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        db.SignIn(admin, RoleNames.Admin);

        db.DbContext.Events.Add(new Event { Title = "Taken", Slug = "taken" });
        await db.DbContext.SaveChangesAsync();

        var result = await service.SaveEventAsync(new EventEditModel { Title = "Another", Slug = "taken" });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(x => x.Contains("already in use"));
    }

    [Fact]
    public async Task AHandTypedSlugIsNormalisedBeforeItIsStored()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        db.SignIn(admin, RoleNames.Admin);

        var result = await service.SaveEventAsync(new EventEditModel { Title = "Meeting", Slug = "My Slug!" });

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        (await db.DbContext.Events.AsNoTracking().FirstAsync(x => x.Id == result.Value))
            .Slug.ShouldBe("my-slug");
    }

    [Fact]
    public async Task ASlugWithNoUsableCharactersIsRejected()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        db.SignIn(admin, RoleNames.Admin);

        var result = await service.SaveEventAsync(new EventEditModel { Title = "Meeting", Slug = "!!!" });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(x => x.Contains("at least one letter or digit"));
    }

    [Fact]
    public async Task AnAdminCanCreateADraftWithOnlyATitleAndSlug()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        db.SignIn(admin, RoleNames.Admin);

        var result = await service.SaveEventAsync(new EventEditModel { Title = "Draft", Slug = "draft" });

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        result.Value.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ASpeakerCannotCreateAnEvent()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var speaker = await db.CreateUserAsync("speaker@test.local", "Sam", RoleNames.Speaker);
        db.SignIn(speaker, RoleNames.Speaker);

        var result = await service.SaveEventAsync(new EventEditModel { Title = "Draft", Slug = "draft" });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(x => x.Contains("administrator"));
    }

    [Fact]
    public async Task PublishingWithoutASpeakerIsRejectedServerSide()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        db.SignIn(admin, RoleNames.Admin);

        var result = await service.SaveEventAsync(new EventEditModel
        {
            Title = "Incomplete",
            Slug = "incomplete",
            Description = "Something",
            EventDateTime = Days(7),
            Location = "Hall",
            IsPublished = true
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("At least one speaker is required to publish an event.");
    }

    [Fact]
    public async Task AnUnassignedSpeakerCannotEditSomeoneElsesEvent()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var assigned = await db.CreateUserAsync("assigned@test.local", "Ann", RoleNames.Speaker);
        var other = await db.CreateUserAsync("other@test.local", "Otto", RoleNames.Speaker);

        var meeting = new Event { Title = "Theirs", Slug = "theirs" };
        meeting.Speakers.Add(new EventSpeaker { UserId = assigned.Id });
        db.DbContext.Events.Add(meeting);
        await db.DbContext.SaveChangesAsync();

        db.SignIn(other, RoleNames.Speaker);

        var result = await service.SaveEventAsync(new EventEditModel
        {
            Id = meeting.Id,
            Title = "Hijacked",
            Slug = "hijacked"
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(x => x.Contains("permission"));
    }

    [Fact]
    public async Task AnAssignedSpeakerCanEditTheirOwnEvent()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var speaker = await db.CreateUserAsync("speaker@test.local", "Sam", RoleNames.Speaker);

        var meeting = new Event { Title = "Mine", Slug = "mine" };
        meeting.Speakers.Add(new EventSpeaker { UserId = speaker.Id });
        db.DbContext.Events.Add(meeting);
        await db.DbContext.SaveChangesAsync();

        db.SignIn(speaker, RoleNames.Speaker);

        var result = await service.SaveEventAsync(new EventEditModel
        {
            Id = meeting.Id,
            Title = "Mine, updated",
            Slug = "mine",
            SpeakerUserIds = [speaker.Id]
        });

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        (await db.DbContext.Events.AsNoTracking().FirstAsync(x => x.Id == meeting.Id))
            .Title.ShouldBe("Mine, updated");
    }

    [Fact]
    public async Task AssigningAUserWhoIsNotASpeakerIsRejected()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        var ordinary = await db.CreateUserAsync("member@test.local", "Mel");
        db.SignIn(admin, RoleNames.Admin);

        var result = await service.SaveEventAsync(new EventEditModel
        {
            Title = "Meeting",
            Slug = "meeting",
            SpeakerUserIds = [ordinary.Id]
        });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(x => x.Contains("Speaker role"));
    }

    [Fact]
    public async Task AvailableSpeakersListsOnlyMembersInTheSpeakerRole()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        await db.CreateUserAsync("speaker@test.local", "Sam", RoleNames.Speaker);
        await db.CreateUserAsync("member@test.local", "Mel");

        var speakers = await service.GetAvailableSpeakersAsync();

        speakers.Select(x => x.FirstName).ShouldBe(["Sam"]);
    }

    [Fact]
    public async Task SavingSynchronisesSpeakerAssignments()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var admin = await db.CreateUserAsync("admin@test.local", "Ada", RoleNames.Admin);
        var first = await db.CreateUserAsync("first@test.local", "Fay", RoleNames.Speaker);
        var second = await db.CreateUserAsync("second@test.local", "Sid", RoleNames.Speaker);
        db.SignIn(admin, RoleNames.Admin);

        var created = await service.SaveEventAsync(new EventEditModel
        {
            Title = "Meeting",
            Slug = "meeting",
            SpeakerUserIds = [first.Id]
        });

        await service.SaveEventAsync(new EventEditModel
        {
            Id = created.Value,
            Title = "Meeting",
            Slug = "meeting",
            SpeakerUserIds = [second.Id]
        });

        var assignments = await db.DbContext.EventSpeakers
            .AsNoTracking()
            .Where(x => x.EventId == created.Value)
            .Select(x => x.UserId)
            .ToListAsync();

        assignments.ShouldBe([second.Id]);
    }

    private static DateTimeOffset Days(int offset)
    {
        return DateTimeOffset.UtcNow.AddDays(offset);
    }

    private static IEventService CreateService(TestDatabase db)
    {
        return new ServerEventService(
            db.DbContext,
            db.GetService<IAuthorizationService>(),
            db.GetService<IHttpContextAccessor>(),
            NullLogger<ServerEventService>.Instance);
    }
}
