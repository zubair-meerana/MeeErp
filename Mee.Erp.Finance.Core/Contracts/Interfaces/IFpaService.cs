using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for Financial Planning & Analysis (FP&A)
/// </summary>
public interface IFpaService
{
    /// <summary>
    /// Creates driver-based planning models with complex formulas
    /// </summary>
    Task<DriverBasedPlanningResult> CreateDriverBasedPlanningModelAsync(DriverBasedPlanningRequest request);

    /// <summary>
    /// Performs sensitivity analysis and scenario modeling
    /// </summary>
    Task<SensitivityAnalysisResult> PerformSensitivityAnalysisAsync(SensitivityAnalysisRequest request);

    /// <summary>
    /// Automates variance analysis and commentary
    /// </summary>
    Task<VarianceAnalysisResult> AutomateVarianceAnalysisAsync(VarianceAnalysisRequest request);

    /// <summary>
    /// Tracks and analyzes forecast accuracy
    /// </summary>
    Task<ForecastAccuracyResult> TrackForecastAccuracyAsync(ForecastAccuracyRequest request);

    /// <summary>
    /// Creates rolling forecast methodologies
    /// </summary>
    Task<RollingForecastResult> CreateRollingForecastAsync(RollingForecastRequest request);

    /// <summary>
    /// Generates management commentary
    /// </summary>
    Task<ManagementCommentaryResult> GenerateManagementCommentaryAsync(ManagementCommentaryRequest request);

    /// <summary>
    /// Creates executive dashboards with KPIs
    /// </summary>
    Task<ExecutiveDashboardResult> CreateExecutiveDashboardAsync(ExecutiveDashboardRequest request);

    /// <summary>
    /// Performs financial ratio analysis
    /// </summary>
    Task<FinancialRatioAnalysisResult> PerformFinancialRatioAnalysisAsync(FinancialRatioAnalysisRequest request);
}

public class DriverBasedPlanningRequest
{
    public Guid CompanyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PlanningHorizon { get; set; } // "Monthly", "Quarterly", "Annually"
    public List<PlanningDriver> Drivers { get; set; } = new List<PlanningDriver>();
    public List<PlanningConstraint> Constraints { get; set; } = new List<PlanningConstraint>();
    public string PlanningMethod { get; set; } // "TopDown", "BottomUp", "DriverBased"
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class PlanningDriver
{
    public string Name { get; set; }
    public string Type { get; set; } // "Economic", "Operational", "Market", "Internal"
    public decimal Weight { get; set; }
    public string Formula { get; set; }
    public List<DriverValue> Values { get; set; } = new List<DriverValue>();
}

public class DriverValue
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string DataSource { get; set; }
}

public class PlanningConstraint
{
    public string ConstraintType { get; set; } // "Resource", "Capacity", "Regulatory", "Policy"
    public string Description { get; set; }
    public decimal Limit { get; set; }
    public string Formula { get; set; }
}

public class DriverBasedPlanningResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<PlanningOutput> Outputs { get; set; } = new List<PlanningOutput>();
    public List<PlanningDriver> AppliedDrivers { get; set; } = new List<PlanningDriver>();
    public List<PlanningConstraint> AppliedConstraints { get; set; } = new List<PlanningConstraint>();
    public string ModelAccuracy { get; set; }
    public List<PlanningAssumption> Assumptions { get; set; } = new List<PlanningAssumption>();
}

public class PlanningOutput
{
    public Guid OutputId { get; set; }
    public string OutputName { get; set; }
    public string OutputType { get; set; } // "Revenue", "Expense", "Headcount", "Production"
    public List<OutputValue> Values { get; set; } = new List<OutputValue>();
    public string VarianceExplanation { get; set; }
}

public class OutputValue
{
    public DateTime Period { get; set; }
    public decimal Value { get; set; }
    public string VarianceType { get; set; } // "Favorable", "Unfavorable"
    public decimal Variance { get; set; }
}

public class PlanningAssumption
{
    public string AssumptionName { get; set; }
    public string Description { get; set; }
    public string Value { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string ConfidenceLevel { get; set; } // "High", "Medium", "Low"
}

public class SensitivityAnalysisRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<SensitivityParameter> Parameters { get; set; } = new List<SensitivityParameter>();
    public List<Guid> OutputIds { get; set; } = new List<Guid>();
    public decimal ChangePercentage { get; set; } // Percentage change to test
}

public class SensitivityParameter
{
    public string ParameterName { get; set; }
    public string ParameterType { get; set; } // "Economic", "Operational", "Market"
    public decimal BaseValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal SensitivityCoefficient { get; set; }
}

public class SensitivityAnalysisResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<SensitivityOutput> Outputs { get; set; } = new List<SensitivityOutput>();
    public List<SensitivityMatrix> SensitivityMatrices { get; set; } = new List<SensitivityMatrix>();
    public List<ScenarioResult> ScenarioResults { get; set; } = new List<ScenarioResult>();
}

public class SensitivityOutput
{
    public Guid OutputId { get; set; }
    public string OutputName { get; set; }
    public decimal BaseValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal SensitivityCoefficient { get; set; }
    public string ImpactLevel { get; set; } // "High", "Medium", "Low"
}

public class SensitivityMatrix
{
    public string RowParameter { get; set; }
    public string ColumnParameter { get; set; }
    public decimal CorrelationCoefficient { get; set; }
}

public class ScenarioResult
{
    public string ScenarioName { get; set; }
    public string ScenarioType { get; set; } // "BestCase", "WorstCase", "BaseCase"
    public List<ScenarioOutput> Outputs { get; set; } = new List<ScenarioOutput>();
}

public class ScenarioOutput
{
    public Guid OutputId { get; set; }
    public string OutputName { get; set; }
    public decimal Value { get; set; }
    public decimal Probability { get; set; }
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
    public bool IncludeCommentary { get; set; }
    public string VarianceType { get; set; } // "Favorable", "Unfavorable", "Both"
}

public class VarianceAnalysisResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<VarianceLine> VarianceLines { get; set; } = new List<VarianceLine>();
    public decimal TotalBudgetedAmount { get; set; }
    public decimal TotalActualAmount { get; set; }
    public decimal TotalVariance { get; set; }
    public decimal TotalVariancePercentage { get; set; }
    public List<VarianceDrillDown> DrillDownDetails { get; set; } = new List<VarianceDrillDown>();
    public string Commentary { get; set; }
    public List<VarianceInvestigation> Investigations { get; set; } = new List<VarianceInvestigation>();
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

public class VarianceInvestigation
{
    public Guid InvestigationId { get; set; }
    public Guid AccountId { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercentage { get; set; }
    public string InvestigationStatus { get; set; } // "Open", "InReview", "Completed"
    public string InvestigatorNotes { get; set; }
    public string RootCause { get; set; }
    public string CorrectiveAction { get; set; }
    public DateTime? InvestigationDate { get; set; }
    public DateTime? ResolutionDate { get; set; }
    public Guid? InvestigatorUserId { get; set; }
}

public class ForecastAccuracyRequest
{
    public Guid CompanyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid> ForecastIds { get; set; } = new List<Guid>();
    public string AccuracyMetric { get; set; } // "MAPE", "RMSE", "MAE"
    public List<Guid> AccountIds { get; set; } = new List<Guid>();
}

public class ForecastAccuracyResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<ForecastAccuracyLine> AccuracyLines { get; set; } = new List<ForecastAccuracyLine>();
    public decimal OverallAccuracy { get; set; }
    public decimal MeanAbsolutePercentageError { get; set; }
    public decimal RootMeanSquareError { get; set; }
    public decimal MeanAbsoluteError { get; set; }
    public List<AccuracyImprovementRecommendation> Recommendations { get; set; } = new List<AccuracyImprovementRecommendation>();
}

public class ForecastAccuracyLine
{
    public Guid ForecastId { get; set; }
    public string ForecastName { get; set; }
    public decimal ActualValue { get; set; }
    public decimal ForecastValue { get; set; }
    public decimal AbsoluteError { get; set; }
    public decimal PercentageError { get; set; }
    public DateTime Period { get; set; }
}

public class AccuracyImprovementRecommendation
{
    public string RecommendationType { get; set; } // "Model", "Data", "Process"
    public string Description { get; set; }
    public string ExpectedImprovement { get; set; }
    public DateTime RecommendedDate { get; set; }
}

public class RollingForecastRequest
{
    public Guid CompanyId { get; set; }
    public DateTime StartDate { get; set; }
    public int ForecastHorizonMonths { get; set; }
    public string ForecastMethod { get; set; } // "TimeSeries", "Regression", "MachineLearning"
    public List<ForecastDriver> Drivers { get; set; } = new List<ForecastDriver>();
    public bool IncludePredictiveAnalytics { get; set; }
    public string ConfidenceInterval { get; set; } // "80%", "90%", "95%"
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
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

public class ManagementCommentaryRequest
{
    public Guid CompanyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<CommentaryTopic> Topics { get; set; } = new List<CommentaryTopic>();
    public string CommentaryType { get; set; } // "Quarterly", "Annual", "Monthly"
    public List<Guid> AccountIds { get; set; } = new List<Guid>();
}

public class CommentaryTopic
{
    public string TopicName { get; set; }
    public string TopicType { get; set; } // "Revenue", "Expense", "Margin", "CashFlow"
    public string Analysis { get; set; }
    public string KeyPoints { get; set; }
    public string Outlook { get; set; }
}

public class ManagementCommentaryResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public string Commentary { get; set; }
    public List<CommentarySection> Sections { get; set; } = new List<CommentarySection>();
    public List<KeyMetric> KeyMetrics { get; set; } = new List<KeyMetric>();
    public List<OutlookItem> OutlookItems { get; set; } = new List<OutlookItem>();
}

public class CommentarySection
{
    public string SectionTitle { get; set; }
    public string Content { get; set; }
    public string AnalysisType { get; set; } // "Variance", "Trend", "Comparison"
}

public class KeyMetric
{
    public string MetricName { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal Variance { get; set; }
    public string Trend { get; set; } // "Increasing", "Decreasing", "Stable"
}

public class OutlookItem
{
    public string ItemName { get; set; }
    public string Description { get; set; }
    public string Probability { get; set; } // "High", "Medium", "Low"
    public DateTime ExpectedDate { get; set; }
}

public class ExecutiveDashboardRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<KpiDefinition> Kpis { get; set; } = new List<KpiDefinition>();
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
    public string DashboardType { get; set; } // "RealTime", "Daily", "Weekly", "Monthly"
}

public class KpiDefinition
{
    public string KpiName { get; set; }
    public string KpiType { get; set; } // "Financial", "Operational", "Strategic"
    public string Formula { get; set; }
    public string TargetValue { get; set; }
    public string ThresholdValues { get; set; } // "Green", "Yellow", "Red" thresholds
    public string TrendDirection { get; set; } // "Positive", "Negative", "Neutral"
}

public class ExecutiveDashboardResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<KpiResult> KpiResults { get; set; } = new List<KpiResult>();
    public List<DashboardWidget> Widgets { get; set; } = new List<DashboardWidget>();
    public List<Alert> Alerts { get; set; } = new List<Alert>();
    public string DashboardUrl { get; set; }
}

public class KpiResult
{
    public string KpiName { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal Variance { get; set; }
    public string Status { get; set; } // "Green", "Yellow", "Red"
    public string Trend { get; set; } // "Up", "Down", "Flat"
    public DateTime LastUpdated { get; set; }
}

public class DashboardWidget
{
    public string WidgetName { get; set; }
    public string WidgetType { get; set; } // "Chart", "Table", "Gauge", "Kpi"
    public string Data { get; set; }
    public string Configuration { get; set; }
}

public class Alert
{
    public string AlertType { get; set; } // "Threshold", "Variance", "Trend"
    public string Description { get; set; }
    public DateTime AlertDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
    public string ActionRequired { get; set; }
}

public class FinancialRatioAnalysisRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<RatioCategory> RatioCategories { get; set; } = new List<RatioCategory>();
    public List<Guid> AccountIds { get; set; } = new List<Guid>();
    public string ComparisonPeriod { get; set; } // "PreviousPeriod", "PreviousYear", "Industry"
}

public class RatioCategory
{
    public string CategoryName { get; set; } // "Liquidity", "Profitability", "Efficiency", "Leverage"
    public List<RatioDefinition> Ratios { get; set; } = new List<RatioDefinition>();
}

public class RatioDefinition
{
    public string RatioName { get; set; }
    public string Formula { get; set; }
    public string Description { get; set; }
    public string IndustryBenchmark { get; set; }
    public string Interpretation { get; set; }
}

public class FinancialRatioAnalysisResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<RatioResult> RatioResults { get; set; } = new List<RatioResult>();
    public List<RatioTrend> RatioTrends { get; set; } = new List<RatioTrend>();
    public List<RatioComparison> Comparisons { get; set; } = new List<RatioComparison>();
    public List<RatioInsight> Insights { get; set; } = new List<RatioInsight>();
}

public class RatioResult
{
    public string RatioName { get; set; }
    public decimal Value { get; set; }
    public string Category { get; set; }
    public string Status { get; set; } // "Strong", "Average", "Weak"
    public string Interpretation { get; set; }
    public DateTime AsOfDate { get; set; }
}

public class RatioTrend
{
    public string RatioName { get; set; }
    public List<RatioValuePoint> Values { get; set; } = new List<RatioValuePoint>();
    public string TrendDirection { get; set; } // "Improving", "Deteriorating", "Stable"
}

public class RatioValuePoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}

public class RatioComparison
{
    public string RatioName { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ComparisonValue { get; set; }
    public decimal Variance { get; set; }
    public string ComparisonType { get; set; } // "PreviousPeriod", "PreviousYear", "Industry"
}

public class RatioInsight
{
    public string InsightType { get; set; } // "Strength", "Weakness", "Opportunity", "Threat"
    public string Description { get; set; }
    public string Recommendation { get; set; }
    public string ImpactLevel { get; set; } // "High", "Medium", "Low"
}}
