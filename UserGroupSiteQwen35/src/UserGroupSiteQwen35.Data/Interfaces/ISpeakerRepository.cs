using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Data.Interfaces;

public interface ISpeakerRepository
{
    Task<IEnumerable<Speaker>> GetAllAsync();
    Task<Speaker?> GetByIdAsync(int id);
    Task<Speaker?> GetByUserIdAsync(int userId);
    Task<Speaker> CreateAsync(Speaker speaker);
    Task<Speaker> UpdateAsync(Speaker speaker);
    Task<IEnumerable<Speaker>> GetApprovedSpeakersAsync();
}
