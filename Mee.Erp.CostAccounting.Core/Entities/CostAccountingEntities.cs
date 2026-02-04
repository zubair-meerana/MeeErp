using Mee.Erp.Finance.Core.Entities;
using Mee.Erp.Shared.Kernel;
using Mee.Erp.CostAccounting.Core.Enums;

namespace Mee.Erp.CostAccounting.Core.Entities;

public class Project : Entity<Guid>, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public BillingMethod BillingMethod { get; set; } = BillingMethod.FixedPrice;
    
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public Guid? ManagerId { get; set; }
    public User? Manager { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? EstimatedBudget { get; set; }
    public decimal? ContractValue { get; set; }
    public decimal ActualCost { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal ProgressPercentage { get; set; }
    
    // Overhead allocation settings
    public OverheadAllocationMethod OverheadAllocationMethod { get; set; } = OverheadAllocationMethod.DirectLaborCost;
    public decimal OverheadRate { get; set; }
    
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<ProjectCost> Costs { get; set; } = new List<ProjectCost>();
    public ICollection<ProjectTimeEntry> TimeEntries { get; set; } = new List<ProjectTimeEntry>();
    public ICollection<ProjectMaterialUsage> MaterialUsages { get; set; } = new List<ProjectMaterialUsage>();
    public ICollection<ProjectMilestone> Milestones { get; set; } = new List<ProjectMilestone>();
}

public class ProjectTask : Entity<Guid>, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Sequence { get; set; }
    
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    public Guid? ParentTaskId { get; set; }
    public ProjectTask? ParentTask { get; set; }
    public ICollection<ProjectTask> SubTasks { get; set; } = new List<ProjectTask>();
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal ProgressPercentage { get; set; }
    
    public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    
    public Guid? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }
    
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    
    // Navigation properties
    public ICollection<ProjectTimeEntry> TimeEntries { get; set; } = new List<ProjectTimeEntry>();
}

public class ProjectCost : Entity<Guid>, ISoftDeletable
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    public Guid? TaskId { get; set; }
    public ProjectTask? Task { get; set; }
    
    public CostType CostType { get; set; }
    public CostTransactionType TransactionType { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }
    
    public DateTime TransactionDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? SupplierInvoice { get; set; }
    
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class ProjectTimeEntry : Entity<Guid>, ISoftDeletable
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    public Guid? TaskId { get; set; }
    public ProjectTask? Task { get; set; }
    
    public Guid EmployeeId { get; set; }
    public User Employee { get; set; } = null!;
    
    public DateTime EntryDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalCost { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public bool IsBillable { get; set; } = true;
    public bool IsBilled { get; set; }
    public Guid? InvoiceLineId { get; set; }
    
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class ProjectMaterialUsage : Entity<Guid>, ISoftDeletable
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    public Guid? TaskId { get; set; }
    public ProjectTask? Task { get; set; }
    
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    
    public DateTime UsageDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? SupplierName { get; set; }
    
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class ProjectMilestone : Entity<Guid>, ISoftDeletable
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class ProjectBudget : Entity<Guid>, ISoftDeletable
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    public CostType CostType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BudgetedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercentage { get; set; }
    
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }
    
    public FiscalYear FiscalYear { get; set; }
    public int FiscalPeriod { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

// Additional enums
public enum TaskStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    OnHold = 4,
    Cancelled = 5
}

public enum TaskPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public enum MilestoneStatus
{
    Pending = 1,
    Completed = 2,
    Overdue = 3,
    Cancelled = 4
}