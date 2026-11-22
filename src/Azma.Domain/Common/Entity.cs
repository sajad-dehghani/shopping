namespace Azma.Domain.Common;

public abstract class Entity
{
    public int Id { get; set; }
}

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
