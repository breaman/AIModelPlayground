using UserGroupSiteGlm51.Data.Interfaces;

namespace UserGroupSiteGlm51.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}