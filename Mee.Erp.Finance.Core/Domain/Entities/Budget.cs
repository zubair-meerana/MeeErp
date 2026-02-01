using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class Budget : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BusinessUnitId { get; set; }
    public int FiscalYear { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;
    public decimal TotalBudgetAmount { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? Notes { get; set; }
    
    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
    // Note: BusinessUnit would need to be imported from Mee.Erp.Core or defined here
}

public class BudgetLine : BaseEntity
{
    public Guid BudgetId { get; set; }
    public Guid AccountId { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; } = 0;
    public decimal Variance { get; set; }
    public decimal VariancePercentage { get; set; }
    
    public virtual Budget Budget { get; set; }
    public virtual Account Account { get; set; }
}

public enum BudgetStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Active = 3,
    Closed = 4,
    Rejected = 5
}