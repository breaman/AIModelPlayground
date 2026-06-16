using UserGroupSiteKimiK27Code.Data.Interfaces;

namespace UserGroupSiteKimiK27Code.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}