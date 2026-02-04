using Mee.Erp.CostAccounting.Core.Entities;
using Mee.Erp.CostAccounting.Core.Enums;
using Mee.Erp.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Mee.Erp.CostAccounting.Core.Services;

public class ProjectCostService : IProjectCostService
{
    private readonly CostAccountingDbContext _context;

    public ProjectCostService(CostAccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProjectCost>> GetProjectCostsAsync(Guid projectId)
    {
        return await _context.ProjectCosts
            .Include(c => c.Task)
            .Include(c => c.Account)
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.TransactionDate)
            .ThenBy(c => c.Description)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProjectCost>> GetCostsByTypeAsync(Guid projectId, CostType costType)
    {
        return await _context.ProjectCosts
            .Include(c => c.Task)
            .Include(c => c.Account)
            .Where(c => c.ProjectId == projectId && c.CostType == costType)
            .OrderByDescending(c => c.TransactionDate)
            .ToListAsync();
    }

    public async Task<ProjectCost> AddCostAsync(ProjectCost cost)
    {
        cost.TotalAmount = cost.Quantity * cost.UnitPrice;
        cost.CreatedDate = DateTime.UtcNow;
        cost.UpdatedDate = DateTime.UtcNow;

        _context.ProjectCosts.Add(cost);
        await _context.SaveChangesAsync();

        return cost;
    }

    public async Task<ProjectCost> UpdateCostAsync(ProjectCost cost)
    {
        var existingCost = await _context.ProjectCosts.FindAsync(cost.Id);
        if (existingCost == null)
        {
            throw new InvalidOperationException($"Cost with ID {cost.Id} not found");
        }

        existingCost.TaskId = cost.TaskId;
        existingCost.CostType = cost.CostType;
        existingCost.TransactionType = cost.TransactionType;
        existingCost.Description = cost.Description;
        existingCost.Quantity = cost.Quantity;
        existingCost.UnitPrice = cost.UnitPrice;
        existingCost.TotalAmount = cost.Quantity * cost.UnitPrice;
        existingCost.AccountId = cost.AccountId;
        existingCost.TransactionDate = cost.TransactionDate;
        existingCost.ReferenceNumber = cost.ReferenceNumber;
        existingCost.SupplierInvoice = cost.SupplierInvoice;
        existingCost.Notes = cost.Notes;
        existingCost.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingCost;
    }

    public async Task DeleteCostAsync(Guid id)
    {
        var cost = await _context.ProjectCosts.FindAsync(id);
        if (cost != null)
        {
            cost.IsDeleted = true;
            cost.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<decimal> CalculateTotalCostsAsync(Guid projectId)
    {
        return await _context.ProjectCosts
            .Where(c => c.ProjectId == projectId)
            .SumAsync(c => c.TotalAmount);
    }

    public async Task<decimal> CalculateCostsByTypeAsync(Guid projectId, CostType costType)
    {
        return await _context.ProjectCosts
            .Where(c => c.ProjectId == projectId && c.CostType == costType)
            .SumAsync(c => c.TotalAmount);
    }

    public async Task<decimal> CalculateActualVsBudgetVarianceAsync(Guid projectId)
    {
        var totalBudget = await _context.ProjectBudgets
            .Where(b => b.ProjectId == projectId)
            .SumAsync(b => b.BudgetedAmount);

        var totalActual = await CalculateTotalCostsAsync(projectId);

        return totalBudget - totalActual;
    }
}

public class ProjectTimeEntryService : IProjectTimeEntryService
{
    private readonly CostAccountingDbContext _context;

    public ProjectTimeEntryService(CostAccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProjectTimeEntry>> GetTimeEntriesAsync(Guid projectId)
    {
        return await _context.ProjectTimeEntries
            .Include(te => te.Project)
            .Include(te => te.Task)
            .Include(te => te.Employee)
            .Where(te => te.ProjectId == projectId)
            .OrderByDescending(te => te.EntryDate)
            .ThenBy(te => te.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProjectTimeEntry>> GetTimeEntriesByEmployeeAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        return await _context.ProjectTimeEntries
            .Include(te => te.Project)
            .Include(te => te.Task)
            .Where(te => te.EmployeeId == employeeId && 
                        te.EntryDate >= startDate && 
                        te.EntryDate <= endDate)
            .OrderByDescending(te => te.EntryDate)
            .ThenBy(te => te.StartTime)
            .ToListAsync();
    }

    public async Task<ProjectTimeEntry> AddTimeEntryAsync(ProjectTimeEntry timeEntry)
    {
        // Calculate hours if not provided
        if (timeEntry.Hours == 0)
        {
            timeEntry.Hours = (decimal)(timeEntry.EndTime - timeEntry.StartTime).TotalHours;
        }

        // Calculate total cost
        timeEntry.TotalCost = timeEntry.Hours * timeEntry.HourlyRate;
        timeEntry.CreatedDate = DateTime.UtcNow;
        timeEntry.UpdatedDate = DateTime.UtcNow;

        _context.ProjectTimeEntries.Add(timeEntry);
        await _context.SaveChangesAsync();

        return timeEntry;
    }

    public async Task<ProjectTimeEntry> UpdateTimeEntryAsync(ProjectTimeEntry timeEntry)
    {
        var existingEntry = await _context.ProjectTimeEntries.FindAsync(timeEntry.Id);
        if (existingEntry == null)
        {
            throw new InvalidOperationException($"Time entry with ID {timeEntry.Id} not found");
        }

        existingEntry.TaskId = timeEntry.TaskId;
        existingEntry.EntryDate = timeEntry.EntryDate;
        existingEntry.StartTime = timeEntry.StartTime;
        existingEntry.EndTime = timeEntry.EndTime;
        existingEntry.Hours = timeEntry.Hours == 0 
            ? (decimal)(timeEntry.EndTime - timeEntry.StartTime).TotalHours 
            : timeEntry.Hours;
        existingEntry.HourlyRate = timeEntry.HourlyRate;
        existingEntry.TotalCost = existingEntry.Hours * existingEntry.HourlyRate;
        existingEntry.Description = timeEntry.Description;
        existingEntry.IsBillable = timeEntry.IsBillable;
        existingEntry.Notes = timeEntry.Notes;
        existingEntry.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingEntry;
    }

    public async Task DeleteTimeEntryAsync(Guid id)
    {
        var timeEntry = await _context.ProjectTimeEntries.FindAsync(id);
        if (timeEntry != null)
        {
            timeEntry.IsDeleted = true;
            timeEntry.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<decimal> CalculateTotalHoursAsync(Guid projectId)
    {
        return await _context.ProjectTimeEntries
            .Where(te => te.ProjectId == projectId)
            .SumAsync(te => te.Hours);
    }

    public async Task<decimal> CalculateLaborCostsAsync(Guid projectId)
    {
        return await _context.ProjectTimeEntries
            .Where(te => te.ProjectId == projectId)
            .SumAsync(te => te.TotalCost);
    }

    public async Task<decimal> CalculateEmployeeHoursAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        return await _context.ProjectTimeEntries
            .Where(te => te.EmployeeId == employeeId && 
                        te.EntryDate >= startDate && 
                        te.EntryDate <= endDate)
            .SumAsync(te => te.Hours);
    }

    public async Task<IEnumerable<ProjectTimeEntry>> GetUnbilledTimeEntriesAsync(Guid projectId)
    {
        return await _context.ProjectTimeEntries
            .Include(te => te.Project)
            .Include(te => te.Task)
            .Include(te => te.Employee)
            .Where(te => te.ProjectId == projectId && 
                        te.IsBillable && 
                        !te.IsBilled)
            .OrderBy(te => te.EntryDate)
            .ThenBy(te => te.Employee.LastName)
            .ToListAsync();
    }

    public async Task<decimal> GetBillableTimeEntriesAsync(Guid projectId)
    {
        return await _context.ProjectTimeEntries
            .Where(te => te.ProjectId == projectId && te.IsBillable)
            .SumAsync(te => te.TotalCost);
    }
}

public class ProjectBudgetService : IProjectBudgetService
{
    private readonly CostAccountingDbContext _context;
    private readonly IProjectCostService _costService;

    public ProjectBudgetService(
        CostAccountingDbContext context,
        IProjectCostService costService)
    {
        _context = context;
        _costService = costService;
    }

    public async Task<IEnumerable<ProjectBudget>> GetProjectBudgetsAsync(Guid projectId)
    {
        return await _context.ProjectBudgets
            .Include(b => b.Account)
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.CostType)
            .ThenBy(b => b.Description)
            .ToListAsync();
    }

    public async Task<ProjectBudget> CreateBudgetAsync(ProjectBudget budget)
    {
        budget.ActualAmount = 0;
        budget.Variance = budget.BudgetedAmount;
        budget.VariancePercentage = budget.BudgetedAmount > 0 ? 100 : 0;
        budget.CreatedDate = DateTime.UtcNow;
        budget.UpdatedDate = DateTime.UtcNow;

        _context.ProjectBudgets.Add(budget);
        await _context.SaveChangesAsync();

        return budget;
    }

    public async Task<ProjectBudget> UpdateBudgetAsync(ProjectBudget budget)
    {
        var existingBudget = await _context.ProjectBudgets.FindAsync(budget.Id);
        if (existingBudget == null)
        {
            throw new InvalidOperationException($"Budget with ID {budget.Id} not found");
        }

        existingBudget.CostType = budget.CostType;
        existingBudget.Description = budget.Description;
        existingBudget.BudgetedAmount = budget.BudgetedAmount;
        existingBudget.AccountId = budget.AccountId;
        existingBudget.FiscalYear = budget.FiscalYear;
        existingBudget.FiscalPeriod = budget.FiscalPeriod;
        
        // Recalculate variance
        existingBudget.Variance = existingBudget.BudgetedAmount - existingBudget.ActualAmount;
        existingBudget.VariancePercentage = existingBudget.BudgetedAmount > 0 
            ? (existingBudget.Variance / existingBudget.BudgetedAmount) * 100 
            : 0;
        
        existingBudget.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingBudget;
    }

    public async Task DeleteBudgetAsync(Guid id)
    {
        var budget = await _context.ProjectBudgets.FindAsync(id);
        if (budget != null)
        {
            budget.IsDeleted = true;
            budget.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<decimal> CalculateTotalBudgetAsync(Guid projectId)
    {
        return await _context.ProjectBudgets
            .Where(b => b.ProjectId == projectId)
            .SumAsync(b => b.BudgetedAmount);
    }

    public async Task<decimal> CalculateBudgetVarianceAsync(Guid projectId)
    {
        return await _context.ProjectBudgets
            .Where(b => b.ProjectId == projectId)
            .SumAsync(b => b.Variance);
    }

    public async Task UpdateBudgetActualsAsync(Guid projectId)
    {
        var budgets = await _context.ProjectBudgets
            .Where(b => b.ProjectId == projectId)
            .ToListAsync();

        foreach (var budget in budgets)
        {
            // Calculate actual amount based on cost type
            budget.ActualAmount = await _costService.CalculateCostsByTypeAsync(projectId, budget.CostType);
            budget.Variance = budget.BudgetedAmount - budget.ActualAmount;
            budget.VariancePercentage = budget.BudgetedAmount > 0 
                ? (budget.Variance / budget.BudgetedAmount) * 100 
                : 0;
            budget.UpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}