using UserGroupSiteNemoTron3.Data.Interfaces;

namespace UserGroupSiteNemoTron3.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}