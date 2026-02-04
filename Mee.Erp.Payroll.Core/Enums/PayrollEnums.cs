using Mee.Erp.Shared.Kernel;

namespace Mee.Erp.Payroll.Core.Enums;

public enum EmployeeStatus
{
    [Display(Name = "Active")]
    Active = 1,
    
    [Display(Name = "On Leave")]
    OnLeave = 2,
    
    [Display(Name = "Terminated")]
    Terminated = 3,
    
    [Display(Name = "Probation")]
    Probation = 4,
    
    [Display(Name = "Contract")]
    Contract = 5
}

public enum EmploymentType
{
    [Display(Name = "Full Time")]
    FullTime = 1,
    
    [Display(Name = "Part Time")]
    PartTime = 2,
    
    [Display(Name = "Contract")]
    Contract = 3,
    
    [Display(Name = "Intern")]
    Intern = 4,
    
    [Display(Name = "Consultant")]
    Consultant = 5
}

public enum PayFrequency
{
    [Display(Name = "Weekly")]
    Weekly = 1,
    
    [Display(Name = "Bi-Weekly")]
    BiWeekly = 2,
    
    [Display(Name = "Monthly")]
    Monthly = 3,
    
    [Display(Name = "Semi-Monthly")]
    SemiMonthly = 4,
    
    [Display(Name = "Quarterly")]
    Quarterly = 5
}

public enum PayrollStatus
{
    [Display(Name = "Draft")]
    Draft = 1,
    
    [Display(Name = "Calculated")]
    Calculated = 2,
    
    [Display(Name = "Approved")]
    Approved = 3,
    
    [Display(Name = "Processed")]
    Processed = 4,
    
    [Display(Name = "Paid")]
    Paid = 5,
    
    [Display(Name = "Cancelled")]
    Cancelled = 6
}

public enum EarningType
{
    [Display(Name = "Basic Salary")]
    BasicSalary = 1,
    
    [Display(Name = "Overtime")]
    Overtime = 2,
    
    [Display(Name = "Bonus")]
    Bonus = 3,
    
    [Display(Name = "Commission")]
    Commission = 4,
    
    [Display(Name = "Allowance")]
    Allowance = 5,
    
    [Display(Name = "Leave Encashment")]
    LeaveEncashment = 6,
    
    [Display(Name = "Other")]
    Other = 7
}

public enum DeductionType
{
    [Display(Name = "Income Tax")]
    IncomeTax = 1,
    
    [Display(Name = "Social Security")]
    SocialSecurity = 2,
    
    [Display(Name = "Health Insurance")]
    HealthInsurance = 3,
    
    [Display(Name = "Pension")]
    Pension = 4,
    
    [Display(Name = "Loan")]
    Loan = 5,
    
    [Display(Name = "Union Due")]
    UnionDue = 6,
    
    [Display(Name = "Other")]
    Other = 7
}

public enum LeaveType
{
    [Display(Name = "Annual Leave")]
    Annual = 1,
    
    [Display(Name = "Sick Leave")]
    Sick = 2,
    
    [Display(Name = "Maternity Leave")]
    Maternity = 3,
    
    [Display(Name = "Paternity Leave")]
    Paternity = 4,
    
    [Display(Name = "Unpaid Leave")]
    Unpaid = 5,
    
    [Display(Name = "Emergency Leave")]
    Emergency = 6,
    
    [Display(Name = "Study Leave")]
    Study = 7
}

public enum LeaveStatus
{
    [Display(Name = "Pending")]
    Pending = 1,
    
    [Display(Name = "Approved")]
    Approved = 2,
    
    [Display(Name = "Rejected")]
    Rejected = 3,
    
    [Display(Name = "Cancelled")]
    Cancelled = 4,
    
    [Display(Name = "Taken")]
    Taken = 5
}

public enum TimeSheetStatus
{
    [Display(Name = "Draft")]
    Draft = 1,
    
    [Display(Name = "Submitted")]
    Submitted = 2,
    
    [Display(Name = "Approved")]
    Approved = 3,
    
    [Display(Name = "Rejected")]
    Rejected = 4,
    
    [Display(Name = "Processed")]
    Processed = 5
}

public enum TaxCalculationMethod
{
    [Display(Name = "Progressive")]
    Progressive = 1,
    
    [Display(Name = "Flat Rate")]
    FlatRate = 2,
    
    [Display(Name = "Slab")]
    Slab = 3
}

public enum OvertimeCalculationMethod
{
    [Display(Name = "Standard")]
    Standard = 1,
    
    [Display(Name = "Weighted")]
    Weighted = 2,
    
    [Display(Name = "Custom")]
    Custom = 3
}