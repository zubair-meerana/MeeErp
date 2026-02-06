using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for comprehensive budgeting and planning
/// </summary>
public interface IBudgetingPlanningService
{
    /// <summary>
    /// Creates rolling forecasts with predictive analytics
    /// </summary>
    Task<RollingForecastResult> CreateRollingForecastAsync(RollingForecastRequest request);

    /// <summary>
    /// Implements driver-based budgeting methodologies
    /// </summary>
    Task<DriverBasedBudgetResult> CreateDriverBasedBudgetAsync(DriverBasedBudgetRequest request);

    /// <summary>
    /// Performs budget vs. actual variance analysis with drill-down
    /// </summary>
    Task<VarianceAnalysisResult> PerformVarianceAnalysisAsync(VarianceAnalysisRequest request);

    /// <summary>
    /// Manages budget approval workflows with multiple levels
    /// </summary>
    Task<BudgetApprovalResult> ProcessBudgetApprovalWorkflowAsync(BudgetApprovalRequest request);

    /// <summary>
    /// Creates scenario planning and modeling capabilities
    /// </summary>
    Task<ScenarioModelingResult> CreateScenarioModelAsync(ScenarioModelingRequest request);

    /// <summary>
    /// Implements budget allocation algorithms
    /// </summary>
    Task<BudgetAllocationResult> PerformBudgetAllocationAsync(BudgetAllocationRequest request);

    /// <summary>
    /// Supports zero-based budgeting
    /// </summary>
    Task<ZeroBasedBudgetResult> CreateZeroBasedBudgetAsync(ZeroBasedBudgetRequest request);
}

public class RollingForecastRequest
{
    public Guid CompanyId { get; set; }
    public int ForecastHorizonMonths { get; set; }
    public DateTime StartDate { get; set; }
    public string ForecastMethod { get; set; } // "TimeSeries", "Regression", "MachineLearning"
    public List<ForecastDriver> Drivers { get; set; } = new List<ForecastDriver>();
    public bool IncludePredictiveAnalytics { get; set; }
    public string ConfidenceInterval { get; set; } // "80%", "90%", "95%"
}

public class ForecastDriver
{
    public string Name { get; set; }
    public string Type { get; set; } // "Economic", "Operational", "Market"
    public decimal Weight { get; set; }
    public string Formula { get; set; }
    public List<HistoricalDataPoint> HistoricalData { get; set; } = new List<HistoricalDataPoint>();
}

public class HistoricalDataPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string DataSource { get; set; }
}

public class RollingForecastResult
{
    public Guid RequestId { get; set; }
    public DateTime GeneratedDate { get; set; }
    public List<ForecastPeriod> ForecastPeriods { get; set; } = new List<ForecastPeriod>();
    public decimal OverallAccuracy { get; set; }
    public string ModelUsed { get; set; }
    public List<ConfidenceInterval> ConfidenceIntervals { get; set; } = new List<ConfidenceInterval>();
}

public class ForecastPeriod
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal ForecastedValue { get; set; }
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public string VarianceExplanation { get; set; }
}

public class ConfidenceInterval
{
    public string Level { get; set; } // "80%", "90%", "95%"
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
}

public class DriverBasedBudgetRequest
{
    public Guid CompanyId { get; set; }
    public Guid BudgetId { get; set; }
    public List<DriverBasedAllocation> Allocations { get; set; } = new List<DriverBasedAllocation>();
    public DateTime BudgetYear { get; set; }
    public string CalculationMethod { get; set; } // "ActivityBased", "VolumeBased", "EfficiencyBased"
}

public class DriverBasedAllocation
{
    public Guid AccountId { get; set; }
    public string DriverName { get; set; }
    public decimal DriverValue { get; set; }
    public decimal RatePerUnit { get; set; }
    public string Formula { get; set; }
}

public class DriverBasedBudgetResult
{
    public Guid BudgetId { get; set; }
    public bool IsCalculated { get; set; }
    public List<DriverBasedAllocationResult> AllocationResults { get; set; } = new List<DriverBasedAllocationResult>();
    public string CalculationMethod { get; set; }
    public DateTime CalculatedDate { get; set; }
}

public class DriverBasedAllocationResult
{
    public Guid AccountId { get; set; }
    public string DriverName { get; set; }
    public decimal DriverValue { get; set; }
    public decimal RatePerUnit { get; set; }
    public decimal CalculatedAmount { get; set; }
    public string VarianceExplanation { get; set; }
}

public class VarianceAnalysisRequest
{
    public Guid CompanyId { get; set; }
    public Guid BudgetId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid> AccountIds { get; set; } = new List<Guid>();
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
    public decimal VarianceThreshold { get; set; } // Percentage threshold for highlighting variances
    public bool IncludeDrillDown { get; set; }
    public string VarianceType { get; set; } // "Favorable", "Unfavorable", "Both"
}

public class VarianceAnalysisResult
{
    public Guid RequestId { get; set; }
    public DateTime AnalysisDate { get; set; }
    public List<VarianceLine> VarianceLines { get; set; } = new List<VarianceLine>();
    public decimal TotalBudgetedAmount { get; set; }
    public decimal TotalActualAmount { get; set; }
    public decimal TotalVariance { get; set; }
    public decimal TotalVariancePercentage { get; set; }
    public List<VarianceDrillDown> DrillDownDetails { get; set; } = new List<VarianceDrillDown>();
}

public class VarianceLine
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public decimal BudgetedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercentage { get; set; }
    public string VarianceType { get; set; } // "Favorable", "Unfavorable"
    public string VarianceExplanation { get; set; }
}

public class VarianceDrillDown
{
    public Guid AccountId { get; set; }
    public Guid TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string VarianceCause { get; set; }
}

public class BudgetApprovalRequest
{
    public Guid BudgetId { get; set; }
    public Guid RequestorId { get; set; }
    public Guid CompanyId { get; set; }
    public string ApprovalLevel { get; set; } // "Department", "Division", "Corporate"
    public decimal TotalAmount { get; set; }
    public List<ApprovalStep> ApprovalSteps { get; set; } = new List<ApprovalStep>();
    public string Justification { get; set; }
}

public class ApprovalStep
{
    public int StepNumber { get; set; }
    public Guid ApproverId { get; set; }
    public string Role { get; set; }
    public decimal ApprovalLimit { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string Status { get; set; } // "Pending", "Approved", "Rejected", "Escalated"
    public string Comments { get; set; }
}

public class BudgetApprovalResult
{
    public Guid BudgetId { get; set; }
    public string OverallStatus { get; set; } // "Approved", "Rejected", "Pending", "Modified"
    public List<ApprovalStep> ApprovalSteps { get; set; } = new List<ApprovalStep>();
    public DateTime ProcessedDate { get; set; }
    public Guid ProcessedBy { get; set; }
}

public class ScenarioModelingRequest
{
    public Guid CompanyId { get; set; }
    public string ScenarioName { get; set; }
    public string ScenarioType { get; set; } // "BestCase", "WorstCase", "MostLikely", "Custom"
    public List<ScenarioAssumption> Assumptions { get; set; } = new List<ScenarioAssumption>();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid> AccountIds { get; set; } = new List<Guid>();
}

public class ScenarioAssumption
{
    public string Factor { get; set; }
    public decimal Value { get; set; }
    public string Unit { get; set; }
    public string ImpactFormula { get; set; }
    public decimal Sensitivity { get; set; } // How sensitive the outcome is to this factor
}

public class ScenarioModelingResult
{
    public Guid ScenarioId { get; set; }
    public string ScenarioName { get; set; }
    public List<ScenarioOutcome> Outcomes { get; set; } = new List<ScenarioOutcome>();
    public List<SensitivityAnalysis> SensitivityAnalyses { get; set; } = new List<SensitivityAnalysis>();
    public DateTime GeneratedDate { get; set; }
}

public class ScenarioOutcome
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; }
    public decimal BestCaseValue { get; set; }
    public decimal WorstCaseValue { get; set; }
    public decimal MostLikelyValue { get; set; }
    public decimal Probability { get; set; }
}

public class SensitivityAnalysis
{
    public string Factor { get; set; }
    public decimal SensitivityCoefficient { get; set; }
    public string ImpactLevel { get; set; } // "High", "Medium", "Low"
    public decimal MinImpact { get; set; }
    public decimal MaxImpact { get; set; }
}

public class BudgetAllocationRequest
{
    public Guid CompanyId { get; set; }
    public Guid BudgetId { get; set; }
    public string AllocationMethod { get; set; } // "Proportional", "PriorityBased", "Historical", "ZeroBased"
    public List<AllocationRule> Rules { get; set; } = new List<AllocationRule>();
    public decimal TotalBudgetAmount { get; set; }
    public List<AllocationTarget> Targets { get; set; } = new List<AllocationTarget>();
}

public class AllocationRule
{
    public string RuleName { get; set; }
    public string Condition { get; set; }
    public decimal Percentage { get; set; }
    public string Formula { get; set; }
}

public class AllocationTarget
{
    public Guid TargetId { get; set; } // Could be AccountId, DepartmentId, etc.
    public string TargetType { get; set; } // "Account", "Department", "Project", "CostCenter"
    public decimal AllocatedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class BudgetAllocationResult
{
    public Guid BudgetId { get; set; }
    public string AllocationMethod { get; set; }
    public List<AllocationTarget> Allocations { get; set; } = new List<AllocationTarget>();
    public decimal TotalAllocated { get; set; }
    public decimal TotalRemaining { get; set; }
    public DateTime ProcessedDate { get; set; }
}

public class ZeroBasedBudgetRequest
{
    public Guid CompanyId { get; set; }
    public Guid BudgetId { get; set; }
    public DateTime BudgetYear { get; set; }
    public List<ZbbActivity> Activities { get; set; } = new List<ZbbActivity>();
    public List<ZbbDecisionPackage> DecisionPackages { get; set; } = new List<ZbbDecisionPackage>();
}

public class ZbbActivity
{
    public Guid ActivityId { get; set; }
    public string ActivityName { get; set; }
    public string Description { get; set; }
    public List<ZbbResource> Resources { get; set; } = new List<ZbbResource>();
    public decimal TotalCost { get; set; }
}

public class ZbbResource
{
    public string ResourceName { get; set; }
    public string ResourceType { get; set; } // "Labor", "Material", "Overhead"
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Justification { get; set; }
}

public class ZbbDecisionPackage
{
    public Guid PackageId { get; set; }
    public string PackageName { get; set; }
    public List<ZbbAlternative> Alternatives { get; set; } = new List<ZbbAlternative>();
    public string Priority { get; set; } // "MustHave", "ShouldHave", "NiceToHave"
}

public class ZbbAlternative
{
    public string AlternativeName { get; set; }
    public decimal Cost { get; set; }
    public string Benefit { get; set; }
    public string Rationale { get; set; }
}

public class ZeroBasedBudgetResult
{
    public Guid BudgetId { get; set; }
    public List<ZbbActivity> Activities { get; set; } = new List<ZbbActivity>();
    public List<ZbbDecisionPackage> DecisionPackages { get; set; } = new List<ZbbDecisionPackage>();
    public decimal TotalBudgetedAmount { get; set; }
    public DateTime ProcessedDate { get; set; }
}
