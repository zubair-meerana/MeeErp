namespace Mee.Erp.Shared.Kernel.Domain.Entities;

/// <summary>
/// This is the base class for all entities in the system.
/// It provides common properties for auditing and soft deletion.
/// Every entity that needs to be persisted to the database should inherit from this class.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// The unique identifier for the entity.
    /// Using Guid prevents key collisions in a distributed environment.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The UTC date and time when the entity was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The identifier of the user who created the entity.
    /// Nullable for system-generated entities.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// The UTC date and time when the entity was last updated.
    /// Nullable for newly created entities.
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// The identifier of the user who last updated the entity.
    /// Nullable.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Flag to indicate if the entity is soft-deleted.
    /// We never hard-delete records from the database.
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}