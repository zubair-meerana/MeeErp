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
/// Implements Financial Planning & Analysis (FP&A)
/// </summary>
public class FpaService : IFpaService
{
    private readonly FinanceDbContext _context;

    public FpaService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<DriverBasedPlanningResult> CreateDriverBasedPlanningModelAsync(DriverBasedPlanningRequest request)
    {
        var result = new DriverBasedPlanningResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Outputs = new List<PlanningOutput>(),
            AppliedDrivers = request.Drivers,
            AppliedConstraints = request.Constraints,
            Assumptions = new List<PlanningAssumption>()
        };

        // Process each driver to generate planning outputs
        foreach (var driver in request.Drivers)
        {
            var output = new PlanningOutput
            {
                OutputId = Guid.NewGuid(),
                OutputName = driver.Name,
                OutputType = DetermineOutputType(driver.Type),
                Values = new List<OutputValue>()
            };

            // Generate values based on driver formula and historical data
            var period = request.StartDate;
            while (period <= request.EndDate)
            {
                var value = CalculateDriverValue(driver, period);
                output.Values.Add(new OutputValue
                {
                    Period = period,
                    Value = value,
                    VarianceType = value >= 0 ? "Favorable" : "Unfavorable",
                    Variance = value // For this example, variance is the value itself
                });

                // Move to next period based on planning horizon
                period = request.PlanningHorizon.ToLower() switch
                {
                    "monthly" => period.AddMonths(1),
                    "quarterly" => period.AddMonths(3),
                    "annually" => period.AddYears(1),
                    _ => period.AddMonths(1) // Default to monthly
                };
            }

            result.Outputs.Add(output);
        }

        // Add planning assumptions
        result.Assumptions.Add(new PlanningAssumption
        {
            AssumptionName = "Economic Growth Rate",
            Description = "Assumed economic growth rate for planning period",
            Value = "3.5%",
            EffectiveDate = request.StartDate,
            ConfidenceLevel = "High"
        });

        result.Assumptions.Add(new PlanningAssumption
        {
            AssumptionName = "Inflation Rate",
            Description = "Assumed inflation rate for planning period",
            Value = "2.0%",
            EffectiveDate = request.StartDate,
            ConfidenceLevel = "Medium"
        });

        return result;
    }

    public async Task<SensitivityAnalysisResult> PerformSensitivityAnalysisAsync(SensitivityAnalysisRequest request)
    {
        var result = new SensitivityAnalysisResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Outputs = new List<SensitivityOutput>(),
            SensitivityMatrices = new List<SensitivityMatrix>(),
            ScenarioResults = new List<ScenarioResult>()
        };

        // Process each parameter to determine sensitivity
        foreach (var parameter in request.Parameters)
        {
            var output = new SensitivityOutput
            {
                OutputId = Guid.NewGuid(),
                OutputName = parameter.ParameterName,
                BaseValue = parameter.BaseValue,
                MinValue = parameter.MinValue,
                MaxValue = parameter.MaxValue,
                SensitivityCoefficient = parameter.SensitivityCoefficient,
                ImpactLevel = CalculateImpactLevel(Math.Abs(parameter.SensitivityCoefficient))
            };

            result.Outputs.Add(output);
        }

        // Create sensitivity matrices
        for (int i = 0; i < request.Parameters.Count; i++)
        {
            for (int j = i + 1; j < request.Parameters.Count; j++)
            {
                var matrix = new SensitivityMatrix
                {
                    RowParameter = request.Parameters[i].ParameterName,
                    ColumnParameter = request.Parameters[j].ParameterName,
                    CorrelationCoefficient = CalculateCorrelation(request.Parameters[i], request.Parameters[j])
                };

                result.SensitivityMatrices.Add(matrix);
            }
        }

        // Generate scenario results
        var bestCaseScenario = new ScenarioResult
        {
            ScenarioName = "Best Case",
            ScenarioType = "BestCase",
            Outputs = GenerateScenarioOutputs(request.OutputIds, request.ChangePercentage * 1.2m)
        };

        var baseCaseScenario = new ScenarioResult
        {
            ScenarioName = "Base Case",
            ScenarioType = "BaseCase",
            Outputs = GenerateScenarioOutputs(request.OutputIds, 0)
        };

        var worstCaseScenario = new ScenarioResult
        {
            ScenarioName = "Worst Case",
            ScenarioType = "WorstCase",
            Outputs = GenerateScenarioOutputs(request.OutputIds, request.ChangePercentage * -1.2m)
        };

        result.ScenarioResults.Add(bestCaseScenario);
        result.ScenarioResults.Add(baseCaseScenario);
        result.ScenarioResults.Add(worstCaseScenario);

        return result;
    }

    public async Task<VarianceAnalysisResult> AutomateVarianceAnalysisAsync(VarianceAnalysisRequest request)
    {
        var result = new VarianceAnalysisResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            VarianceLines = new List<VarianceLine>(),
            DrillDownDetails = new List<VarianceDrillDown>(),
            Investigations = new List<VarianceInvestigation>()
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
            .Where(le => request.AccountIds.Contains(le.AccountId) &&
                        le.EntryDate >= request.StartDate &&
                        le.EntryDate <= request.EndDate)
            .GroupBy(le => le.AccountId)
            .Select(g => new { AccountId = g.Key, TotalAmount = g.Sum(le => le.Debit - le.Credit) })
            .ToListAsync();

        // Calculate variances
        foreach (var budgetLine in budget.Lines)
        {
            var actualAmount = actualData.FirstOrDefault(ad => ad.AccountId == budgetLine.AccountId)?.TotalAmount ?? 0;
            var variance = actualAmount - budgetLine.BudgetAmount;
            var variancePercentage = budgetLine.BudgetAmount != 0 ? (variance / budgetLine.BudgetAmount) * 100 : 0;

            // Only include if variance exceeds threshold
            if (Math.Abs(variancePercentage) >= request.VarianceThreshold)
            {
                var account = await _context.Accounts.FindAsync(budgetLine.AccountId);

                var varianceLine = new VarianceLine
                {
                    AccountId = budgetLine.AccountId,
                    AccountNumber = account?.AccountNumber ?? "",
                    AccountName = account?.Name ?? "",
                    BudgetedAmount = budgetLine.BudgetAmount,
                    ActualAmount = actualAmount,
                    Variance = variance,
                    VariancePercentage = variancePercentage,
                    VarianceType = variance >= 0 ? "Favorable" : "Unfavorable",
                    VarianceExplanation = GenerateVarianceExplanation(variance, variancePercentage, account?.Name)
                };

                result.VarianceLines.Add(varianceLine);

                // Add drill-down details
                var ledgerEntries = await _context.LedgerEntries
                    .Where(le => le.AccountId == budgetLine.AccountId &&
                                le.EntryDate >= request.StartDate &&
                                le.EntryDate <= request.EndDate)
                    .ToListAsync();

                foreach (var entry in ledgerEntries)
                {
                    result.DrillDownDetails.Add(new VarianceDrillDown
                    {
                        AccountId = budgetLine.AccountId,
                        TransactionId = entry.Id,
                        TransactionDate = entry.EntryDate,
                        Description = entry.Description ?? "",
                        Amount = entry.Debit - entry.Credit,
                        VarianceCause = "Detailed transaction entry"
                    });
                }

                // Auto-create investigation for significant variances
                if (Math.Abs(variancePercentage) > request.VarianceThreshold * 2) // Double threshold
                {
                    result.Investigations.Add(new VarianceInvestigation
                    {
                        InvestigationId = Guid.NewGuid(),
                        AccountId = budgetLine.AccountId,
                        VarianceAmount = variance,
                        VariancePercentage = variancePercentage,
                        InvestigationStatus = "Open",
                        InvestigatorNotes = $"Auto-generated investigation for variance of {variancePercentage:F2}%",
                        RootCause = "Pending investigation",
                        CorrectiveAction = "Pending investigation",
                        InvestigationDate = DateTime.UtcNow
                    });
                }
            }
        }

        // Calculate totals
        result.TotalBudgetedAmount = result.VarianceLines.Sum(vl => vl.BudgetedAmount);
        result.TotalActualAmount = result.VarianceLines.Sum(vl => vl.ActualAmount);
        result.TotalVariance = result.TotalActualAmount - result.TotalBudgetedAmount;
        result.TotalVariancePercentage = result.TotalBudgetedAmount != 0 ?
            (result.TotalVariance / result.TotalBudgetedAmount) * 100 : 0;

        // Generate commentary if requested
        if (request.IncludeCommentary)
        {
            result.Commentary = GenerateVarianceCommentary(result);
        }

        return result;
    }

    public async Task<ForecastAccuracyResult> TrackForecastAccuracyAsync(ForecastAccuracyRequest request)
    {
        var result = new ForecastAccuracyResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            AccuracyLines = new List<ForecastAccuracyLine>(),
            Recommendations = new List<AccuracyImprovementRecommendation>()
        };

        // Get forecast and actual data for accuracy calculation
        foreach (var forecastId in request.ForecastIds)
        {
            // In a real implementation, this would fetch forecast vs actual data
            // For this example, we'll use placeholder values
            var accuracyLine = new ForecastAccuracyLine
            {
                ForecastId = forecastId,
                ForecastName = $"Forecast {forecastId}",
                ActualValue = 100000, // Placeholder
                ForecastValue = 95000, // Placeholder
                AbsoluteError = 5000,
                PercentageError = 5.0m,
                Period = DateTime.Today
            };

            result.AccuracyLines.Add(accuracyLine);
        }

        // Calculate overall accuracy metrics
        var totalActual = result.AccuracyLines.Sum(al => al.ActualValue);
        var totalAbsoluteError = result.AccuracyLines.Sum(al => al.AbsoluteError);
        var totalPercentageError = result.AccuracyLines.Average(al => al.PercentageError);

        result.OverallAccuracy = totalActual != 0 ? (1 - (totalAbsoluteError / totalActual)) * 100 : 0;
        result.MeanAbsolutePercentageError = totalPercentageError;
        result.MeanAbsoluteError = totalAbsoluteError / result.AccuracyLines.Count;

        // Calculate RMSE
        var squaredErrors = result.AccuracyLines.Select(al => al.AbsoluteError * al.AbsoluteError);
        result.RootMeanSquareError = (decimal)Math.Sqrt((double)squaredErrors.Average());

        // Generate recommendations
        if (result.MeanAbsolutePercentageError > 10) // If MAPE > 10%
        {
            result.Recommendations.Add(new AccuracyImprovementRecommendation
            {
                RecommendationType = "Model",
                Description = "Current forecasting model has high error rate. Consider using more sophisticated model.",
                ExpectedImprovement = "Reduce MAPE by 20-30%",
                RecommendedDate = DateTime.Today
            });
        }

        if (result.AccuracyLines.Any(al => al.PercentageError > 20)) // If any forecast has >20% error
        {
            result.Recommendations.Add(new AccuracyImprovementRecommendation
            {
                RecommendationType = "Data",
                Description = "High error in specific forecasts. Review data quality and assumptions.",
                ExpectedImprovement = "Reduce individual forecast errors",
                RecommendedDate = DateTime.Today
            });
        }

        return result;
    }

    public async Task<RollingForecastResult> CreateRollingForecastAsync(RollingForecastRequest request)
    {
        var result = new RollingForecastResult
        {
            RequestId = Guid.NewGuid(),
            GeneratedDate = DateTime.UtcNow,
            ForecastPeriods = new List<ForecastPeriod>(),
            ConfidenceIntervals = new List<ConfidenceInterval>()
        };

        // Generate rolling forecast periods
        var periodStart = request.StartDate;
        for (int i = 0; i < request.ForecastHorizonMonths; i++)
        {
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            // Calculate forecasted value based on drivers and historical data
            var forecastedValue = CalculateRollingForecastValue(request.Drivers, periodStart);

            // Calculate confidence intervals
            var lowerBound = forecastedValue * 0.9m; // 10% lower bound
            var upperBound = forecastedValue * 1.1m; // 10% upper bound

            var forecastPeriod = new ForecastPeriod
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ForecastedValue = forecastedValue,
                LowerBound = lowerBound,
                UpperBound = upperBound,
                VarianceExplanation = "Based on historical trends and market drivers"
            };

            result.ForecastPeriods.Add(forecastPeriod);

            // Move to next period
            periodStart = periodStart.AddMonths(1);
        }

        // Add confidence intervals
        result.ConfidenceIntervals.Add(new ConfidenceInterval
        {
            Level = request.ConfidenceInterval,
            LowerBound = result.ForecastPeriods.Min(fp => fp.LowerBound),
            UpperBound = result.ForecastPeriods.Max(fp => fp.UpperBound)
        });

        // Set model used based on request
        result.ModelUsed = request.ForecastMethod;

        return result;
    }

    public async Task<ManagementCommentaryResult> GenerateManagementCommentaryAsync(ManagementCommentaryRequest request)
    {
        var result = new ManagementCommentaryResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Sections = new List<CommentarySection>(),
            KeyMetrics = new List<KeyMetric>(),
            OutlookItems = new List<OutlookItem>()
        };

        // Generate commentary sections
        result.Sections.Add(new CommentarySection
        {
            SectionTitle = "Executive Summary",
            Content = GenerateExecutiveSummary(request),
            AnalysisType = "Overview"
        });

        result.Sections.Add(new CommentarySection
        {
            SectionTitle = "Revenue Analysis",
            Content = GenerateRevenueAnalysis(request),
            AnalysisType = "Revenue"
        });

        result.Sections.Add(new CommentarySection
        {
            SectionTitle = "Expense Analysis",
            Content = GenerateExpenseAnalysis(request),
            AnalysisType = "Expense"
        });

        result.Sections.Add(new CommentarySection
        {
            SectionTitle = "Profitability Analysis",
            Content = GenerateProfitabilityAnalysis(request),
            AnalysisType = "Margin"
        });

        // Generate key metrics
        result.KeyMetrics.Add(new KeyMetric
        {
            MetricName = "Revenue Growth",
            CurrentValue = 1050000,
            PreviousValue = 1000000,
            Variance = 50000,
            Trend = "Increasing"
        });

        result.KeyMetrics.Add(new KeyMetric
        {
            MetricName = "Net Profit Margin",
            CurrentValue = 15.5m,
            PreviousValue = 14.2m,
            Variance = 1.3m,
            Trend = "Increasing"
        });

        result.KeyMetrics.Add(new KeyMetric
        {
            MetricName = "Operating Expenses",
            CurrentValue = 300000,
            PreviousValue = 290000,
            Variance = 10000,
            Trend = "Increasing"
        });

        // Generate outlook items
        result.OutlookItems.Add(new OutlookItem
        {
            ItemName = "New Product Launch",
            Description = "Expected to contribute $200k in revenue next quarter",
            Probability = "High",
            ExpectedDate = DateTime.Today.AddMonths(3)
        });

        result.OutlookItems.Add(new OutlookItem
        {
            ItemName = "Market Expansion",
            Description = "Potential to increase market share by 5%",
            Probability = "Medium",
            ExpectedDate = DateTime.Today.AddMonths(6)
        });

        // Generate overall commentary
        result.Commentary = string.Join("\n\n", result.Sections.Select(s => $"{s.SectionTitle}: {s.Content}"));

        return result;
    }

    public async Task<ExecutiveDashboardResult> CreateExecutiveDashboardAsync(ExecutiveDashboardRequest request)
    {
        var result = new ExecutiveDashboardResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            KpiResults = new List<KpiResult>(),
            Widgets = new List<DashboardWidget>(),
            Alerts = new List<Alert>()
        };

        // Calculate KPIs
        foreach (var kpi in request.Kpis)
        {
            var kpiResult = new KpiResult
            {
                KpiName = kpi.KpiName,
                CurrentValue = CalculateKpiValue(kpi.Formula, request.AsOfDate),
                TargetValue = ParseTargetValue(kpi.TargetValue),
                Variance = 0,
                Status = "Green", // Will be determined below
                Trend = "Up", // Will be determined below
                LastUpdated = DateTime.UtcNow
            };

            kpiResult.Variance = kpiResult.CurrentValue - kpiResult.TargetValue;

            // Determine status based on thresholds
            var thresholdValues = ParseThresholdValues(kpi.ThresholdValues);
            kpiResult.Status = DetermineKpiStatus(kpiResult.CurrentValue, kpiResult.TargetValue, thresholdValues);

            // Determine trend
            kpiResult.Trend = DetermineKpiTrend(kpiResult.KpiName, request.AsOfDate);

            result.KpiResults.Add(kpiResult);

            // Generate alerts for critical KPIs
            if (kpiResult.Status == "Red")
            {
                result.Alerts.Add(new Alert
                {
                    AlertType = "Threshold",
                    Description = $"KPI '{kpi.KpiName}' is below threshold: {kpiResult.CurrentValue} vs target {kpiResult.TargetValue}",
                    AlertDate = DateTime.UtcNow,
                    Severity = "High",
                    ActionRequired = "Immediate management attention required"
                });
            }
        }

        // Add dashboard widgets
        result.Widgets.Add(new DashboardWidget
        {
            WidgetName = "Revenue Trend",
            WidgetType = "Chart",
            Data = "Revenue trend data",
            Configuration = "Line chart configuration"
        });

        result.Widgets.Add(new DashboardWidget
        {
            WidgetName = "Profit Margin",
            WidgetType = "Gauge",
            Data = "Profit margin data",
            Configuration = "Gauge configuration"
        });

        result.Widgets.Add(new DashboardWidget
        {
            WidgetName = "Key Metrics Table",
            WidgetType = "Table",
            Data = "Key metrics data",
            Configuration = "Table configuration"
        });

        // Set dashboard URL
        result.DashboardUrl = $"/dashboard/executive/{request.CompanyId}";

        return result;
    }

    public async Task<FinancialRatioAnalysisResult> PerformFinancialRatioAnalysisAsync(FinancialRatioAnalysisRequest request)
    {
        var result = new FinancialRatioAnalysisResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            RatioResults = new List<RatioResult>(),
            RatioTrends = new List<RatioTrend>(),
            Comparisons = new List<RatioComparison>(),
            Insights = new List<RatioInsight>()
        };

        // Calculate ratios based on categories
        foreach (var category in request.RatioCategories)
        {
            foreach (var ratio in category.Ratios)
            {
                var ratioValue = CalculateRatioValue(ratio.Formula, request.CompanyId, request.AsOfDate);

                var ratioResult = new RatioResult
                {
                    RatioName = ratio.RatioName,
                    Value = ratioValue,
                    Category = category.CategoryName,
                    Status = DetermineRatioStatus(ratioValue, ratio.IndustryBenchmark),
                    Interpretation = ratio.Interpretation,
                    AsOfDate = request.AsOfDate
                };

                result.RatioResults.Add(ratioResult);

                // Add ratio trend
                var trend = new RatioTrend
                {
                    RatioName = ratio.RatioName,
                    Values = GenerateRatioTrendValues(ratio.RatioName, request.CompanyId, request.AsOfDate),
                    TrendDirection = DetermineTrendDirection(GenerateRatioTrendValues(ratio.RatioName, request.CompanyId, request.AsOfDate))
                };

                result.RatioTrends.Add(trend);

                // Add comparison
                var comparisonValue = GetComparisonValue(ratio.RatioName, request.ComparisonPeriod);
                var comparison = new RatioComparison
                {
                    RatioName = ratio.RatioName,
                    CurrentValue = ratioValue,
                    ComparisonValue = comparisonValue,
                    Variance = ratioValue - comparisonValue,
                    ComparisonType = request.ComparisonPeriod
                };

                result.Comparisons.Add(comparison);

                // Add insights based on ratio analysis
                var insights = GenerateRatioInsights(ratio.RatioName, ratioValue, comparisonValue);
                result.Insights.AddRange(insights);
            }
        }

        return result;
    }

    #region Helper Methods

    private decimal CalculateDriverValue(PlanningDriver driver, DateTime period)
    {
        // Simplified calculation - in reality, this would use complex formulas
        decimal baseValue = 10000; // Placeholder base value
        decimal growthFactor = 1.0m;

        foreach (var value in driver.Values)
        {
            if (value.Date <= period)
            {
                growthFactor += (value.Value / 100);
            }
        }

        return baseValue * growthFactor;
    }

    private string DetermineOutputType(string driverType)
    {
        return driverType.ToLower() switch
        {
            "economic" => "Revenue",
            "operational" => "Expense",
            "market" => "Revenue",
            "internal" => "Headcount",
            _ => "Revenue"
        };
    }

    private string CalculateImpactLevel(decimal sensitivityCoefficient)
    {
        if (Math.Abs(sensitivityCoefficient) > 0.8m) return "High";
        if (Math.Abs(sensitivityCoefficient) > 0.5m) return "Medium";
        return "Low";
    }

    private decimal CalculateCorrelation(SensitivityParameter param1, SensitivityParameter param2)
    {
        // Simplified correlation calculation
        return 0.5m; // Placeholder value
    }

    private List<ScenarioOutput> GenerateScenarioOutputs(List<Guid> outputIds, decimal changePercentage)
    {
        var outputs = new List<ScenarioOutput>();

        foreach (var outputId in outputIds)
        {
            outputs.Add(new ScenarioOutput
            {
                OutputId = outputId,
                OutputName = $"Output {outputId}",
                Value = 100000 * (1 + changePercentage / 100), // Apply percentage change
                Probability = 0.5m // Placeholder probability
            });
        }

        return outputs;
    }

    private string GenerateVarianceExplanation(decimal variance, decimal variancePercentage, string accountName)
    {
        if (variance > 0)
        {
            return $"{accountName} performed favorably with variance of {variance:C} ({variancePercentage:F2}%)";
        }
        else
        {
            return $"{accountName} underperformed with variance of {variance:C} ({variancePercentage:F2}%)";
        }
    }

    private string GenerateVarianceCommentary(VarianceAnalysisResult result)
    {
        var commentary = new System.Text.StringBuilder();
        commentary.AppendLine("Variance Analysis Commentary:");
        commentary.AppendLine();

        if (result.TotalVariance > 0)
        {
            commentary.AppendLine($"Overall favorable variance of {result.TotalVariance:C} ({result.TotalVariancePercentage:F2}%)");
        }
        else
        {
            commentary.AppendLine($"Overall unfavorable variance of {Math.Abs(result.TotalVariance):C} ({Math.Abs(result.TotalVariancePercentage):F2}%)");
        }

        commentary.AppendLine();
        commentary.AppendLine("Key observations:");

        var significantVarianceLines = result.VarianceLines
            .Where(vl => Math.Abs(vl.VariancePercentage) > 10)
            .OrderByDescending(vl => Math.Abs(vl.VariancePercentage));

        foreach (var line in significantVarianceLines.Take(5))
        {
            commentary.AppendLine($"- {line.AccountName}: {line.Variance:C} ({line.VariancePercentage:F2}%) variance");
        }

        return commentary.ToString();
    }

    private decimal CalculateRollingForecastValue(List<ForecastDriver> drivers, DateTime period)
    {
        // Simplified calculation - in reality, this would use complex forecasting models
        decimal baseValue = 50000; // Placeholder base value
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

    private string GenerateExecutiveSummary(ManagementCommentaryRequest request)
    {
        return $"Financial performance summary for the period {request.StartDate:MMM yyyy} to {request.EndDate:MMM yyyy}. " +
               "Overall results reflect strong operational performance with revenue growth and controlled expenses.";
    }

    private string GenerateRevenueAnalysis(ManagementCommentaryRequest request)
    {
        return "Revenue analysis shows positive growth trends driven by increased customer acquisition and improved retention rates. " +
               "Market expansion initiatives are showing early signs of success.";
    }

    private string GenerateExpenseAnalysis(ManagementCommentaryRequest request)
    {
        return "Expense management remains disciplined with operational efficiency improvements. " +
               "Cost containment measures are effectively balancing growth investments.";
    }

    private string GenerateProfitabilityAnalysis(ManagementCommentaryRequest request)
    {
        return "Profitability metrics demonstrate healthy margins with continued focus on value creation. " +
               "Efficiency gains are contributing to improved bottom-line performance.";
    }

    private decimal CalculateKpiValue(string formula, DateTime asOfDate)
    {
        // Simplified KPI calculation - in reality, this would execute complex formulas
        return 100000; // Placeholder value
    }

    private decimal ParseTargetValue(string targetValue)
    {
        if (decimal.TryParse(targetValue.Replace("%", ""), out decimal value))
        {
            return value;
        }
        return 0;
    }

    private (decimal Green, decimal Yellow, decimal Red) ParseThresholdValues(string thresholdValues)
    {
        // Parse threshold values like "Green:90,Yellow:80,Red:70"
        return (90, 80, 70); // Placeholder values
    }

    private string DetermineKpiStatus(decimal currentValue, decimal targetValue, (decimal Green, decimal Yellow, decimal Red) thresholds)
    {
        if (currentValue >= thresholds.Green) return "Green";
        if (currentValue >= thresholds.Yellow) return "Yellow";
        return "Red";
    }

    private string DetermineKpiTrend(string kpiName, DateTime asOfDate)
    {
        // Simplified trend determination
        return "Up"; // Placeholder
    }

    private decimal CalculateRatioValue(string formula, Guid companyId, DateTime asOfDate)
    {
        // Simplified ratio calculation - in reality, this would execute complex financial formulas
        return 2.5m; // Placeholder value
    }

    private string DetermineRatioStatus(decimal ratioValue, string industryBenchmark)
    {
        // Simplified status determination
        return "Strong"; // Placeholder
    }

    private List<RatioValuePoint> GenerateRatioTrendValues(string ratioName, Guid companyId, DateTime asOfDate)
    {
        var values = new List<RatioValuePoint>();

        for (int i = 12; i >= 0; i--)
        {
            var date = asOfDate.AddMonths(-i);
            values.Add(new RatioValuePoint
            {
                Date = date,
                Value = 2.5m + (i * 0.1m) // Placeholder trend
            });
        }

        return values;
    }

    private string DetermineTrendDirection(List<RatioValuePoint> values)
    {
        if (values.Count < 2) return "Stable";

        var first = values.First().Value;
        var last = values.Last().Value;

        if (last > first) return "Improving";
        if (last < first) return "Deteriorating";
        return "Stable";
    }

    private decimal GetComparisonValue(string ratioName, string comparisonPeriod)
    {
        // Simplified comparison value retrieval
        return 2.3m; // Placeholder value
    }

    private List<RatioInsight> GenerateRatioInsights(string ratioName, decimal currentValue, decimal comparisonValue)
    {
        var insights = new List<RatioInsight>();

        if (currentValue > comparisonValue)
        {
            insights.Add(new RatioInsight
            {
                InsightType = "Strength",
                Description = $"{ratioName} has improved compared to {comparisonValue:F2}",
                Recommendation = "Continue current strategy",
                ImpactLevel = "Medium"
            });
        }
        else if (currentValue < comparisonValue * 0.9m) // More than 10% worse
        {
            insights.Add(new RatioInsight
            {
                InsightType = "Weakness",
                Description = $"{ratioName} has deteriorated compared to {comparisonValue:F2}",
                Recommendation = "Investigate causes and implement corrective actions",
                ImpactLevel = "High"
            });
        }

        return insights;
    }

    #endregion
}
