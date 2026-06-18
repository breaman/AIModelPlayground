using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteSonnet46.Data.Models;
using UserGroupSiteSonnet46.Shared.Services;

namespace UserGroupSiteSonnet46.Server.Services;

/// <summary>
/// Server-side event service that queries <see cref="ApplicationDbContext"/> directly.
/// The Client project uses <see cref="ClientEventService"/> which calls the HTTP API.
/// </summary>
public class ServerEventService(
    ApplicationDbContext db,
    UserManager<User> userManager) : IEventService
{
    /// <inheritdoc />
    public async Task<List<EventDto>> GetPublishedEventsAsync()
    {
        var events = await db.Events
            .AsNoTracking()
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.EventDateTime)
            .Include(e => e.Speakers)
            .ThenInclude(s => s.User)
            .ToListAsync();

        return events.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<EventDto>> GetAllEventsAsync()
    {
        var events = await db.Events
            .AsNoTracking()
            .OrderByDescending(e => e.EventDateTime)
            .Include(e => e.Speakers)
            .ThenInclude(s => s.User)
            .ToListAsync();

        return events.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        var ev = await db.Events
            .AsNoTracking()
            .Include(e => e.Speakers)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(e => e.Slug == slug);

        return ev is null ? null : MapToDto(ev);
    }

    /// <inheritdoc />
    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        var ev = await db.Events
            .AsNoTracking()
            .Include(e => e.Speakers)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        return ev is null ? null : MapToDto(ev);
    }

    /// <inheritdoc />
    public async Task<EventDto> SaveEventAsync(EventSaveDto dto)
    {
        Event ev;

        if (dto.Id is null)
        {
            ev = new Event { Title = dto.Title, Slug = dto.Slug };
            db.Events.Add(ev);
        }
        else
        {
            ev = await db.Events
                .Include(e => e.Speakers)
                .FirstOrDefaultAsync(e => e.Id == dto.Id)
                ?? throw new InvalidOperationException($"Event {dto.Id} not found.");
        }

        ev.Title = dto.Title;
        ev.Slug = dto.Slug;
        ev.ShortDescription = dto.ShortDescription;
        ev.Description = dto.Description;
        ev.EventDateTime = dto.EventDateTime;
        ev.Location = dto.Location;
        ev.IsPublished = dto.IsPublished;

        // Sync speakers: remove those no longer in the list, add new ones
        var existingIds = ev.Speakers.Select(s => s.UserId).ToHashSet();
        var desiredIds = dto.SpeakerIds.ToHashSet();

        foreach (var removed in existingIds.Except(desiredIds))
        {
            var speaker = ev.Speakers.First(s => s.UserId == removed);
            ev.Speakers.Remove(speaker);
        }

        foreach (var added in desiredIds.Except(existingIds))
        {
            ev.Speakers.Add(new EventSpeaker { UserId = added });
        }

        await db.SaveChangesAsync();

        return await GetEventByIdAsync(ev.Id)
            ?? throw new InvalidOperationException("Event not found after save.");
    }

    /// <inheritdoc />
    public async Task<List<UserDto>> GetSpeakersAsync()
    {
        var speakers = await userManager.GetUsersInRoleAsync("Speaker");
        return speakers
            .Select(u => new UserDto(u.Id, u.FirstName, u.LastName, u.Email, IsAdmin: false, IsSpeaker: true))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToList();
    }

    private static EventDto MapToDto(Event e) =>
        new(
            e.Id,
            e.Title,
            e.Slug,
            e.ShortDescription,
            e.Description,
            e.EventDateTime,
            e.Location,
            e.IsPublished,
            e.Speakers.Select(s => new UserDto(
                s.User.Id,
                s.User.FirstName,
                s.User.LastName,
                s.User.Email,
                IsAdmin: false,
                IsSpeaker: true)).ToList());
}