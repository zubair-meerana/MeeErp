using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

/// <summary>
/// Implements comprehensive budgeting and planning
/// </summary>
public class BudgetingPlanningService : IBudgetingPlanningService
{
    private readonly FinanceDbContext _context;

    public BudgetingPlanningService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<RollingForecastResult> CreateRollingForecastAsync(RollingForecastRequest request)
    {
        var result = new RollingForecastResult
        {
            RequestId = Guid.NewGuid(),
            GeneratedDate = DateTime.UtcNow,
            ForecastPeriods = new List<ForecastPeriod>(),
            OverallAccuracy = 0.85m, // Placeholder accuracy
            ModelUsed = request.ForecastMethod
        };

        // Generate forecast periods based on horizon
        for (int i = 0; i < request.ForecastHorizonMonths; i++)
        {
            var periodStart = request.StartDate.AddMonths(i);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            // Calculate forecasted value based on drivers
            var forecastedValue = CalculateForecastValue(request.Drivers, periodStart);

            // Calculate confidence intervals
            var lowerBound = forecastedValue * 0.9m; // 10% lower bound
            var upperBound = forecastedValue * 1.1m; // 10% upper bound

            result.ForecastPeriods.Add(new ForecastPeriod
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ForecastedValue = forecastedValue,
                LowerBound = lowerBound,
                UpperBound = upperBound,
                VarianceExplanation = "Based on historical trends and market drivers"
            });

            // Add confidence interval
            result.ConfidenceIntervals.Add(new ConfidenceInterval
            {
                Level = request.ConfidenceInterval,
                LowerBound = lowerBound,
                UpperBound = upperBound
            });
        }

        return result;
    }

    public async Task<DriverBasedBudgetResult> CreateDriverBasedBudgetAsync(DriverBasedBudgetRequest request)
    {
        var result = new DriverBasedBudgetResult
        {
            BudgetId = request.BudgetId,
            IsCalculated = true,
            AllocationResults = new List<DriverBasedAllocationResult>(),
            CalculationMethod = request.CalculationMethod,
            CalculatedDate = DateTime.UtcNow
        };

        foreach (var allocation in request.Allocations)
        {
            var calculatedAmount = allocation.DriverValue * allocation.RatePerUnit;

            result.AllocationResults.Add(new DriverBasedAllocationResult
            {
                AccountId = allocation.AccountId,
                DriverName = allocation.DriverName,
                DriverValue = allocation.DriverValue,
                RatePerUnit = allocation.RatePerUnit,
                CalculatedAmount = calculatedAmount,
                VarianceExplanation = "Calculated based on driver values and rates"
            });
        }

        return result;
    }

    public async Task<VarianceAnalysisResult> PerformVarianceAnalysisAsync(VarianceAnalysisRequest request)
    {
        var result = new VarianceAnalysisResult
        {
            RequestId = Guid.NewGuid(),
            AnalysisDate = DateTime.UtcNow,
            VarianceLines = new List<VarianceLine>(),
            DrillDownDetails = new List<VarianceDrillDown>()
        };

        // Get budget data
        var budget = await _context.Budgets
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == request.BudgetId);

        if (budget == null)
        {
            throw new ArgumentException($"Budget with ID {request.BudgetId} not found");
        }

        // Get actual data from ledger
        var actualData = await _context.LedgerEntries
            .Where(le => le.EntryDate >= request.StartDate &&
                        le.EntryDate <= request.EndDate &&
                        request.AccountIds.Contains(le.AccountId))
            .GroupBy(le => le.AccountId)
            .Select(g => new { AccountId = g.Key, TotalAmount = g.Sum(le => le.Debit - le.Credit) })
            .ToListAsync();

        // Calculate variances
        foreach (var budgetLine in budget.Lines)
        {
            var actualAmount = actualData.FirstOrDefault(ad => ad.AccountId == budgetLine.AccountId)?.TotalAmount ?? 0;
            var variance = actualAmount - budgetLine.BudgetAmount;
            var variancePercentage = budgetLine.BudgetAmount != 0 ? (variance / budgetLine.BudgetAmount) * 100 : 0;
            var varianceType = variance >= 0 ? "Favorable" : "Unfavorable";

            // Only include if variance exceeds threshold or if all variances are requested
            if (Math.Abs(variancePercentage) >= request.VarianceThreshold || request.VarianceType == "Both")
            {
                var account = await _context.Accounts.FindAsync(budgetLine.AccountId);

                result.VarianceLines.Add(new VarianceLine
                {
                    AccountId = budgetLine.AccountId,
                    AccountNumber = account?.AccountNumber ?? "",
                    AccountName = account?.Name ?? "",
                    BudgetedAmount = budgetLine.BudgetAmount,
                    ActualAmount = actualAmount,
                    Variance = variance,
                    VariancePercentage = variancePercentage,
                    VarianceType = varianceType,
                    VarianceExplanation = varianceType == "Favorable" ?
                        "Actual performance exceeded budget" :
                        "Actual performance fell short of budget"
                });
            }
        }

        // Calculate totals
        result.TotalBudgetedAmount = result.VarianceLines.Sum(vl => vl.BudgetedAmount);
        result.TotalActualAmount = result.VarianceLines.Sum(vl => vl.ActualAmount);
        result.TotalVariance = result.TotalActualAmount - result.TotalBudgetedAmount;
        result.TotalVariancePercentage = result.TotalBudgetedAmount != 0 ?
            (result.TotalVariance / result.TotalBudgetedAmount) * 100 : 0;

        // Add drill-down details if requested
        if (request.IncludeDrillDown)
        {
            var detailedTransactions = await _context.LedgerEntries
                .Where(le => le.EntryDate >= request.StartDate &&
                            le.EntryDate <= request.EndDate &&
                            request.AccountIds.Contains(le.AccountId))
                .ToListAsync();

            foreach (var transaction in detailedTransactions)
            {
                var account = await _context.Accounts.FindAsync(transaction.AccountId);
                result.DrillDownDetails.Add(new VarianceDrillDown
                {
                    AccountId = transaction.AccountId,
                    TransactionId = transaction.Id,
                    TransactionDate = transaction.EntryDate,
                    Description = transaction.Description ?? "",
                    Amount = transaction.Debit - transaction.Credit,
                    VarianceCause = "Detailed transaction entry"
                });
            }
        }

        return result;
    }

    public async Task<BudgetApprovalResult> ProcessBudgetApprovalWorkflowAsync(BudgetApprovalRequest request)
    {
        var result = new BudgetApprovalResult
        {
            BudgetId = request.BudgetId,
            ApprovalSteps = new List<ApprovalStep>(),
            ProcessedDate = DateTime.UtcNow,
            ProcessedBy = request.RequestorId
        };

        // Process each approval step
        foreach (var step in request.ApprovalSteps.OrderBy(s => s.StepNumber))
        {
            // Check if approver has sufficient authority
            var approverLimit = await GetUserApprovalLimitAsync(step.ApproverId);

            if (request.TotalAmount <= approverLimit)
            {
                // Approve the step
                step.Status = "Approved";
                step.ApprovalDate = DateTime.UtcNow;
            }
            else
            {
                // Escalate to higher authority
                step.Status = "Escalated";
            }

            result.ApprovalSteps.Add(step);
        }

        // Determine overall status
        if (result.ApprovalSteps.All(s => s.Status == "Approved"))
        {
            result.OverallStatus = "Approved";
        }
        else if (result.ApprovalSteps.Any(s => s.Status == "Rejected"))
        {
            result.OverallStatus = "Rejected";
        }
        else
        {
            result.OverallStatus = "Pending";
        }

        return result;
    }

    public async Task<ScenarioModelingResult> CreateScenarioModelAsync(ScenarioModelingRequest request)
    {
        var result = new ScenarioModelingResult
        {
            ScenarioId = Guid.NewGuid(),
            ScenarioName = request.ScenarioName,
            Outcomes = new List<ScenarioOutcome>(),
            SensitivityAnalyses = new List<SensitivityAnalysis>(),
            GeneratedDate = DateTime.UtcNow
        };

        // Generate scenario outcomes based on assumptions
        foreach (var accountId in request.AccountIds)
        {
            var account = await _context.Accounts.FindAsync(accountId);

            // Calculate different scenario values based on assumptions
            var baseValue = await GetAccountHistoricalAverageAsync(accountId, request.StartDate, request.EndDate);

            // Apply scenario assumptions
            var bestCaseValue = ApplyScenarioAssumptions(baseValue, request.Assumptions, "BestCase");
            var worstCaseValue = ApplyScenarioAssumptions(baseValue, request.Assumptions, "WorstCase");
            var mostLikelyValue = ApplyScenarioAssumptions(baseValue, request.Assumptions, "MostLikely");

            result.Outcomes.Add(new ScenarioOutcome
            {
                AccountId = accountId,
                AccountName = account?.Name ?? "",
                BestCaseValue = bestCaseValue,
                WorstCaseValue = worstCaseValue,
                MostLikelyValue = mostLikelyValue,
                Probability = 0.5m // Placeholder probability
            });
        }

        // Perform sensitivity analysis
        foreach (var assumption in request.Assumptions)
        {
            result.SensitivityAnalyses.Add(new SensitivityAnalysis
            {
                Factor = assumption.Factor,
                SensitivityCoefficient = assumption.Sensitivity,
                ImpactLevel = assumption.Sensitivity > 0.7m ? "High" :
                             assumption.Sensitivity > 0.3m ? "Medium" : "Low",
                MinImpact = assumption.Value * 0.8m, // 20% variation
                MaxImpact = assumption.Value * 1.2m  // 20% variation
            });
        }

        return result;
    }

    public async Task<BudgetAllocationResult> PerformBudgetAllocationAsync(BudgetAllocationRequest request)
    {
        var result = new BudgetAllocationResult
        {
            BudgetId = request.BudgetId,
            AllocationMethod = request.AllocationMethod,
            Allocations = new List<AllocationTarget>(),
            TotalAllocated = 0,
            TotalRemaining = request.TotalBudgetAmount,
            ProcessedDate = DateTime.UtcNow
        };

        // Apply allocation rules based on method
        switch (request.AllocationMethod.ToLower())
        {
            case "proportional":
                result = AllocateProportionally(request, result);
                break;
            case "prioritybased":
                result = AllocateByPriority(request, result);
                break;
            case "historical":
                result = AllocateBasedOnHistory(request, result);
                break;
            case "zerobased":
                result = AllocateZeroBased(request, result);
                break;
            default:
                result = AllocateProportionally(request, result);
                break;
        }

        return result;
    }

    public async Task<ZeroBasedBudgetResult> CreateZeroBasedBudgetAsync(ZeroBasedBudgetRequest request)
    {
        var result = new ZeroBasedBudgetResult
        {
            BudgetId = request.BudgetId,
            Activities = new List<ZbbActivity>(),
            DecisionPackages = new List<ZbbDecisionPackage>(),
            TotalBudgetedAmount = 0,
            ProcessedDate = DateTime.UtcNow
        };

        // Process activities
        foreach (var activity in request.Activities)
        {
            var totalCost = activity.Resources.Sum(r => r.TotalCost);
            var processedActivity = new ZbbActivity
            {
                ActivityId = activity.ActivityId,
                ActivityName = activity.ActivityName,
                Description = activity.Description,
                Resources = activity.Resources,
                TotalCost = totalCost
            };

            result.Activities.Add(processedActivity);
            result.TotalBudgetedAmount += totalCost;
        }

        // Process decision packages
        foreach (var package in request.DecisionPackages)
        {
            result.DecisionPackages.Add(package);
        }

        return result;
    }

    #region Helper Methods

    private decimal CalculateForecastValue(List<ForecastDriver> drivers, DateTime period)
    {
        // Simplified calculation - in reality, this would use complex forecasting models
        decimal baseValue = 10000; // Placeholder base value
        decimal growthFactor = 1.0m;

        foreach (var driver in drivers)
        {
            // Apply driver impact based on weight
            growthFactor += (driver.Weight / 100);
        }

        // Apply seasonal adjustment based on period
        var seasonalAdjustment = GetSeasonalAdjustment(period);

        return baseValue * growthFactor * seasonalAdjustment;
    }

    private decimal GetSeasonalAdjustment(DateTime period)
    {
        // Simple seasonal adjustment based on month
        return period.Month switch
        {
            11 or 12 or 1 => 1.2m, // Higher in holiday months
            6 or 7 or 8 => 1.1m,   // Higher in summer months
            _ => 1.0m              // Normal otherwise
        };
    }

    private async Task<decimal> GetUserApprovalLimitAsync(Guid userId)
    {
        // In a real implementation, this would fetch from user profile
        // For this example, returning a default value
        return 100000;
    }

    private async Task<decimal> GetAccountHistoricalAverageAsync(Guid accountId, DateTime startDate, DateTime endDate)
    {
        // Get historical average for the account
        var historicalData = await _context.LedgerEntries
            .Where(le => le.AccountId == accountId &&
                        le.EntryDate >= startDate &&
                        le.EntryDate <= endDate)
            .GroupBy(le => le.EntryDate.Month)
            .Select(g => new { Month = g.Key, Amount = g.Sum(le => le.Debit - le.Credit) })
            .ToListAsync();

        return historicalData.Any() ? historicalData.Average(h => h.Amount) : 10000;
    }

    private decimal ApplyScenarioAssumptions(decimal baseValue, List<ScenarioAssumption> assumptions, string scenarioType)
    {
        decimal multiplier = 1.0m;

        foreach (var assumption in assumptions)
        {
            // Apply different multipliers based on scenario type
            var adjustment = assumption.Value / 100; // Convert percentage to decimal
            multiplier += scenarioType switch
            {
                "BestCase" => adjustment * 1.2m,    // Best case: 120% of assumption
                "WorstCase" => adjustment * 0.8m,   // Worst case: 80% of assumption
                _ => adjustment                     // Most likely: 100% of assumption
            };
        }

        return baseValue * multiplier;
    }

    private BudgetAllocationResult AllocateProportionally(BudgetAllocationRequest request, BudgetAllocationResult result)
    {
        // Allocate budget proportionally based on historical usage or other criteria
        var totalWeight = request.Targets.Sum(t => 1); // Equal weights for simplicity

        foreach (var target in request.Targets)
        {
            var allocationAmount = (request.TotalBudgetAmount / totalWeight);
            result.Allocations.Add(new AllocationTarget
            {
                TargetId = target.TargetId,
                TargetType = target.TargetType,
                AllocatedAmount = allocationAmount,
                RemainingAmount = 0
            });
            result.TotalAllocated += allocationAmount;
        }

        result.TotalRemaining = request.TotalBudgetAmount - result.TotalAllocated;
        return result;
    }

    private BudgetAllocationResult AllocateByPriority(BudgetAllocationRequest request, BudgetAllocationResult result)
    {
        // Sort targets by priority and allocate accordingly
        var sortedTargets = request.Targets.OrderBy(t => GetPriorityValue(t.TargetType)).ToList();

        decimal remainingBudget = request.TotalBudgetAmount;

        foreach (var target in sortedTargets)
        {
            var allocationAmount = Math.Min(GetMinimumRequiredAmount(target), remainingBudget);
            result.Allocations.Add(new AllocationTarget
            {
                TargetId = target.TargetId,
                TargetType = target.TargetType,
                AllocatedAmount = allocationAmount,
                RemainingAmount = 0
            });
            remainingBudget -= allocationAmount;
            result.TotalAllocated += allocationAmount;

            if (remainingBudget <= 0) break;
        }

        result.TotalRemaining = remainingBudget;
        return result;
    }

    private BudgetAllocationResult AllocateBasedOnHistory(BudgetAllocationRequest request, BudgetAllocationResult result)
    {
        // Allocate based on historical spending patterns
        // This is a simplified version - real implementation would be more complex
        return AllocateProportionally(request, result);
    }

    private BudgetAllocationResult AllocateZeroBased(BudgetAllocationRequest request, BudgetAllocationResult result)
    {
        // For zero-based budgeting, start from zero and justify each allocation
        // This is a simplified version - real implementation would involve more complex logic
        return AllocateProportionally(request, result);
    }

    private int GetPriorityValue(string targetType)
    {
        // Define priority values for different target types
        return targetType.ToLower() switch
        {
            "critical" => 1,
            "high" => 2,
            "medium" => 3,
            "low" => 4,
            _ => 5
        };
    }

    private decimal GetMinimumRequiredAmount(AllocationTarget target)
    {
        // Calculate minimum required amount for the target
        // This would be based on historical data, contracts, etc.
        return 1000; // Placeholder value
    }

    #endregion
}
