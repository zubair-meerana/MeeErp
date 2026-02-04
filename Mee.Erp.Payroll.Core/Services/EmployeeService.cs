using Mee.Erp.Payroll.Core.Entities;
using Mee.Erp.Payroll.Core.Enums;
using Mee.Erp.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Mee.Erp.Payroll.Core.Services;

public class EmployeeService : IEmployeeService
{
    private readonly PayrollDbContext _context;
    private readonly ILeaveService _leaveService;

    public EmployeeService(PayrollDbContext context, ILeaveService leaveService)
    {
        _context = context;
        _leaveService = leaveService;
    }

    public async Task<IEnumerable<Employee>> GetEmployeesAsync(Guid companyId)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Where(e => e.CompanyId == companyId)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();
    }

    public async Task<Employee?> GetEmployeeByIdAsync(Guid id)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Include(e => e.Subordinates)
            .Include(e => e.SalaryComponents)
            .Include(e => e.Deductions)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Employee> CreateEmployeeAsync(Employee employee)
    {
        // Generate unique employee number if not provided
        if (string.IsNullOrEmpty(employee.EmployeeNumber))
        {
            employee.EmployeeNumber = await GenerateEmployeeNumberAsync(employee.CompanyId);
        }

        employee.CreatedDate = DateTime.UtcNow;
        employee.UpdatedDate = DateTime.UtcNow;
        employee.IsActive = true;

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Create default salary components
        await CreateDefaultSalaryComponentsAsync(employee);
        await CreateDefaultDeductionsAsync(employee);

        return employee;
    }

    public async Task<Employee> UpdateEmployeeAsync(Employee employee)
    {
        var existingEmployee = await _context.Employees.FindAsync(employee.Id);
        if (existingEmployee == null)
        {
            throw new InvalidOperationException($"Employee with ID {employee.Id} not found");
        }

        existingEmployee.FirstName = employee.FirstName;
        existingEmployee.LastName = employee.LastName;
        existingEmployee.MiddleName = employee.MiddleName;
        existingEmployee.Email = employee.Email;
        existingEmployee.Phone = employee.Phone;
        existingEmployee.DateOfBirth = employee.DateOfBirth;
        existingEmployee.Gender = employee.Gender;
        existingEmployee.Nationality = employee.Nationality;
        existingEmployee.NationalId = employee.NationalId;
        existingEmployee.PassportNumber = employee.PassportNumber;
        existingEmployee.DepartmentId = employee.DepartmentId;
        existingEmployee.PositionId = employee.PositionId;
        existingEmployee.ManagerId = employee.ManagerId;
        existingEmployee.Status = employee.Status;
        existingEmployee.EmploymentType = employee.EmploymentType;
        existingEmployee.BasicSalary = employee.BasicSalary;
        existingEmployee.PayFrequency = employee.PayFrequency;
        existingEmployee.BankAccountNumber = employee.BankAccountNumber;
        existingEmployee.BankName = employee.BankName;
        existingEmployee.BankBranch = employee.BankBranch;
        existingEmployee.SwiftCode = employee.SwiftCode;
        existingEmployee.WorkSchedule = employee.WorkSchedule;
        existingEmployee.StandardHoursPerWeek = employee.StandardHoursPerWeek;
        existingEmployee.StandardHoursPerDay = employee.StandardHoursPerDay;
        existingEmployee.TaxNumber = employee.TaxNumber;
        existingEmployee.TaxCalculationMethod = employee.TaxCalculationMethod;
        existingEmployee.IsTaxExempt = employee.IsTaxExempt;
        existingEmployee.HasHealthInsurance = employee.HasHealthInsurance;
        existingEmployee.HasPension = employee.HasPension;
        existingEmployee.PensionContributionRate = employee.PensionContributionRate;
        existingEmployee.Address = employee.Address;
        existingEmployee.City = employee.City;
        existingEmployee.State = employee.State;
        existingEmployee.Country = employee.Country;
        existingEmployee.PostalCode = employee.PostalCode;
        existingEmployee.EmergencyContactName = employee.EmergencyContactName;
        existingEmployee.EmergencyContactPhone = employee.EmergencyContactPhone;
        existingEmployee.EmergencyContactRelation = employee.EmergencyContactRelation;
        existingEmployee.Notes = employee.Notes;
        existingEmployee.PhotoUrl = employee.PhotoUrl;
        existingEmployee.IsActive = employee.IsActive;
        existingEmployee.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingEmployee;
    }

    public async Task DeleteEmployeeAsync(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee != null)
        {
            employee.IsDeleted = true;
            employee.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync(Guid companyId)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.CompanyId == companyId && e.Status == EmployeeStatus.Active && e.IsActive)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(Guid departmentId)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Where(e => e.DepartmentId == departmentId && e.IsActive)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();
    }

    public async Task<Employee> TerminateEmployeeAsync(Guid id, DateTime terminationDate, string reason)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {id} not found");
        }

        employee.Status = EmployeeStatus.Terminated;
        employee.TerminationDate = terminationDate;
        employee.TerminationReason = reason;
        employee.IsActive = false;
        employee.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return employee;
    }

    private async Task<string> GenerateEmployeeNumberAsync(Guid companyId)
    {
        var employeeCount = await _context.Employees
            .CountAsync(e => e.CompanyId == companyId);

        return $"EMP{DateTime.Now:yy}{(employeeCount + 1):D4}";
    }

    private async Task CreateDefaultSalaryComponentsAsync(Employee employee)
    {
        // Basic Salary Component
        var basicSalaryComponent = new EmployeeSalaryComponent
        {
            EmployeeId = employee.Id,
            ComponentType = EarningType.BasicSalary,
            Description = "Basic Salary",
            Amount = employee.BasicSalary,
            Percentage = 0,
            IsPercentageOfBasic = false,
            IsTaxable = true,
            IsSubjectToPension = true,
            IsRecurring = true,
            EffectiveDate = employee.HireDate,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.EmployeeSalaryComponents.Add(basicSalaryComponent);
        await _context.SaveChangesAsync();
    }

    private async Task CreateDefaultDeductionsAsync(Employee employee)
    {
        var deductions = new List<EmployeeDeduction>();

        // Income Tax
        if (!employee.IsTaxExempt)
        {
            deductions.Add(new EmployeeDeduction
            {
                EmployeeId = employee.Id,
                DeductionType = DeductionType.IncomeTax,
                Description = "Income Tax",
                Amount = 0,
                Percentage = 0,
                IsPercentageOfGross = false,
                IsPreTax = false,
                IsVoluntary = false,
                IsRecurring = true,
                EffectiveDate = employee.HireDate,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        // Pension
        if (employee.HasPension && employee.PensionContributionRate > 0)
        {
            deductions.Add(new EmployeeDeduction
            {
                EmployeeId = employee.Id,
                DeductionType = DeductionType.Pension,
                Description = "Pension Contribution",
                Amount = 0,
                Percentage = employee.PensionContributionRate,
                IsPercentageOfGross = true,
                IsPreTax = true,
                IsVoluntary = false,
                IsRecurring = true,
                EffectiveDate = employee.HireDate,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        // Health Insurance
        if (employee.HasHealthInsurance)
        {
            deductions.Add(new EmployeeDeduction
            {
                EmployeeId = employee.Id,
                DeductionType = DeductionType.HealthInsurance,
                Description = "Health Insurance",
                Amount = 0, // Would be set based on company policy
                Percentage = 0,
                IsPercentageOfGross = false,
                IsPreTax = true,
                IsVoluntary = false,
                IsRecurring = true,
                EffectiveDate = employee.HireDate,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        _context.EmployeeDeductions.AddRange(deductions);
        await _context.SaveChangesAsync();
    }
}

public class PayrollService : IPayrollService
{
    private readonly PayrollDbContext _context;
    private readonly ISalaryComponentService _salaryComponentService;
    private readonly IDeductionService _deductionService;
    private readonly ITaxCalculationService _taxService;
    private readonly ITimeSheetService _timeSheetService;

    public PayrollService(
        PayrollDbContext context,
        ISalaryComponentService salaryComponentService,
        IDeductionService deductionService,
        ITaxCalculationService taxService,
        ITimeSheetService timeSheetService)
    {
        _context = context;
        _salaryComponentService = salaryComponentService;
        _deductionService = deductionService;
        _taxService = taxService;
        _timeSheetService = timeSheetService;
    }

    public async Task<IEnumerable<PayrollRecord>> GetPayrollRecordsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.PayrollRecords
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Include(p => p.Earnings)
            .Include(p => p.Deductions)
            .Where(p => p.Employee.CompanyId == companyId);

        if (startDate.HasValue)
        {
            query = query.Where(p => p.PayPeriodStartDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.PayPeriodEndDate <= endDate.Value);
        }

        return await query
            .OrderByDescending(p => p.PayPeriodStartDate)
            .ThenBy(p => p.Employee.LastName)
            .ToListAsync();
    }

    public async Task<PayrollRecord?> GetPayrollRecordByIdAsync(Guid id)
    {
        return await _context.PayrollRecords
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Position)
            .Include(p => p.Earnings)
            .Include(p => p.Deductions)
            .Include(p => p.TimeSheets)
            .Include(p => p.ApprovedBy)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PayrollRecord> CalculatePayrollAsync(Guid employeeId, DateTime periodStartDate, DateTime periodEndDate)
    {
        var employee = await _context.Employees
            .Include(e => e.SalaryComponents)
            .Include(e => e.Deductions)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {employeeId} not found");
        }

        // Check if payroll already exists for this period
        var existingPayroll = await _context.PayrollRecords
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId &&
                                   p.PayPeriodStartDate == periodStartDate &&
                                   p.PayPeriodEndDate == periodEndDate);

        if (existingPayroll != null)
        {
            return existingPayroll;
        }

        var payrollRecord = new PayrollRecord
        {
            PayrollNumber = await GeneratePayrollNumberAsync(),
            EmployeeId = employeeId,
            PayPeriodStartDate = periodStartDate,
            PayPeriodEndDate = periodEndDate,
            PaymentDate = periodEndDate.AddDays(7), // Payment 7 days after period end
            PayFrequency = employee.PayFrequency,
            Status = PayrollStatus.Calculated,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Calculate earnings
        var earnings = await CalculateEarningsAsync(employee, periodStartDate, periodEndDate);
        payrollRecord.Earnings = earnings;

        payrollRecord.BasicSalary = earnings.Where(e => e.EarningType == EarningType.BasicSalary).Sum(e => e.Amount);
        payrollRecord.OvertimeEarnings = earnings.Where(e => e.EarningType == EarningType.Overtime).Sum(e => e.Amount);
        payrollRecord.BonusEarnings = earnings.Where(e => e.EarningType == EarningType.Bonus).Sum(e => e.Amount);
        payrollRecord.AllowanceEarnings = earnings.Where(e => e.EarningType == EarningType.Allowance).Sum(e => e.Amount);
        payrollRecord.OtherEarnings = earnings.Where(e => e.EarningType == EarningType.Other).Sum(e => e.Amount);
        payrollRecord.GrossEarnings = earnings.Sum(e => e.Amount);

        // Calculate YTD values
        var ytdPayroll = await CalculateYTDValuesAsync(employeeId, periodEndDate);
        payrollRecord.YtdGrossEarnings = ytdPayroll.GrossEarnings + payrollRecord.GrossEarnings;
        payrollRecord.YtdNetPay = ytdPayroll.NetPay;

        // Calculate taxes and deductions
        var deductions = await CalculateDeductionsAsync(employee, payrollRecord.GrossEarnings, periodEndDate);
        payrollRecord.Deductions = deductions;

        payrollRecord.IncomeTaxDeduction = deductions.Where(d => d.DeductionType == DeductionType.IncomeTax).Sum(d => d.Amount);
        payrollRecord.SocialSecurityDeduction = deductions.Where(d => d.DeductionType == DeductionType.SocialSecurity).Sum(d => d.Amount);
        payrollRecord.HealthInsuranceDeduction = deductions.Where(d => d.DeductionType == DeductionType.HealthInsurance).Sum(d => d.Amount);
        payrollRecord.PensionDeduction = deductions.Where(d => d.DeductionType == DeductionType.Pension).Sum(d => d.Amount);
        payrollRecord.LoanDeductions = deductions.Where(d => d.DeductionType == DeductionType.Loan).Sum(d => d.Amount);
        payrollRecord.OtherDeductions = deductions.Where(d => d.DeductionType == DeductionType.Other).Sum(d => d.Amount);
        payrollRecord.TotalDeductions = deductions.Sum(d => d.Amount);

        payrollRecord.YtdTaxDeductions = ytdPayroll.TaxDeductions + payrollRecord.IncomeTaxDeduction;

        payrollRecord.NetPay = payrollRecord.GrossEarnings - payrollRecord.TotalDeductions;
        payrollRecord.YtdNetPay += payrollRecord.NetPay;

        _context.PayrollRecords.Add(payrollRecord);
        await _context.SaveChangesAsync();

        return payrollRecord;
    }

    public async Task<PayrollRecord> ApprovePayrollAsync(Guid id, Guid approvedById)
    {
        var payrollRecord = await _context.PayrollRecords.FindAsync(id);
        if (payrollRecord == null)
        {
            throw new InvalidOperationException($"Payroll record with ID {id} not found");
        }

        payrollRecord.Status = PayrollStatus.Approved;
        payrollRecord.ApprovedById = approvedById;
        payrollRecord.ApprovalDate = DateTime.UtcNow;
        payrollRecord.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return payrollRecord;
    }

    public async Task<PayrollRecord> ProcessPayrollAsync(Guid id, string paymentReference)
    {
        var payrollRecord = await _context.PayrollRecords.FindAsync(id);
        if (payrollRecord == null)
        {
            throw new InvalidOperationException($"Payroll record with ID {id} not found");
        }

        if (payrollRecord.Status != PayrollStatus.Approved)
        {
            throw new InvalidOperationException("Payroll record must be approved before processing");
        }

        payrollRecord.Status = PayrollStatus.Processed;
        payrollRecord.PaymentReference = paymentReference;
        payrollRecord.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return payrollRecord;
    }

    public async Task<byte[]> GeneratePayrollReportAsync(Guid companyId, DateTime startDate, DateTime endDate)
    {
        // Implementation would use a reporting library like Crystal Reports or similar
        // For now, return empty byte array
        return Array.Empty<byte>();
    }

    public async Task<byte[]> GeneratePaySlipAsync(Guid payrollRecordId)
    {
        // Implementation would generate a PDF payslip
        // For now, return empty byte array
        return Array.Empty<byte>();
    }

    public async Task<IEnumerable<PayrollRecord>> GetEmployeePayrollHistoryAsync(Guid employeeId)
    {
        return await _context.PayrollRecords
            .Include(p => p.Earnings)
            .Include(p => p.Deductions)
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.PayPeriodStartDate)
            .ToListAsync();
    }

    private async Task<string> GeneratePayrollNumberAsync()
    {
        var payrollCount = await _context.PayrollRecords.CountAsync();
        return $"PAY{DateTime.Now:yyyyMMddHHmmss}{(payrollCount + 1):D3}";
    }

    private async Task<List<PayrollEarning>> CalculateEarningsAsync(Employee employee, DateTime startDate, DateTime endDate)
    {
        var earnings = new List<PayrollEarning>();

        // Calculate basic salary (prorated if necessary)
        var basicSalaryComponent = employee.SalaryComponents
            .FirstOrDefault(sc => sc.ComponentType == EarningType.BasicSalary);

        if (basicSalaryComponent != null)
        {
            var basicSalary = basicSalaryComponent.IsPercentageOfBasic
                ? employee.BasicSalary * (basicSalaryComponent.Percentage / 100)
                : basicSalaryComponent.Amount;

            // Prorate based on period
            var workingDays = await GetWorkingDaysAsync(startDate, endDate);
            var periodDays = (endDate - startDate).Days + 1;
            var proratedSalary = basicSalary * (workingDays / periodDays);

            earnings.Add(new PayrollEarning
            {
                PayrollRecordId = Guid.Empty, // Will be set after saving
                EarningType = EarningType.BasicSalary,
                Description = "Basic Salary",
                Amount = proratedSalary,
                Quantity = workingDays,
                Rate = basicSalary / workingDays,
                IsTaxable = true,
                IsSubjectToPension = true,
                Date = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        // Calculate overtime
        var overtimeEarnings = await CalculateOvertimeEarningsAsync(employee.Id, startDate, endDate);
        earnings.AddRange(overtimeEarnings);

        // Calculate other earnings (allowances, bonuses)
        foreach (var component in employee.SalaryComponents
            .Where(sc => sc.ComponentType != EarningType.BasicSalary && sc.IsActive))
        {
            if (component.EffectiveDate <= endDate && (!component.EndDate.HasValue || component.EndDate.Value >= startDate))
            {
                var amount = component.IsPercentageOfBasic
                    ? employee.BasicSalary * (component.Percentage / 100)
                    : component.Amount;

                earnings.Add(new PayrollEarning
                {
                    PayrollRecordId = Guid.Empty, // Will be set after saving
                    EarningType = component.ComponentType,
                    Description = component.Description,
                    Amount = amount,
                    IsTaxable = component.IsTaxable,
                    IsSubjectToPension = component.IsSubjectToPension,
                    Date = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                });
            }
        }

        return earnings;
    }

    private async Task<List<PayrollDeduction>> CalculateDeductionsAsync(Employee employee, decimal grossEarnings, DateTime taxDate)
    {
        var deductions = new List<PayrollDeduction>();

        // Calculate tax deductions
        var taxCalculation = await _taxService.CalculateAllTaxesAsync(employee.Id, grossEarnings, taxDate);

        // Income Tax
        if (taxCalculation.IncomeTax > 0)
        {
            deductions.Add(new PayrollDeduction
            {
                PayrollRecordId = Guid.Empty, // Will be set after saving
                DeductionType = DeductionType.IncomeTax,
                Description = "Income Tax",
                Amount = taxCalculation.IncomeTax,
                IsPreTax = false,
                IsVoluntary = false,
                Date = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        // Social Security
        if (taxCalculation.SocialSecurity > 0)
        {
            deductions.Add(new PayrollDeduction
            {
                PayrollRecordId = Guid.Empty, // Will be set after saving
                DeductionType = DeductionType.SocialSecurity,
                Description = "Social Security",
                Amount = taxCalculation.SocialSecurity,
                IsPreTax = true,
                IsVoluntary = false,
                Date = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        // Health Insurance
        if (taxCalculation.HealthInsurance > 0)
        {
            deductions.Add(new PayrollDeduction
            {
                PayrollRecordId = Guid.Empty, // Will be set after saving
                DeductionType = DeductionType.HealthInsurance,
                Description = "Health Insurance",
                Amount = taxCalculation.HealthInsurance,
                IsPreTax = true,
                IsVoluntary = false,
                Date = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        // Pension
        if (taxCalculation.Pension > 0)
        {
            deductions.Add(new PayrollDeduction
            {
                PayrollRecordId = Guid.Empty, // Will be set after saving
                DeductionType = DeductionType.Pension,
                Description = "Pension",
                Amount = taxCalculation.Pension,
                IsPreTax = true,
                IsVoluntary = false,
                Date = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        // Other deductions
        foreach (var deduction in employee.Deductions
            .Where(d => d.IsActive && d.EffectiveDate <= taxDate && (!d.EndDate.HasValue || d.EndDate.Value >= taxDate)))
        {
            var amount = deduction.IsPercentageOfGross
                ? grossEarnings * (deduction.Percentage / 100)
                : deduction.Amount;

            if (amount > 0)
            {
                deductions.Add(new PayrollDeduction
                {
                    PayrollRecordId = Guid.Empty, // Will be set after saving
                    DeductionType = deduction.DeductionType,
                    Description = deduction.Description,
                    Amount = amount,
                    Percentage = deduction.Percentage,
                    IsPreTax = deduction.IsPreTax,
                    IsVoluntary = deduction.IsVoluntary,
                    Date = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                });
            }
        }

        return deductions;
    }

    private async Task<decimal> GetWorkingDaysAsync(DateTime startDate, DateTime endDate)
    {
        var workingDays = 0;
        var current = startDate;

        while (current <= endDate)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
            current = current.AddDays(1);
        }

        return workingDays;
    }

    private async Task<List<PayrollEarning>> CalculateOvertimeEarningsAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        var overtimeEarnings = new List<PayrollEarning>();

        // Get timesheets for the period
        var timesheets = await _context.TimeSheets
            .Where(t => t.EmployeeId == employeeId &&
                       t.TimesheetDate >= startDate &&
                       t.TimesheetDate <= endDate &&
                       t.Status == TimeSheetStatus.Approved)
            .ToListAsync();

        foreach (var timesheet in timesheets)
        {
            if (timesheet.OvertimeHours > 0)
            {
                var employee = await _context.Employees.FindAsync(employeeId);
                var overtimeRate = timesheet.OvertimeRate * employee.BasicSalary / employee.StandardHoursPerDay;

                overtimeEarnings.Add(new PayrollEarning
                {
                    PayrollRecordId = Guid.Empty, // Will be set after saving
                    EarningType = EarningType.Overtime,
                    Description = "Overtime Pay",
                    Amount = timesheet.OvertimeHours * overtimeRate,
                    Quantity = timesheet.OvertimeHours,
                    Rate = overtimeRate,
                    IsTaxable = true,
                    IsSubjectToPension = true,
                    Date = timesheet.TimesheetDate,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                });
            }
        }

        return overtimeEarnings;
    }

    private async Task<(decimal GrossEarnings, decimal NetPay, decimal TaxDeductions)> CalculateYTDValuesAsync(Guid employeeId, DateTime endDate)
    {
        var ytdPayroll = await _context.PayrollRecords
            .Where(p => p.EmployeeId == employeeId && p.PayPeriodEndDate < endDate)
            .ToListAsync();

        return (
            ytdPayroll.Sum(p => p.GrossEarnings),
            ytdPayroll.Sum(p => p.NetPay),
            ytdPayroll.Sum(p => p.IncomeTaxDeduction)
        );
    }
}

public class TaxCalculationService : ITaxCalculationService
{
    private readonly PayrollDbContext _context;

    public TaxCalculationService(PayrollDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> CalculateIncomeTaxAsync(Guid employeeId, decimal taxableIncome, DateTime taxDate)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || employee.IsTaxExempt)
        {
            return 0;
        }

        // Simplified progressive tax calculation (UAE example - no income tax)
        // In real implementation, this would use country-specific tax rules
        return employee.TaxCalculationMethod switch
        {
            TaxCalculationMethod.Progressive => await CalculateProgressiveTaxAsync(taxableIncome, taxDate),
            TaxCalculationMethod.FlatRate => CalculateFlatRateTaxAsync(taxableIncome),
            TaxCalculationMethod.Slab => await CalculateSlabTaxAsync(taxableIncome, taxDate),
            _ => 0
        };
    }

    public async Task<decimal> CalculateSocialSecurityAsync(Guid employeeId, decimal grossEarnings)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            return 0;
        }

        // Simplified social security calculation
        // In real implementation, this would use country-specific rates
        return grossEarnings * 0.05m; // 5% social security
    }

    public async Task<decimal> CalculateHealthInsuranceAsync(Guid employeeId, decimal grossEarnings)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || !employee.HasHealthInsurance)
        {
            return 0;
        }

        // Simplified health insurance calculation
        return grossEarnings * 0.025m; // 2.5% health insurance
    }

    public async Task<decimal> CalculatePensionAsync(Guid employeeId, decimal grossEarnings)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || !employee.HasPension || employee.PensionContributionRate == 0)
        {
            return 0;
        }

        return grossEarnings * (employee.PensionContributionRate / 100);
    }

    public async Task<TaxCalculationResult> CalculateAllTaxesAsync(Guid employeeId, decimal grossEarnings, DateTime taxDate)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            return new TaxCalculationResult { GrossEarnings = grossEarnings };
        }

        var taxableIncome = grossEarnings;
        var preTaxDeductions = 0m;

        // Calculate pre-tax deductions
        if (employee.HasPension)
        {
            var pension = await CalculatePensionAsync(employeeId, grossEarnings);
            preTaxDeductions += pension;
        }

        if (employee.HasHealthInsurance)
        {
            var healthInsurance = await CalculateHealthInsuranceAsync(employeeId, grossEarnings);
            preTaxDeductions += healthInsurance;
        }

        taxableIncome = grossEarnings - preTaxDeductions;

        // Calculate taxes
        var incomeTax = await CalculateIncomeTaxAsync(employeeId, taxableIncome, taxDate);
        var socialSecurity = await CalculateSocialSecurityAsync(employeeId, grossEarnings);
        var healthInsurance = await CalculateHealthInsuranceAsync(employeeId, grossEarnings);
        var pension = await CalculatePensionAsync(employeeId, grossEarnings);

        var totalDeductions = incomeTax + socialSecurity + healthInsurance + pension;
        var netPay = grossEarnings - totalDeductions;

        return new TaxCalculationResult
        {
            GrossEarnings = grossEarnings,
            TaxableIncome = taxableIncome,
            IncomeTax = incomeTax,
            SocialSecurity = socialSecurity,
            HealthInsurance = healthInsurance,
            Pension = pension,
            TotalDeductions = totalDeductions,
            NetPay = netPay,
            Details = new List<TaxCalculationDetail>
            {
                new() { Name = "Gross Earnings", Amount = grossEarnings, Description = "Total earnings before deductions" },
                new() { Name = "Income Tax", Amount = incomeTax, Description = "Income tax deduction" },
                new() { Name = "Social Security", Amount = socialSecurity, Description = "Social security contribution" },
                new() { Name = "Health Insurance", Amount = healthInsurance, Description = "Health insurance premium" },
                new() { Name = "Pension", Amount = pension, Description = "Pension contribution" }
            }
        };
    }

    private async Task<decimal> CalculateProgressiveTaxAsync(decimal taxableIncome, DateTime taxDate)
    {
        // Simplified progressive tax calculation
        // Real implementation would use country-specific tax brackets
        if (taxableIncome <= 5000) return 0;
        if (taxableIncome <= 10000) return (taxableIncome - 5000) * 0.10m;
        if (taxableIncome <= 20000) return 500 + (taxableIncome - 10000) * 0.15m;
        if (taxableIncome <= 50000) return 2000 + (taxableIncome - 20000) * 0.20m;
        return 8000 + (taxableIncome - 50000) * 0.25m;
    }

    private decimal CalculateFlatRateTaxAsync(decimal taxableIncome)
    {
        return taxableIncome * 0.15m; // 15% flat rate
    }

    private async Task<decimal> CalculateSlabTaxAsync(decimal taxableIncome, DateTime taxDate)
    {
        // Similar to progressive but using defined slabs
        return await CalculateProgressiveTaxAsync(taxableIncome, taxDate);
    }
}