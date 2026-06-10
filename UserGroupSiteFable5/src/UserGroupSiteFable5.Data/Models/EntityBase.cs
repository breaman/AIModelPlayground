using UserGroupSiteFable5.Data.Interfaces;

namespace UserGroupSiteFable5.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}