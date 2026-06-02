using UserGroupSiteOpus48.Data.Interfaces;

namespace UserGroupSiteOpus48.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}