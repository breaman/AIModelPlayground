using UserGroupSiteGpt55.Data.Interfaces;

namespace UserGroupSiteGpt55.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}