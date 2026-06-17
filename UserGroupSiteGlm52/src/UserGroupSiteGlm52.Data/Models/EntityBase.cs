using UserGroupSiteGlm52.Data.Interfaces;

namespace UserGroupSiteGlm52.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}