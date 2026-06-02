using UserGroupSiteKimiK26.Data.Models;

using Microsoft.AspNetCore.Identity;

namespace UserGroupSiteKimiK26.Server.Services;

public static class SeedDataService
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Seed admin user
        const string adminEmail = "admin@example.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                MemberSince = DateTime.UtcNow
            };
            await userManager.CreateAsync(admin, "password");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Seed speaker users
        string[] speakerEmails = ["speaker1@example.com", "speaker2@example.com", "speaker3@example.com"];
        var speakers = new List<User>();
        for (int i = 0; i < speakerEmails.Length; i++)
        {
            var email = speakerEmails[i];
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = $"Speaker",
                    LastName = $"{i + 1}",
                    EmailConfirmed = true,
                    MemberSince = DateTime.UtcNow
                };
                await userManager.CreateAsync(user, "password");
                await userManager.AddToRoleAsync(user, "Speaker");
            }
            speakers.Add(user);
        }

        // Seed events if none exist
        if (!dbContext.Events.Any())
        {
            var event1 = new Event
            {
                Title = "Introduction to .NET 10",
                Slug = "introduction-to-net-10",
                ShortDescription = "A deep dive into the new features of .NET 10.",
                Description = "Join us for an exciting session covering the latest improvements in .NET 10, including performance enhancements, new APIs, and tooling updates.",
                EventDate = DateTime.UtcNow.AddDays(14),
                Location = "Community Center, Room 101",
                IsPublished = true
            };
            var event2 = new Event
            {
                Title = "Blazor WebAssembly Best Practices",
                Slug = "blazor-webassembly-best-practices",
                ShortDescription = "Learn how to build production-ready Blazor WASM apps.",
                Description = "This session covers architecture patterns, performance optimization, authentication, and deployment strategies for Blazor WebAssembly applications.",
                EventDate = DateTime.UtcNow.AddDays(30),
                Location = "Tech Hub, Main Auditorium",
                IsPublished = true
            };
            var event3 = new Event
            {
                Title = "Draft Event - Not Published",
                Slug = "draft-event",
                ShortDescription = "This is a draft event for testing.",
                Description = "Still working on the details for this one.",
                EventDate = null,
                Location = null,
                IsPublished = false
            };

            dbContext.Events.AddRange(event1, event2, event3);
            await dbContext.SaveChangesAsync();

            // Assign speakers
            dbContext.EventSpeakers.AddRange(
                new EventSpeaker { EventId = event1.Id, UserId = speakers[0].Id },
                new EventSpeaker { EventId = event1.Id, UserId = speakers[1].Id },
                new EventSpeaker { EventId = event2.Id, UserId = speakers[1].Id },
                new EventSpeaker { EventId = event2.Id, UserId = speakers[2].Id }
            );
            await dbContext.SaveChangesAsync();
        }

        // Seed topic suggestions if none exist
        if (!dbContext.TopicSuggestions.Any())
        {
            var admin = await userManager.FindByEmailAsync(adminEmail);
            var speaker1 = speakers[0];

            var topic1 = new TopicSuggestion
            {
                Title = "Getting Started with Aspire",
                Description = "I'd love to see a talk on .NET Aspire and how to orchestrate cloud-native applications.",
                SuggestedByUserId = admin?.Id ?? 1,
                VolunteerUserId = speaker1.Id
            };
            var topic2 = new TopicSuggestion
            {
                Title = "Entity Framework Performance Tuning",
                Description = "Tips and tricks for optimizing EF Core queries in high-traffic applications.",
                SuggestedByUserId = speaker1.Id
            };
            var topic3 = new TopicSuggestion
            {
                Title = "Identity and Authorization Patterns",
                Description = "How to implement custom authorization handlers and policies in ASP.NET Core.",
                SuggestedByUserId = admin?.Id ?? 1
            };

            dbContext.TopicSuggestions.AddRange(topic1, topic2, topic3);
            await dbContext.SaveChangesAsync();

            // Add some votes
            if (admin is not null)
            {
                dbContext.TopicVotes.Add(new TopicVote { TopicSuggestionId = topic2.Id, UserId = admin.Id });
            }
            dbContext.TopicVotes.Add(new TopicVote { TopicSuggestionId = topic2.Id, UserId = speaker1.Id });
            dbContext.TopicVotes.Add(new TopicVote { TopicSuggestionId = topic3.Id, UserId = speaker1.Id });
            await dbContext.SaveChangesAsync();
        }
    }
}
