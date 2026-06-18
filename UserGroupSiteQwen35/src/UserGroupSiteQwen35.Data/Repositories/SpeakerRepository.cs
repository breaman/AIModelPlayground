using Microsoft.EntityFrameworkCore;

using UserGroupSiteQwen35.Data.Interfaces;
using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Data.Repositories;

public class SpeakerRepository : ISpeakerRepository
{
    private readonly ApplicationDbContext _context;

    public SpeakerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Speaker>> GetAllAsync()
    {
        return await _context.Speakers
            .Include(s => s.User)
            .ToListAsync();
    }

    public async Task<Speaker?> GetByIdAsync(int id)
    {
        return await _context.Speakers
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Speaker?> GetByUserIdAsync(int userId)
    {
        return await _context.Speakers
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Speaker> CreateAsync(Speaker speaker)
    {
        _context.Speakers.Add(speaker);
        await _context.SaveChangesAsync();
        return speaker;
    }

    public async Task<Speaker> UpdateAsync(Speaker speaker)
    {
        _context.Entry(speaker).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return speaker;
    }

    public async Task<IEnumerable<Speaker>> GetApprovedSpeakersAsync()
    {
        return await _context.Speakers
            .Where(s => s.IsApproved)
            .Include(s => s.User)
            .ToListAsync();
    }
}