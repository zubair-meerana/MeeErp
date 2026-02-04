using Mee.Erp.Shared.Kernel;

namespace Mee.Erp.CostAccounting.Core.Enums;

public enum ProjectStatus
{
    [Display(Name = "Planning")]
    Planning = 1,
    
    [Display(Name = "Active")]
    Active = 2,
    
    [Display(Name = "On Hold")]
    OnHold = 3,
    
    [Display(Name = "Completed")]
    Completed = 4,
    
    [Display(Name = "Cancelled")]
    Cancelled = 5
}

public enum CostType
{
    [Display(Name = "Labor")]
    Labor = 1,
    
    [Display(Name = "Material")]
    Material = 2,
    
    [Display(Name = "Overhead")]
    Overhead = 3,
    
    [Display(Name = "Equipment")]
    Equipment = 4,
    
    [Display(Name = "Subcontractor")]
    Subcontractor = 5,
    
    [Display(Name = "Other")]
    Other = 6
}

public enum CostTransactionType
{
    [Display(Name = "Actual")]
    Actual = 1,
    
    [Display(Name = "Budgeted")]
    Budgeted = 2,
    
    [Display(Name = "Forecasted")]
    Forecasted = 3
}

public enum OverheadAllocationMethod
{
    [Display(Name = "Direct Labor Cost")]
    DirectLaborCost = 1,
    
    [Display(Name = "Direct Labor Hours")]
    DirectLaborHours = 2,
    
    [Display(Name = "Machine Hours")]
    MachineHours = 3,
    
    [Display(Name = "Material Cost")]
    MaterialCost = 4,
    
    [Display(Name = "Fixed Percentage")]
    FixedPercentage = 5
}

public enum BillingMethod
{
    [Display(Name = "Fixed Price")]
    FixedPrice = 1,
    
    [Display(Name = "Time and Materials")]
    TimeAndMaterials = 2,
    
    [Display(Name = "Cost Plus")]
    CostPlus = 3,
    
    [Display(Name = "Milestone")]
    Milestone = 4
}