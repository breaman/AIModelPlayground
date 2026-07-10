using UserGroupSiteGpt56Sol.Data.Interfaces;

namespace UserGroupSiteGpt56Sol.Data.Models;

public abstract class EntityBase : IEntityBase
{
    public int Id { get; set; }
}