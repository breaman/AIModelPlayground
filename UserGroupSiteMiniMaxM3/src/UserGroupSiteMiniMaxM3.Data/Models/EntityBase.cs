using UserGroupSiteMiniMaxM3.Data.Interfaces;

namespace UserGroupSiteMiniMaxM3.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}