using UserGroupSiteQwen35.Data.Interfaces;
using UserGroupSiteQwen35.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteQwen35.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetAllAsync()
    {
        return await _context.Events
            .OrderByDescending(e => e.DateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetPublishedAsync()
    {
        return await _context.Events
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.DateTime)
            .ToListAsync();
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await _context.Events.FindAsync(id);
    }

    public async Task<Event?> GetBySlugAsync(string slug)
    {
        return await _context.Events.FirstOrDefaultAsync(e => e.Slug == slug);
    }

    public async Task<Event> CreateAsync(Event evt)
    {
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    public async Task<Event> UpdateAsync(Event evt)
    {
        _context.Entry(evt).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return evt;
    }

    public async Task DeleteAsync(int id)
    {
        var evt = await _context.Events.FindAsync(id);
        if (evt != null)
        {
            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null)
    {
        var query = _context.Events.Where(e => e.Slug == slug);
        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }
}
