using UserGroupSiteQwen35.Data.Interfaces;

namespace UserGroupSiteQwen35.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}