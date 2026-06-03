using UserGroupSiteSonnet46.Data.Interfaces;

namespace UserGroupSiteSonnet46.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}