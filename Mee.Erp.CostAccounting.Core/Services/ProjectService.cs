using Mee.Erp.CostAccounting.Core.Entities;
using Mee.Erp.CostAccounting.Core.Enums;
using Mee.Erp.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Mee.Erp.CostAccounting.Core.Services;

public class ProjectService : IProjectService
{
    private readonly CostAccountingDbContext _context;
    private readonly IProjectCostService _costService;
    private readonly IProjectTimeEntryService _timeEntryService;

    public ProjectService(
        CostAccountingDbContext context,
        IProjectCostService costService,
        IProjectTimeEntryService timeEntryService)
    {
        _context = context;
        _costService = costService;
        _timeEntryService = timeEntryService;
    }

    public async Task<IEnumerable<Project>> GetProjectsAsync(Guid companyId)
    {
        return await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Manager)
            .Include(p => p.Tasks)
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(Guid id)
    {
        return await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Manager)
            .Include(p => p.Tasks)
                .ThenInclude(t => t.AssignedTo)
            .Include(p => p.Costs)
            .Include(p => p.TimeEntries)
                .ThenInclude(te => te.Employee)
            .Include(p => p.Milestones)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        // Generate unique project code if not provided
        if (string.IsNullOrEmpty(project.Code))
        {
            project.Code = await GenerateProjectCodeAsync(project.CompanyId);
        }

        project.CreatedDate = DateTime.UtcNow;
        project.UpdatedDate = DateTime.UtcNow;
        project.ActualCost = 0;
        project.BilledAmount = 0;
        project.ProgressPercentage = 0;

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return project;
    }

    public async Task<Project> UpdateProjectAsync(Project project)
    {
        var existingProject = await _context.Projects.FindAsync(project.Id);
        if (existingProject == null)
        {
            throw new InvalidOperationException($"Project with ID {project.Id} not found");
        }

        existingProject.Name = project.Name;
        existingProject.Description = project.Description;
        existingProject.Status = project.Status;
        existingProject.BillingMethod = project.BillingMethod;
        existingProject.CustomerId = project.CustomerId;
        existingProject.ManagerId = project.ManagerId;
        existingProject.StartDate = project.StartDate;
        existingProject.EndDate = project.EndDate;
        existingProject.EstimatedBudget = project.EstimatedBudget;
        existingProject.ContractValue = project.ContractValue;
        existingProject.OverheadAllocationMethod = project.OverheadAllocationMethod;
        existingProject.OverheadRate = project.OverheadRate;
        existingProject.Notes = project.Notes;
        existingProject.IsActive = project.IsActive;
        existingProject.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingProject;
    }

    public async Task DeleteProjectAsync(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            project.IsDeleted = true;
            project.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Project> UpdateProjectProgressAsync(Guid id)
    {
        var project = await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            throw new InvalidOperationException($"Project with ID {id} not found");
        }

        // Calculate progress based on tasks
        if (project.Tasks.Any())
        {
            var completedTasks = project.Tasks.Count(t => t.Status == TaskStatus.Completed);
            var totalTasks = project.Tasks.Count();
            project.ProgressPercentage = totalTasks > 0 
                ? (decimal)completedTasks / totalTasks * 100 
                : 0;
        }

        // Update actual cost
        project.ActualCost = await _costService.CalculateTotalCostsAsync(id);
        project.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<IEnumerable<Project>> GetActiveProjectsAsync(Guid companyId)
    {
        return await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Manager)
            .Where(p => p.CompanyId == companyId && 
                       p.Status == ProjectStatus.Active && 
                       p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<decimal> CalculateProjectCostsAsync(Guid projectId)
    {
        return await _costService.CalculateTotalCostsAsync(projectId);
    }

    public async Task<decimal> CalculateProjectRevenueAsync(Guid projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return 0;

        // For fixed price projects, use contract value
        if (project.BillingMethod == BillingMethod.FixedPrice)
        {
            return project.ContractValue * (project.ProgressPercentage / 100);
        }

        // For time and materials, calculate from time entries
        var billableAmount = await _timeEntryService.GetBillableTimeEntriesAsync(projectId);
        return billableAmount.Sum(te => te.TotalCost);
    }

    public async Task<decimal> CalculateProjectProfitAsync(Guid projectId)
    {
        var revenue = await CalculateProjectRevenueAsync(projectId);
        var costs = await CalculateProjectCostsAsync(projectId);
        return revenue - costs;
    }

    private async Task<string> GenerateProjectCodeAsync(Guid companyId)
    {
        var projectCount = await _context.Projects
            .CountAsync(p => p.CompanyId == companyId);

        return $"PRJ{DateTime.Now:yy}{(projectCount + 1):D4}";
    }
}

public class ProjectTaskService : IProjectTaskService
{
    private readonly CostAccountingDbContext _context;
    private readonly IProjectTimeEntryService _timeEntryService;

    public ProjectTaskService(
        CostAccountingDbContext context,
        IProjectTimeEntryService timeEntryService)
    {
        _context = context;
        _timeEntryService = timeEntryService;
    }

    public async Task<IEnumerable<ProjectTask>> GetProjectTasksAsync(Guid projectId)
    {
        return await _context.ProjectTasks
            .Include(t => t.AssignedTo)
            .Include(t => t.ParentTask)
            .Include(t => t.SubTasks)
            .Include(t => t.TimeEntries)
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Sequence)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<ProjectTask?> GetTaskByIdAsync(Guid id)
    {
        return await _context.ProjectTasks
            .Include(t => t.Project)
            .Include(t => t.AssignedTo)
            .Include(t => t.ParentTask)
            .Include(t => t.SubTasks)
            .Include(t => t.TimeEntries)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<ProjectTask> CreateTaskAsync(ProjectTask task)
    {
        // Generate unique task code if not provided
        if (string.IsNullOrEmpty(task.Code))
        {
            task.Code = await GenerateTaskCodeAsync(task.ProjectId);
        }

        task.CreatedDate = DateTime.UtcNow;
        task.UpdatedDate = DateTime.UtcNow;
        task.ActualHours = 0;
        task.ActualCost = 0;
        task.ProgressPercentage = 0;

        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync();

        return task;
    }

    public async Task<ProjectTask> UpdateTaskAsync(ProjectTask task)
    {
        var existingTask = await _context.ProjectTasks.FindAsync(task.Id);
        if (existingTask == null)
        {
            throw new InvalidOperationException($"Task with ID {task.Id} not found");
        }

        existingTask.Name = task.Name;
        existingTask.Description = task.Description;
        existingTask.Sequence = task.Sequence;
        existingTask.StartDate = task.StartDate;
        existingTask.EndDate = task.EndDate;
        existingTask.EstimatedHours = task.EstimatedHours;
        existingTask.EstimatedCost = task.EstimatedCost;
        existingTask.Status = task.Status;
        existingTask.Priority = task.Priority;
        existingTask.AssignedToId = task.AssignedToId;
        existingTask.Notes = task.Notes;
        existingTask.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingTask;
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        var task = await _context.ProjectTasks.FindAsync(id);
        if (task != null)
        {
            task.IsDeleted = true;
            task.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ProjectTask> UpdateTaskProgressAsync(Guid id)
    {
        var task = await _context.ProjectTasks
            .Include(t => t.TimeEntries)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
        {
            throw new InvalidOperationException($"Task with ID {id} not found");
        }

        // Calculate actual hours from time entries
        task.ActualHours = task.TimeEntries.Sum(te => te.Hours);
        task.ActualCost = task.TimeEntries.Sum(te => te.TotalCost);

        // Calculate progress based on hours if estimated hours are set
        if (task.EstimatedHours > 0)
        {
            task.ProgressPercentage = Math.Min((task.ActualHours / task.EstimatedHours) * 100, 100);
        }

        // Update status based on progress
        if (task.ProgressPercentage >= 100)
        {
            task.Status = TaskStatus.Completed;
        }
        else if (task.ProgressPercentage > 0)
        {
            task.Status = TaskStatus.InProgress;
        }

        task.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return task;
    }

    public async Task<IEnumerable<ProjectTask>> GetTasksByEmployeeAsync(Guid employeeId)
    {
        return await _context.ProjectTasks
            .Include(t => t.Project)
            .Where(t => t.AssignedToId == employeeId)
            .OrderBy(t => t.Project.Name)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    private async Task<string> GenerateTaskCodeAsync(Guid projectId)
    {
        var taskCount = await _context.ProjectTasks
            .CountAsync(t => t.ProjectId == projectId);

        return $"TSK{DateTime.Now:yy}{(taskCount + 1):D4}";
    }
}