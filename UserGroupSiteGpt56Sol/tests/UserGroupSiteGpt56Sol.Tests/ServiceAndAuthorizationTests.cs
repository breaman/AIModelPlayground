using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using UserGroupSiteGpt56Sol.Data.Models;
using UserGroupSiteGpt56Sol.Server.Authorization;
using UserGroupSiteGpt56Sol.Server.Services;
using UserGroupSiteGpt56Sol.Shared.Authorization;
using UserGroupSiteGpt56Sol.Shared.Models;

namespace UserGroupSiteGpt56Sol.Tests;

public sealed class ServiceAndAuthorizationTests
{
    [Fact]
    public async Task PublishedEventQueryFiltersDraftsAndOrdersDescending()
    {
        await using var context = CreateContext();
        context.Events.AddRange(
            CreateEvent("older", new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc), true),
            CreateEvent("newer", new DateTime(2026, 2, 1, 18, 0, 0, DateTimeKind.Utc), true),
            CreateEvent("draft", new DateTime(2026, 3, 1, 18, 0, 0, DateTimeKind.Utc), false));
        await context.SaveChangesAsync();
        var service = CreateEventService(context, CreateCurrentUser(1));

        var events = await service.GetPublishedAsync();

        Assert.Equal(["newer", "older"], events.Select(item => item.Slug));
    }

    [Fact]
    public async Task EventEditorRequirementAllowsAssignedSpeakerButNotAnotherUser()
    {
        await using var context = CreateContext();
        var user = new User { Id = 4, UserName = "speaker@example.com", MemberSince = DateTime.UtcNow };
        var eventEntity = CreateEvent("assigned", DateTime.UtcNow, false);
        context.Users.Add(user);
        context.Events.Add(eventEntity);
        eventEntity.EventSpeakers.Add(new EventSpeaker { UserId = user.Id });
        await context.SaveChangesAsync();
        var requirement = new EventEditorRequirement();
        var handler = new EventEditorAuthorizationHandler(context);

        var assignedContext = new AuthorizationHandlerContext([requirement], CreatePrincipal(4), eventEntity.Id);
        await handler.HandleAsync(assignedContext);
        var otherContext = new AuthorizationHandlerContext([requirement], CreatePrincipal(5), eventEntity.Id);
        await handler.HandleAsync(otherContext);

        Assert.True(assignedContext.HasSucceeded);
        Assert.False(otherContext.HasSucceeded);
    }

    [Fact]
    public async Task AdministratorCannotRemoveOwnAdminRole()
    {
        await using var context = CreateContext();
        var user = new User
        {
            Id = 7,
            UserName = "admin@example.com",
            Email = "admin@example.com",
            NormalizedUserName = "ADMIN@EXAMPLE.COM",
            MemberSince = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var userManager = CreateUserManager(context);
        var service = new UserAdministrationService(context, userManager,
            CreateCurrentUser(7, AppRoles.Admin), NullLogger<UserAdministrationService>.Instance);

        var result = await service.UpdateRolesAsync(7, new UpdateUserRolesRequest { IsAdmin = false });

        Assert.False(result.Succeeded);
        Assert.Contains("cannot remove your own", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VotingTwiceIsIdempotentAndExistingVolunteerWins()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { Id = 2, UserName = "voter", MemberSince = DateTime.UtcNow },
            new User { Id = 9, UserName = "volunteer", MemberSince = DateTime.UtcNow });
        var topic = new TopicSuggestion { Title = "Concurrency", CreatedBy = 2, VolunteerUserId = 9 };
        context.TopicSuggestions.Add(topic);
        await context.SaveChangesAsync();
        var service = new TopicService(context, CreateCurrentUser(2), NullLogger<TopicService>.Instance);

        var firstVote = await service.VoteAsync(topic.Id);
        var secondVote = await service.VoteAsync(topic.Id);
        var volunteer = await service.VolunteerAsync(topic.Id);

        Assert.True(firstVote.Succeeded);
        Assert.True(secondVote.Succeeded);
        Assert.Single(context.TopicVotes);
        Assert.False(volunteer.Succeeded);
        Assert.Contains("already volunteered", volunteer.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Creates an isolated in-memory application context.</summary>
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>Creates a minimal valid event entity.</summary>
    private static Event CreateEvent(string slug, DateTime startsAtUtc, bool published) => new()
    {
        Title = slug,
        Slug = slug,
        NormalizedSlug = slug,
        DescriptionMarkdown = "Details",
        StartsAtUtc = startsAtUtc,
        Location = "Online",
        IsPublished = published
    };

    /// <summary>Creates the event service for query-focused tests.</summary>
    private static EventService CreateEventService(ApplicationDbContext context, CurrentUserAccessor user) =>
        new(context, user, null!, NullLogger<EventService>.Instance);

    /// <summary>Creates a current-user accessor backed by test HTTP claims.</summary>
    private static CurrentUserAccessor CreateCurrentUser(int userId, params string[] roles)
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = CreatePrincipal(userId, roles) }
        };
        return new CurrentUserAccessor(accessor);
    }

    /// <summary>Creates an authenticated principal with a stable integer identifier.</summary>
    private static ClaimsPrincipal CreatePrincipal(int userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>Creates a real Identity user manager over the in-memory EF store.</summary>
    private static UserManager<User> CreateUserManager(ApplicationDbContext context)
    {
        var store = new UserStore<User, Role, ApplicationDbContext, int>(context);
        return new UserManager<User>(store, Options.Create(new IdentityOptions()), new PasswordHasher<User>(),
            [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(), NullLogger<UserManager<User>>.Instance);
    }
}
