using Mee.Erp.CostAccounting.Core.Entities;
using Mee.Erp.CostAccounting.Core.Enums;
using Mee.Erp.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Mee.Erp.CostAccounting.Core.Services;

public class CostAnalysisService : ICostAnalysisService
{
    private readonly CostAccountingDbContext _context;
    private readonly IProjectCostService _costService;
    private readonly IProjectTimeEntryService _timeEntryService;

    public CostAnalysisService(
        CostAccountingDbContext context,
        IProjectCostService costService,
        IProjectTimeEntryService timeEntryService)
    {
        _context = context;
        _costService = costService;
        _timeEntryService = timeEntryService;
    }

    public async Task<ProjectProfitabilityAnalysis> AnalyzeProjectProfitabilityAsync(Guid projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            throw new InvalidOperationException($"Project with ID {projectId} not found");
        }

        var totalCost = await _costService.CalculateTotalCostsAsync(projectId);
        var totalRevenue = await CalculateProjectRevenueAsync(project);
        var grossProfit = totalRevenue - totalCost;
        var profitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

        return new ProjectProfitabilityAnalysis
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            GrossProfit = grossProfit,
            ProfitMarginPercentage = profitMargin,
            CostVariance = await CalculateCostVarianceAsync(projectId),
            ScheduleVariance = await CalculateScheduleVarianceAsync(projectId),
            EstimatedCompletionPercentage = project.ProgressPercentage
        };
    }

    public async Task<ProjectCostBreakdown> GetCostBreakdownAsync(Guid projectId)
    {
        var costs = await _context.ProjectCosts
            .Include(c => c.Task)
            .ToListAsync();

        var timeEntries = await _context.ProjectTimeEntries
            .Include(te => te.Employee)
            .ToListAsync();

        var costsByType = new Dictionary<CostType, decimal>();
        var costsByTask = new Dictionary<string, decimal>();
        var costsByEmployee = new Dictionary<string, decimal>();

        // Calculate costs by type
        foreach (var cost in costs)
        {
            costsByType[cost.CostType] = costsByType.GetValueOrDefault(cost.CostType, 0) + cost.TotalAmount;
        }

        // Add labor costs from time entries
        var laborCosts = timeEntries.Sum(te => te.TotalCost);
        costsByType[CostType.Labor] = costsByType.GetValueOrDefault(CostType.Labor, 0) + laborCosts;

        // Calculate costs by task
        foreach (var cost in costs.Where(c => c.Task != null))
        {
            var taskName = cost.Task!.Name;
            costsByTask[taskName] = costsByTask.GetValueOrDefault(taskName, 0) + cost.TotalAmount;
        }

        foreach (var timeEntry in timeEntries.Where(te => te.Task != null))
        {
            var taskName = timeEntry.Task!.Name;
            costsByTask[taskName] = costsByTask.GetValueOrDefault(taskName, 0) + timeEntry.TotalCost;
        }

        // Calculate costs by employee
        foreach (var timeEntry in timeEntries)
        {
            var employeeName = $"{timeEntry.Employee.FirstName} {timeEntry.Employee.LastName}";
            costsByEmployee[employeeName] = costsByEmployee.GetValueOrDefault(employeeName, 0) + timeEntry.TotalCost;
        }

        var totalCost = costsByType.Values.Sum();
        var budgetedCost = await _context.ProjectBudgets
            .Where(b => b.ProjectId == projectId)
            .SumAsync(b => b.BudgetedAmount);
        var variance = budgetedCost - totalCost;
        var variancePercentage = budgetedCost > 0 ? (variance / budgetedCost) * 100 : 0;

        return new ProjectCostBreakdown
        {
            ProjectId = projectId,
            CostsByType = costsByType,
            CostsByTask = costsByTask,
            CostsByEmployee = costsByEmployee,
            TotalCost = totalCost,
            BudgetedCost = budgetedCost,
            Variance = variance,
            VariancePercentage = variancePercentage
        };
    }

    public async Task<IEnumerable<ProjectVarianceAnalysis>> AnalyzeBudgetVariancesAsync(Guid projectId)
    {
        var budgets = await _context.ProjectBudgets
            .Where(b => b.ProjectId == projectId)
            .ToListAsync();

        var variances = new List<ProjectVarianceAnalysis>();

        foreach (var budget in budgets)
        {
            variances.Add(new ProjectVarianceAnalysis
            {
                CostType = budget.CostType,
                Description = budget.Description,
                BudgetedAmount = budget.BudgetedAmount,
                ActualAmount = budget.ActualAmount,
                Variance = budget.Variance,
                VariancePercentage = budget.VariancePercentage,
                IsOverBudget = budget.Variance < 0
            });
        }

        return variances;
    }

    public async Task<EmployeeProductivityReport> GetEmployeeProductivityAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        var timeEntries = await _context.ProjectTimeEntries
            .Include(te => te.Employee)
            .Include(te => te.Project)
            .Where(te => te.EmployeeId == employeeId && 
                        te.EntryDate >= startDate && 
                        te.EntryDate <= endDate)
            .ToListAsync();

        var totalHours = timeEntries.Sum(te => te.Hours);
        var billableHours = timeEntries.Where(te => te.IsBillable).Sum(te => te.Hours);
        var nonBillableHours = totalHours - billableHours;
        var productivityPercentage = totalHours > 0 ? (billableHours / totalHours) * 100 : 0;
        var averageHourlyRate = totalHours > 0 ? timeEntries.Average(te => te.HourlyRate) : 0;
        var totalRevenue = timeEntries.Where(te => te.IsBillable).Sum(te => te.TotalCost);

        var hoursByProject = timeEntries
            .GroupBy(te => te.Project.Name)
            .ToDictionary(g => g.Key, g => g.Sum(te => te.Hours));

        return new EmployeeProductivityReport
        {
            EmployeeId = employeeId,
            EmployeeName = $"{timeEntries.FirstOrDefault()?.Employee.FirstName} {timeEntries.FirstOrDefault()?.Employee.LastName}",
            TotalHours = totalHours,
            BillableHours = billableHours,
            NonBillableHours = nonBillableHours,
            ProductivityPercentage = productivityPercentage,
            AverageHourlyRate = averageHourlyRate,
            TotalRevenue = totalRevenue,
            ProjectCount = timeEntries.Select(te => te.ProjectId).Distinct().Count(),
            HoursByProject = hoursByProject
        };
    }

    public async Task<ProjectPerformanceMetrics> GetProjectPerformanceAsync(Guid projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            throw new InvalidOperationException($"Project with ID {projectId} not found");
        }

        var actualCost = await _costService.CalculateTotalCostsAsync(projectId);
        var plannedValue = project.EstimatedBudget ?? 0;
        var earnedValue = plannedValue * (project.ProgressPercentage / 100);

        // Calculate EVM metrics
        var costVariance = earnedValue - actualCost;
        var scheduleVariance = earnedValue - plannedValue;
        var costPerformanceIndex = earnedValue > 0 ? earnedValue / actualCost : 0;
        var schedulePerformanceIndex = plannedValue > 0 ? earnedValue / plannedValue : 0;

        // Forecasting
        var estimateAtCompletion = costPerformanceIndex > 0 ? plannedValue / costPerformanceIndex : actualCost;
        var estimateToComplete = estimateAtCompletion - actualCost;
        var varianceAtCompletion = plannedValue - estimateAtCompletion;

        return new ProjectPerformanceMetrics
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            SchedulePerformanceIndex = schedulePerformanceIndex,
            CostPerformanceIndex = costPerformanceIndex,
            CostVariance = costVariance,
            ScheduleVariance = scheduleVariance,
            EstimateAtCompletion = estimateAtCompletion,
            EstimateToComplete = estimateToComplete,
            VarianceAtCompletion = varianceAtCompletion
        };
    }

    public async Task<IEnumerable<Project>> GetProfitableProjectsAsync(Guid companyId, decimal minProfitMargin)
    {
        var projects = await _context.Projects
            .Where(p => p.CompanyId == companyId && p.Status == ProjectStatus.Active)
            .ToListAsync();

        var profitableProjects = new List<Project>();

        foreach (var project in projects)
        {
            var analysis = await AnalyzeProjectProfitabilityAsync(project.Id);
            if (analysis.ProfitMarginPercentage >= minProfitMargin)
            {
                profitableProjects.Add(project);
            }
        }

        return profitableProjects;
    }

    private async Task<decimal> CalculateProjectRevenueAsync(Project project)
    {
        if (project.BillingMethod == BillingMethod.FixedPrice)
        {
            return project.ContractValue * (project.ProgressPercentage / 100);
        }

        // For time and materials
        var billableAmount = await _context.ProjectTimeEntries
            .Where(te => te.ProjectId == project.Id && te.IsBillable)
            .SumAsync(te => te.TotalCost);

        return billableAmount;
    }

    private async Task<decimal> CalculateCostVarianceAsync(Guid projectId)
    {
        var budgetedAmount = await _context.ProjectBudgets
            .Where(b => b.ProjectId == projectId)
            .SumAsync(b => b.BudgetedAmount);

        var actualAmount = await _costService.CalculateTotalCostsAsync(projectId);

        return budgetedAmount - actualAmount;
    }

    private async Task<decimal> CalculateScheduleVarianceAsync(Guid projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return 0;

        var plannedDuration = (project.EndDate ?? DateTime.Today) - project.StartDate;
        var elapsedDuration = DateTime.Today - project.StartDate;
        var plannedProgress = plannedDuration.TotalDays > 0 
            ? (elapsedDuration.TotalDays / plannedDuration.TotalDays) * 100 
            : 0;

        return project.ProgressPercentage - (decimal)plannedProgress;
    }
}

public class OverheadAllocationService : IOverheadAllocationService
{
    private readonly CostAccountingDbContext _context;

    public OverheadAllocationService(CostAccountingDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> CalculateOverheadAsync(Guid projectId, Guid taskId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return 0;

        var task = await _context.ProjectTasks.FindAsync(taskId);
        if (task == null) return 0;

        return project.OverheadAllocationMethod switch
        {
            OverheadAllocationMethod.DirectLaborCost => await CalculateOverheadByLaborCostAsync(projectId, taskId),
            OverheadAllocationMethod.DirectLaborHours => await CalculateOverheadByLaborHoursAsync(projectId, taskId),
            OverheadAllocationMethod.MachineHours => await CalculateOverheadByMachineHoursAsync(taskId),
            OverheadAllocationMethod.MaterialCost => await CalculateOverheadByMaterialCostAsync(projectId, taskId),
            OverheadAllocationMethod.FixedPercentage => await CalculateOverheadByFixedPercentageAsync(taskId),
            _ => 0
        };
    }

    public async Task AllocateOverheadToProjectAsync(Guid projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null || project.OverheadRate == 0) return;

        foreach (var task in project.Tasks)
        {
            var overheadAmount = await CalculateOverheadAsync(projectId, task.Id);
            
            if (overheadAmount > 0)
            {
                var overheadCost = new ProjectCost
                {
                    ProjectId = projectId,
                    TaskId = task.Id,
                    CostType = CostType.Overhead,
                    TransactionType = CostTransactionType.Actual,
                    Description = $"Overhead allocation - {project.OverheadAllocationMethod}",
                    Quantity = 1,
                    UnitPrice = overheadAmount,
                    TotalAmount = overheadAmount,
                    TransactionDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.ProjectCosts.Add(overheadCost);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<decimal> CalculateOverheadRateAsync(Guid companyId, OverheadAllocationMethod method)
    {
        // This would typically be calculated based on company-wide overhead costs
        // For now, return a default rate based on method
        return method switch
        {
            OverheadAllocationMethod.DirectLaborCost => 0.15m, // 15% of labor cost
            OverheadAllocationMethod.DirectLaborHours => 25m, // $25 per labor hour
            OverheadAllocationMethod.MachineHours => 35m, // $35 per machine hour
            OverheadAllocationMethod.MaterialCost => 0.10m, // 10% of material cost
            OverheadAllocationMethod.FixedPercentage => 0.20m, // 20% fixed
            _ => 0
        };
    }

    public async Task<IEnumerable<OverheadAllocation>> GetOverheadAllocationsAsync(Guid projectId)
    {
        var overheadCosts = await _context.ProjectCosts
            .Include(c => c.Task)
            .Where(c => c.ProjectId == projectId && c.CostType == CostType.Overhead)
            .ToListAsync();

        return overheadCosts.Select(c => new OverheadAllocation
        {
            Id = c.Id,
            ProjectId = c.ProjectId,
            TaskId = c.TaskId,
            CostType = c.CostType,
            BaseAmount = 0, // Would need to be calculated based on allocation method
            OverheadAmount = c.TotalAmount,
            AllocationRate = 0, // Would need to be calculated
            AllocationDate = c.TransactionDate,
            Method = "Default"
        });
    }

    private async Task<decimal> CalculateOverheadByLaborCostAsync(Guid projectId, Guid taskId)
    {
        var laborCost = await _context.ProjectTimeEntries
            .Where(te => te.ProjectId == projectId && te.TaskId == taskId)
            .SumAsync(te => te.TotalCost);

        return laborCost * 0.15m; // 15% of labor cost
    }

    private async Task<decimal> CalculateOverheadByLaborHoursAsync(Guid projectId, Guid taskId)
    {
        var laborHours = await _context.ProjectTimeEntries
            .Where(te => te.ProjectId == projectId && te.TaskId == taskId)
            .SumAsync(te => te.Hours);

        return laborHours * 25m; // $25 per labor hour
    }

    private async Task<decimal> CalculateOverheadByMachineHoursAsync(Guid taskId)
    {
        // This would need machine hours tracking - for now, return 0
        return 0;
    }

    private async Task<decimal> CalculateOverheadByMaterialCostAsync(Guid projectId, Guid taskId)
    {
        var materialCost = await _context.ProjectMaterialUsages
            .Where(mu => mu.ProjectId == projectId && mu.TaskId == taskId)
            .SumAsync(mu => mu.TotalCost);

        return materialCost * 0.10m; // 10% of material cost
    }

    private async Task<decimal> CalculateOverheadByFixedPercentageAsync(Guid taskId)
    {
        var task = await _context.ProjectTasks.FindAsync(taskId);
        return task?.EstimatedCost * 0.20m ?? 0; // 20% of estimated cost
    }
}