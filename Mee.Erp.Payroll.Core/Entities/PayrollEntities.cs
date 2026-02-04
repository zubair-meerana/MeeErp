using Mee.Erp.Finance.Core.Entities;
using Mee.Erp.Shared.Kernel;
using Mee.Erp.Payroll.Core.Enums;

namespace Mee.Erp.Payroll.Core.Entities;

public class Employee : Entity<Guid>, ISoftDeletable
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    
    public Guid? PositionId { get; set; }
    public Position? Position { get; set; }
    
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    
    public EmployeeStatus Status { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    
    // Compensation
    public decimal BasicSalary { get; set; }
    public PayFrequency PayFrequency { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? SwiftCode { get; set; }
    
    // Work schedule
    public string WorkSchedule { get; set; } = string.Empty;
    public decimal StandardHoursPerWeek { get; set; }
    public decimal StandardHoursPerDay { get; set; }
    
    // Tax and benefits
    public string TaxNumber { get; set; } = string.Empty;
    public TaxCalculationMethod TaxCalculationMethod { get; set; }
    public bool IsTaxExempt { get; set; }
    public bool HasHealthInsurance { get; set; }
    public bool HasPension { get; set; }
    public decimal PensionContributionRate { get; set; }
    
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    
    public string? Notes { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<TimeSheet> TimeSheets { get; set; } = new List<TimeSheet>();
    public ICollection<EmployeeSalaryComponent> SalaryComponents { get; set; } = new List<EmployeeSalaryComponent>();
    public ICollection<EmployeeDeduction> Deductions { get; set; } = new List<EmployeeDeduction>();
}

public class Department : Entity<Guid>, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    
    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    
    public decimal Budget { get; set; }
    public int EmployeeCount { get; set; }
    public bool IsCostCenter { get; set; }
    
    public Guid? ExpenseAccountId { get; set; }
    public Account? ExpenseAccount { get; set; }
    
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Position : Entity<Guid>, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string JobLevel { get; set; } = string.Empty;
    public string JobGrade { get; set; } = string.Empty;
    
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public decimal AverageSalary { get; set; }
    
    public string Requirements { get; set; } = string.Empty;
    public string Responsibilities { get; set; } = string.Empty;
    public string Qualifications { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class PayrollRecord : Entity<Guid>, ISoftDeletable
{
    public string PayrollNumber { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    
    public DateTime PayPeriodStartDate { get; set; }
    public DateTime PayPeriodEndDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public PayFrequency PayFrequency { get; set; }
    
    // Earnings
    public decimal BasicSalary { get; set; }
    public decimal OvertimeEarnings { get; set; }
    public decimal BonusEarnings { get; set; }
    public decimal AllowanceEarnings { get; set; }
    public decimal OtherEarnings { get; set; }
    public decimal GrossEarnings { get; set; }
    
    // Deductions
    public decimal IncomeTaxDeduction { get; set; }
    public decimal SocialSecurityDeduction { get; set; }
    public decimal HealthInsuranceDeduction { get; set; }
    public decimal PensionDeduction { get; set; }
    public decimal LoanDeductions { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }
    
    // Net
    public decimal NetPay { get; set; }
    public decimal YtdGrossEarnings { get; set; }
    public decimal YtdNetPay { get; set; }
    public decimal YtdTaxDeductions { get; set; }
    
    public PayrollStatus Status { get; set; }
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? PaymentReference { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<PayrollEarning> Earnings { get; set; } = new List<PayrollEarning>();
    public ICollection<PayrollDeduction> Deductions { get; set; } = new List<PayrollDeduction>();
    public ICollection<TimeSheet> TimeSheets { get; set; } = new List<TimeSheet>();
}

public class PayrollEarning : Entity<Guid>, ISoftDeletable
{
    public Guid PayrollRecordId { get; set; }
    public PayrollRecord PayrollRecord { get; set; } = null!;
    
    public EarningType EarningType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool IsSubjectToPension { get; set; } = true;
    
    public string? Reference { get; set; }
    public DateTime? Date { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class PayrollDeduction : Entity<Guid>, ISoftDeletable
{
    public Guid PayrollRecordId { get; set; }
    public PayrollRecord PayrollRecord { get; set; } = null!;
    
    public DeductionType DeductionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public bool IsPreTax { get; set; }
    public bool IsVoluntary { get; set; }
    
    public string? Reference { get; set; }
    public Guid? LoanId { get; set; }
    public decimal? RemainingBalance { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class EmployeeSalaryComponent : Entity<Guid>, ISoftDeletable
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    
    public EarningType ComponentType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public bool IsPercentageOfBasic { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool IsRecurring { get; set; } = true;
    
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class EmployeeDeduction : Entity<Guid>, ISoftDeletable
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    
    public DeductionType DeductionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public bool IsPercentageOfGross { get; set; }
    public bool IsPreTax { get; set; }
    public bool IsVoluntary { get; set; }
    public bool IsRecurring { get; set; } = true;
    
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class LeaveRequest : Entity<Guid>, ISoftDeletable
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    
    public LeaveType LeaveType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public decimal AvailableBalance { get; set; }
    
    public Guid? ApproverId { get; set; }
    public Employee? Approver { get; set; }
    
    public LeaveStatus Status { get; set; }
    public DateTime? AppliedDate { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? RejectionReason { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<LeaveAttachment> Attachments { get; set; } = new List<LeaveAttachment>();
}

public class LeaveAttachment : Entity<Guid>, ISoftDeletable
{
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;
    
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Description { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class TimeSheet : Entity<Guid>, ISoftDeletable
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    
    public Guid PayrollRecordId { get; set; }
    public PayrollRecord PayrollRecord { get; set; } = null!;
    
    public DateTime TimesheetDate { get; set; }
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal DoubleTimeHours { get; set; }
    public decimal TotalHours { get; set; }
    
    public OvertimeCalculationMethod OvertimeMethod { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal DoubleTimeRate { get; set; }
    
    public string ProjectCode { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    
    public TimeSheetStatus Status { get; set; }
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? RejectionReason { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
}

public class PayrollPeriod : Entity<Guid>, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public PayFrequency Frequency { get; set; }
    
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    
    public PayrollStatus Status { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalPayroll { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalTaxes { get; set; }
    
    public Guid? ProcessedById { get; set; }
    public User? ProcessedBy { get; set; }
    public DateTime? ProcessedDate { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
}