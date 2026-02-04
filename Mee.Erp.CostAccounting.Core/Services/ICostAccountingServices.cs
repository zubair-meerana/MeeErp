using Mee.Erp.CostAccounting.Core.Entities;
using Mee.Erp.CostAccounting.Core.Enums;

namespace Mee.Erp.CostAccounting.Core.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetProjectsAsync(Guid companyId);
    Task<Project?> GetProjectByIdAsync(Guid id);
    Task<Project> CreateProjectAsync(Project project);
    Task<Project> UpdateProjectAsync(Project project);
    Task DeleteProjectAsync(Guid id);
    Task<Project> UpdateProjectProgressAsync(Guid id);
    Task<IEnumerable<Project>> GetActiveProjectsAsync(Guid companyId);
    Task<decimal> CalculateProjectCostsAsync(Guid projectId);
    Task<decimal> CalculateProjectRevenueAsync(Guid projectId);
    Task<decimal> CalculateProjectProfitAsync(Guid projectId);
}

public interface IProjectTaskService
{
    Task<IEnumerable<ProjectTask>> GetProjectTasksAsync(Guid projectId);
    Task<ProjectTask?> GetTaskByIdAsync(Guid id);
    Task<ProjectTask> CreateTaskAsync(ProjectTask task);
    Task<ProjectTask> UpdateTaskAsync(ProjectTask task);
    Task DeleteTaskAsync(Guid id);
    Task<ProjectTask> UpdateTaskProgressAsync(Guid id);
    Task<IEnumerable<ProjectTask>> GetTasksByEmployeeAsync(Guid employeeId);
}

public interface IProjectCostService
{
    Task<IEnumerable<ProjectCost>> GetProjectCostsAsync(Guid projectId);
    Task<IEnumerable<ProjectCost>> GetCostsByTypeAsync(Guid projectId, CostType costType);
    Task<ProjectCost> AddCostAsync(ProjectCost cost);
    Task<ProjectCost> UpdateCostAsync(ProjectCost cost);
    Task DeleteCostAsync(Guid id);
    Task<decimal> CalculateTotalCostsAsync(Guid projectId);
    Task<decimal> CalculateCostsByTypeAsync(Guid projectId, CostType costType);
    Task<decimal> CalculateActualVsBudgetVarianceAsync(Guid projectId);
}

public interface IProjectTimeEntryService
{
    Task<IEnumerable<ProjectTimeEntry>> GetTimeEntriesAsync(Guid projectId);
    Task<IEnumerable<ProjectTimeEntry>> GetTimeEntriesByEmployeeAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<ProjectTimeEntry> AddTimeEntryAsync(ProjectTimeEntry timeEntry);
    Task<ProjectTimeEntry> UpdateTimeEntryAsync(ProjectTimeEntry timeEntry);
    Task DeleteTimeEntryAsync(Guid id);
    Task<decimal> CalculateTotalHoursAsync(Guid projectId);
    Task<decimal> CalculateLaborCostsAsync(Guid projectId);
    Task<decimal> CalculateEmployeeHoursAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<ProjectTimeEntry>> GetUnbilledTimeEntriesAsync(Guid projectId);
}

public interface IProjectBudgetService
{
    Task<IEnumerable<ProjectBudget>> GetProjectBudgetsAsync(Guid projectId);
    Task<ProjectBudget> CreateBudgetAsync(ProjectBudget budget);
    Task<ProjectBudget> UpdateBudgetAsync(ProjectBudget budget);
    Task DeleteBudgetAsync(Guid id);
    Task<decimal> CalculateTotalBudgetAsync(Guid projectId);
    Task<decimal> CalculateBudgetVarianceAsync(Guid projectId);
    Task UpdateBudgetActualsAsync(Guid projectId);
}

public interface ICostAnalysisService
{
    Task<ProjectProfitabilityAnalysis> AnalyzeProjectProfitabilityAsync(Guid projectId);
    Task<ProjectCostBreakdown> GetCostBreakdownAsync(Guid projectId);
    Task<IEnumerable<ProjectVarianceAnalysis>> AnalyzeBudgetVariancesAsync(Guid projectId);
    Task<EmployeeProductivityReport> GetEmployeeProductivityAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<ProjectPerformanceMetrics> GetProjectPerformanceAsync(Guid projectId);
    Task<IEnumerable<Project>> GetProfitableProjectsAsync(Guid companyId, decimal minProfitMargin);
}

public interface IProjectReportingService
{
    Task<byte[]> GenerateProjectCostReportAsync(Guid projectId);
    Task<byte[]> GenerateProjectProgressReportAsync(Guid projectId);
    Task<byte[]> GenerateEmployeeProductivityReportAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateProjectPortfolioReportAsync(Guid companyId);
    Task<byte[]> GenerateBudgetVarianceReportAsync(Guid projectId);
}

public interface IOverheadAllocationService
{
    Task<decimal> CalculateOverheadAsync(Guid projectId, Guid taskId);
    Task AllocateOverheadToProjectAsync(Guid projectId);
    Task<decimal> CalculateOverheadRateAsync(Guid companyId, OverheadAllocationMethod method);
    Task<IEnumerable<OverheadAllocation>> GetOverheadAllocationsAsync(Guid projectId);
}

public interface IProjectBillingService
{
    Task<IEnumerable<ProjectTimeEntry>> GetBillableTimeEntriesAsync(Guid projectId);
    Task<IEnumerable<ProjectCost>> GetBillableCostsAsync(Guid projectId);
    Task<decimal> CalculateBillableAmountAsync(Guid projectId);
    Task<byte[]> GenerateInvoiceDataAsync(Guid projectId);
    Task MarkEntriesAsBilledAsync(Guid projectId, Guid invoiceId);
}

public interface IProjectValidationService
{
    Task<bool> ValidateProjectCreationAsync(Project project);
    Task<bool> ValidateTimeEntryAsync(ProjectTimeEntry timeEntry);
    Task<bool> ValidateCostEntryAsync(ProjectCost cost);
    Task<bool> ValidateBudgetEntryAsync(ProjectBudget budget);
    Task<string[]> GetValidationErrorsAsync(Project project);
}

// DTOs and Response Models
public class ProjectProfitabilityAnalysis
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMarginPercentage { get; set; }
    public decimal CostVariance { get; set; }
    public decimal ScheduleVariance { get; set; }
    public decimal EstimatedCompletionPercentage { get; set; }
}

public class ProjectCostBreakdown
{
    public Guid ProjectId { get; set; }
    public Dictionary<CostType, decimal> CostsByType { get; set; } = new();
    public Dictionary<string, decimal> CostsByTask { get; set; } = new();
    public Dictionary<string, decimal> CostsByEmployee { get; set; } = new();
    public decimal TotalCost { get; set; }
    public decimal BudgetedCost { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercentage { get; set; }
}

public class ProjectVarianceAnalysis
{
    public CostType CostType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BudgetedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercentage { get; set; }
    public bool IsOverBudget { get; set; }
}

public class EmployeeProductivityReport
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public decimal BillableHours { get; set; }
    public decimal NonBillableHours { get; set; }
    public decimal ProductivityPercentage { get; set; }
    public decimal AverageHourlyRate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ProjectCount { get; set; }
    public Dictionary<string, decimal> HoursByProject { get; set; } = new();
}

public class ProjectPerformanceMetrics
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal SchedulePerformanceIndex { get; set; }
    public decimal CostPerformanceIndex { get; set; }
    public decimal CostVariance { get; set; }
    public decimal ScheduleVariance { get; set; }
    public decimal EstimateAtCompletion { get; set; }
    public decimal EstimateToComplete { get; set; }
    public decimal VarianceAtCompletion { get; set; }
}

public class OverheadAllocation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public CostType CostType { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal OverheadAmount { get; set; }
    public decimal AllocationRate { get; set; }
    public DateTime AllocationDate { get; set; }
    public string Method { get; set; } = string.Empty;
}