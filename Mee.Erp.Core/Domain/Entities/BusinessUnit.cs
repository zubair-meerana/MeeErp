using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Core.Domain.Entities;

// Represents a department, branch, store, or cost center.
public class BusinessUnit : BaseEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public Guid CompanyId { get; set; }
    
    // Self-referencing key for creating hierarchies (e.g., Branch reports to Regional Office)
    public Guid? ParentBusinessUnitId { get; set; }
}