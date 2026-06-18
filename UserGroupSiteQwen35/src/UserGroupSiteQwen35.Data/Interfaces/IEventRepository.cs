using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Data.Interfaces;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<IEnumerable<Event>> GetPublishedAsync();
    Task<Event?> GetByIdAsync(int id);
    Task<Event?> GetBySlugAsync(string slug);
    Task<Event> CreateAsync(Event evt);
    Task<Event> UpdateAsync(Event evt);
    Task DeleteAsync(int id);
    Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null);
}