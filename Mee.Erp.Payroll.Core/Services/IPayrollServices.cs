using Mee.Erp.Payroll.Core.Entities;
using Mee.Erp.Payroll.Core.Enums;

namespace Mee.Erp.Payroll.Core.Services;

public interface IEmployeeService
{
    Task<IEnumerable<Employee>> GetEmployeesAsync(Guid companyId);
    Task<Employee?> GetEmployeeByIdAsync(Guid id);
    Task<Employee> CreateEmployeeAsync(Employee employee);
    Task<Employee> UpdateEmployeeAsync(Employee employee);
    Task DeleteEmployeeAsync(Guid id);
    Task<IEnumerable<Employee>> GetActiveEmployeesAsync(Guid companyId);
    Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(Guid departmentId);
    Task<Employee> TerminateEmployeeAsync(Guid id, DateTime terminationDate, string reason);
}

public interface IDepartmentService
{
    Task<IEnumerable<Department>> GetDepartmentsAsync(Guid companyId);
    Task<Department?> GetDepartmentByIdAsync(Guid id);
    Task<Department> CreateDepartmentAsync(Department department);
    Task<Department> UpdateDepartmentAsync(Department department);
    Task DeleteDepartmentAsync(Guid id);
    Task<IEnumerable<Department>> GetDepartmentTreeAsync(Guid companyId);
}

public interface IPositionService
{
    Task<IEnumerable<Position>> GetPositionsAsync(Guid companyId);
    Task<Position?> GetPositionByIdAsync(Guid id);
    Task<Position> CreatePositionAsync(Position position);
    Task<Position> UpdatePositionAsync(Position position);
    Task DeletePositionAsync(Guid id);
}

public interface IPayrollService
{
    Task<IEnumerable<PayrollRecord>> GetPayrollRecordsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null);
    Task<PayrollRecord?> GetPayrollRecordByIdAsync(Guid id);
    Task<PayrollRecord> CalculatePayrollAsync(Guid employeeId, DateTime periodStartDate, DateTime periodEndDate);
    Task<PayrollRecord> ApprovePayrollAsync(Guid id, Guid approvedById);
    Task<PayrollRecord> ProcessPayrollAsync(Guid id, string paymentReference);
    Task<byte[]> GeneratePayrollReportAsync(Guid companyId, DateTime startDate, DateTime endDate);
    Task<byte[]> GeneratePaySlipAsync(Guid payrollRecordId);
    Task<IEnumerable<PayrollRecord>> GetEmployeePayrollHistoryAsync(Guid employeeId);
}

public interface IPayrollPeriodService
{
    Task<IEnumerable<PayrollPeriod>> GetPayrollPeriodsAsync(Guid companyId);
    Task<PayrollPeriod> CreatePayrollPeriodAsync(PayrollPeriod period);
    Task<PayrollPeriod> ProcessPayrollPeriodAsync(Guid id);
    Task<PayrollPeriod> GetCurrentPayrollPeriodAsync(Guid companyId);
    Task<IEnumerable<PayrollPeriod>> GetUpcomingPayrollPeriodsAsync(Guid companyId);
}

public interface ITimeSheetService
{
    Task<IEnumerable<TimeSheet>> GetTimeSheetsAsync(Guid employeeId, DateTime? startDate = null, DateTime? endDate = null);
    Task<TimeSheet> CreateTimeSheetAsync(TimeSheet timeSheet);
    Task<TimeSheet> ApproveTimeSheetAsync(Guid id, Guid approvedById);
    Task<TimeSheet> RejectTimeSheetAsync(Guid id, Guid approvedById, string reason);
    Task<IEnumerable<TimeSheet>> GetPendingTimeSheetsAsync(Guid companyId);
    Task<decimal> CalculateOvertimeAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateTimeSheetReportAsync(Guid employeeId, DateTime startDate, DateTime endDate);
}

public interface ILeaveService
{
    Task<IEnumerable<LeaveRequest>> GetLeaveRequestsAsync(Guid employeeId);
    Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest leaveRequest);
    Task<LeaveRequest> ApproveLeaveRequestAsync(Guid id, Guid approverId);
    Task<LeaveRequest> RejectLeaveRequestAsync(Guid id, Guid approverId, string reason);
    Task<decimal> CalculateLeaveBalanceAsync(Guid employeeId, LeaveType leaveType);
    Task<LeaveRequest> CancelLeaveRequestAsync(Guid id);
    Task<byte[]> GenerateLeaveReportAsync(Guid companyId, DateTime startDate, DateTime endDate);
}

public interface ISalaryComponentService
{
    Task<IEnumerable<EmployeeSalaryComponent>> GetSalaryComponentsAsync(Guid employeeId);
    Task<EmployeeSalaryComponent> AddSalaryComponentAsync(EmployeeSalaryComponent component);
    Task<EmployeeSalaryComponent> UpdateSalaryComponentAsync(EmployeeSalaryComponent component);
    Task DeleteSalaryComponentAsync(Guid id);
    Task<decimal> CalculateTotalEarningsAsync(Guid employeeId, DateTime periodStartDate, DateTime periodEndDate);
}

public interface IDeductionService
{
    Task<IEnumerable<EmployeeDeduction>> GetEmployeeDeductionsAsync(Guid employeeId);
    Task<EmployeeDeduction> AddDeductionAsync(EmployeeDeduction deduction);
    Task<EmployeeDeduction> UpdateDeductionAsync(EmployeeDeduction deduction);
    Task DeleteDeductionAsync(Guid id);
    Task<decimal> CalculateTotalDeductionsAsync(Guid employeeId, DateTime periodStartDate, DateTime periodEndDate);
}

public interface ITaxCalculationService
{
    Task<decimal> CalculateIncomeTaxAsync(Guid employeeId, decimal taxableIncome, DateTime taxDate);
    Task<decimal> CalculateSocialSecurityAsync(Guid employeeId, decimal grossEarnings);
    Task<decimal> CalculateHealthInsuranceAsync(Guid employeeId, decimal grossEarnings);
    Task<decimal> CalculatePensionAsync(Guid employeeId, decimal grossEarnings);
    Task<TaxCalculationResult> CalculateAllTaxesAsync(Guid employeeId, decimal grossEarnings, DateTime taxDate);
}

public interface IPayrollReportingService
{
    Task<byte[]> GeneratePayrollSummaryReportAsync(Guid companyId, DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateDepartmentPayrollReportAsync(Guid departmentId, DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateEmployeePayrollReportAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateTaxReportAsync(Guid companyId, DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateLeaveBalanceReportAsync(Guid companyId);
    Task<byte[]> GenerateOvertimeReportAsync(Guid companyId, DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateYearEndTaxReportAsync(Guid companyId, int fiscalYear);
}

public interface IPayrollIntegrationService
{
    Task<IntegrationResult> PostPayrollToFinanceAsync(PayrollRecord payrollRecord);
    Task<IntegrationResult> PostTaxPaymentsToFinanceAsync(DateTime paymentDate);
    Task<IntegrationResult> SyncEmployeeAccountsAsync();
    Task<IntegrationResult> ProcessBankPaymentsAsync(IEnumerable<Guid> payrollRecordIds);
}

// DTOs and Response Models
public class TaxCalculationResult
{
    public decimal GrossEarnings { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal SocialSecurity { get; set; }
    public decimal HealthInsurance { get; set; }
    public decimal Pension { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public List<TaxCalculationDetail> Details { get; set; } = new();
}

public class TaxCalculationDetail
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public bool IsPreTax { get; set; }
    public bool IsVoluntary { get; set; }
}

public class PayrollCalculationRequest
{
    public Guid EmployeeId { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public bool IncludeOvertime { get; set; } = true;
    public bool IncludeBonus { get; set; } = true;
    public bool IncludeAllowances { get; set; } = true;
    public List<Guid> ExcludedDeductionIds { get; set; } = new();
}

public class PayrollSummary
{
    public int EmployeeCount { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal TotalOvertime { get; set; }
    public decimal TotalBonuses { get; set; }
    public Dictionary<string, decimal> DepartmentTotals { get; set; } = new();
    public Dictionary<string, decimal> DeductionTotals { get; set; } = new();
}

public class LeaveBalance
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Dictionary<LeaveType, LeaveBalanceDetail> Balances { get; set; } = new();
}

public class LeaveBalanceDetail
{
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal AvailableDays { get; set; }
    public decimal PendingDays { get; set; }
    public DateTime NextAccrualDate { get; set; }
}

public class OvertimeCalculation
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal DoubleTimeHours { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal DoubleTimeRate { get; set; }
    public decimal OvertimeEarnings { get; set; }
    public List<TimeSheet> TimeSheets { get; set; } = new();
}

public class PayrollIntegrationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? JournalEntryId { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<BankPayment> Payments { get; set; } = new();
}

public class BankPayment
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
}

public class IntegrationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? JournalEntryId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}