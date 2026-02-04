using Mee.Erp.Payroll.Core.Entities;
using Mee.Erp.Payroll.Core.Enums;
using Mee.Erp.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Mee.Erp.Payroll.Core.Persistence;

public class PayrollDbContext : DbContext
{
    public PayrollDbContext(DbContextOptions<PayrollDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<PayrollRecord> PayrollRecords { get; set; }
    public DbSet<PayrollEarning> PayrollEarnings { get; set; }
    public DbSet<PayrollDeduction> PayrollDeductions { get; set; }
    public DbSet<EmployeeSalaryComponent> EmployeeSalaryComponents { get; set; }
    public DbSet<EmployeeDeduction> EmployeeDeductions { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<LeaveAttachment> LeaveAttachments { get; set; }
    public DbSet<TimeSheet> TimeSheets { get; set; }
    public DbSet<PayrollPeriod> PayrollPeriods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Employee
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Nationality).HasMaxLength(100);
            entity.Property(e => e.NationalId).HasMaxLength(100);
            entity.Property(e => e.PassportNumber).HasMaxLength(100);
            entity.Property(e => e.BasicSalary).HasPrecision(18, 2);
            entity.Property(e => e.StandardHoursPerWeek).HasPrecision(5, 2);
            entity.Property(e => e.StandardHoursPerDay).HasPrecision(5, 2);
            entity.Property(e => e.BankAccountNumber).HasMaxLength(100);
            entity.Property(e => e.BankName).HasMaxLength(200);
            entity.Property(e => e.BankBranch).HasMaxLength(200);
            entity.Property(e => e.SwiftCode).HasMaxLength(50);
            entity.Property(e => e.WorkSchedule).HasMaxLength(100);
            entity.Property(e => e.TaxNumber).HasMaxLength(100);
            entity.Property(e => e.PensionContributionRate).HasPrecision(5, 4);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(200);
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(50);
            entity.Property(e => e.EmergencyContactRelation).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
            
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Position)
                .WithMany(p => p.Employees)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Manager)
                .WithMany(m => m.Subordinates)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.HasIndex(e => new { e.CompanyId, e.EmployeeNumber });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Department
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Budget).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ParentDepartment)
                .WithMany(d => d.SubDepartments)
                .HasForeignKey(e => e.ParentDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.ExpenseAccount)
                .WithMany()
                .HasForeignKey(e => e.ExpenseAccountId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.CompanyId, e.Code });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Position
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.JobLevel).HasMaxLength(50);
            entity.Property(e => e.JobGrade).HasMaxLength(20);
            entity.Property(e => e.MinSalary).HasPrecision(18, 2);
            entity.Property(e => e.MaxSalary).HasPrecision(18, 2);
            entity.Property(e => e.AverageSalary).HasPrecision(18, 2);
            entity.Property(e => e.Requirements).HasMaxLength(2000);
            entity.Property(e => e.Responsibilities).HasMaxLength(2000);
            entity.Property(e => e.Qualifications).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.CompanyId, e.Code });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure PayrollRecord
        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PayrollNumber).IsRequired().HasMaxLength(30);
            entity.Property(e => e.BasicSalary).HasPrecision(18, 2);
            entity.Property(e => e.OvertimeEarnings).HasPrecision(18, 2);
            entity.Property(e => e.BonusEarnings).HasPrecision(18, 2);
            entity.Property(e => e.AllowanceEarnings).HasPrecision(18, 2);
            entity.Property(e => e.OtherEarnings).HasPrecision(18, 2);
            entity.Property(e => e.GrossEarnings).HasPrecision(18, 2);
            entity.Property(e => e.IncomeTaxDeduction).HasPrecision(18, 2);
            entity.Property(e => e.SocialSecurityDeduction).HasPrecision(18, 2);
            entity.Property(e => e.HealthInsuranceDeduction).HasPrecision(18, 2);
            entity.Property(e => e.PensionDeduction).HasPrecision(18, 2);
            entity.Property(e => e.LoanDeductions).HasPrecision(18, 2);
            entity.Property(e => e.OtherDeductions).HasPrecision(18, 2);
            entity.Property(e => e.TotalDeductions).HasPrecision(18, 2);
            entity.Property(e => e.NetPay).HasPrecision(18, 2);
            entity.Property(e => e.YtdGrossEarnings).HasPrecision(18, 2);
            entity.Property(e => e.YtdNetPay).HasPrecision(18, 2);
            entity.Property(e => e.YtdTaxDeductions).HasPrecision(18, 2);
            entity.Property(e => e.PaymentReference).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            
            entity.HasOne(e => e.Employee)
                .WithMany(e => e.PayrollRecords)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.ApprovedBy)
                .WithMany()
                .HasForeignKey(e => e.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.PayrollNumber).IsUnique();
            entity.HasIndex(e => new { e.EmployeeId, e.PayPeriodStartDate, e.PayPeriodEndDate });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure PayrollEarning
        modelBuilder.Entity<PayrollEarning>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Quantity).HasPrecision(10, 2);
            entity.Property(e => e.Rate).HasPrecision(18, 4);
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.PayrollRecord)
                .WithMany(p => p.Earnings)
                .HasForeignKey(e => e.PayrollRecordId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure PayrollDeduction
        modelBuilder.Entity<PayrollDeduction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Percentage).HasPrecision(5, 4);
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.RemainingBalance).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.PayrollRecord)
                .WithMany(p => p.Deductions)
                .HasForeignKey(e => e.PayrollRecordId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure EmployeeSalaryComponent
        modelBuilder.Entity<EmployeeSalaryComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Percentage).HasPrecision(5, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Employee)
                .WithMany(e => e.SalaryComponents)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.EmployeeId, e.ComponentType });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure EmployeeDeduction
        modelBuilder.Entity<EmployeeDeduction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Percentage).HasPrecision(5, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Employee)
                .WithMany(e => e.Deductions)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.EmployeeId, e.DeductionType });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure LeaveRequest
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Days).HasPrecision(5, 2);
            entity.Property(e => e.AvailableBalance).HasPrecision(5, 2);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApproverId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure LeaveAttachment
        modelBuilder.Entity<LeaveAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(100);
            entity.Property(e => e.FileSize);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasOne(e => e.LeaveRequest)
                .WithMany(l => l.Attachments)
                .HasForeignKey(e => e.LeaveRequestId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure TimeSheet
        modelBuilder.Entity<TimeSheet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RegularHours).HasPrecision(5, 2);
            entity.Property(e => e.OvertimeHours).HasPrecision(5, 2);
            entity.Property(e => e.DoubleTimeHours).HasPrecision(5, 2);
            entity.Property(e => e.TotalHours).HasPrecision(5, 2);
            entity.Property(e => e.OvertimeRate).HasPrecision(5, 4);
            entity.Property(e => e.DoubleTimeRate).HasPrecision(5, 4);
            entity.Property(e => e.ProjectCode).HasMaxLength(50);
            entity.Property(e => e.TaskDescription).HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Employee)
                .WithMany(e => e.TimeSheets)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.PayrollRecord)
                .WithMany(p => p.TimeSheets)
                .HasForeignKey(e => e.PayrollRecordId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.ApprovedBy)
                .WithMany()
                .HasForeignKey(e => e.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => new { e.EmployeeId, e.TimesheetDate });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure PayrollPeriod
        modelBuilder.Entity<PayrollPeriod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TotalPayroll).HasPrecision(18, 2);
            entity.Property(e => e.TotalGrossPay).HasPrecision(18, 2);
            entity.Property(e => e.TotalNetPay).HasPrecision(18, 2);
            entity.Property(e => e.TotalTaxes).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ProcessedBy)
                .WithMany()
                .HasForeignKey(e => e.ProcessedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => new { e.CompanyId, e.StartDate, e.EndDate });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<ISoftDeletable>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(e => e.CreatedDate).CurrentValue = DateTime.UtcNow;
                entry.Property(e => e.UpdatedDate).CurrentValue = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.UpdatedDate).CurrentValue = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}