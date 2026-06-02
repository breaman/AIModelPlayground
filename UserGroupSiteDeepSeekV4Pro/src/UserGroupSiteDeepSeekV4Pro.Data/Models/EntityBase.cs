using UserGroupSiteDeepSeekV4Pro.Data.Interfaces;

namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}