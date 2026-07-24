using UserGroupSiteOpus5.Data.Interfaces;

namespace UserGroupSiteOpus5.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}