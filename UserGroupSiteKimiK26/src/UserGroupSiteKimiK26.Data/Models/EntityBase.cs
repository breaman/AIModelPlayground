using UserGroupSiteKimiK26.Data.Interfaces;

namespace UserGroupSiteKimiK26.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}