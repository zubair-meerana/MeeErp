using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class FpaServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly FpaService _service;

    public FpaServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new FpaService(_mockContext.Object);
    }

    [Fact]
    public async Task CreateDriverBasedPlanningModelAsync_ValidRequest_ReturnsPlanningResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new DriverBasedPlanningRequest
        {
            CompanyId = companyId,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(12),
            PlanningHorizon = "Monthly",
            PlanningMethod = "DriverBased",
            BusinessUnitIds = new List<Guid> { Guid.NewGuid() },
            Drivers = new List<PlanningDriver>
            {
                new PlanningDriver
                {
                    Name = "Market Growth",
                    Type = "Economic",
                    Weight = 0.3m,
                    Formula = "Linear Growth",
                    Values = new List<DriverValue>
                    {
                        new DriverValue { Date = DateTime.Today, Value = 1.05m },
                        new DriverValue { Date = DateTime.Today.AddMonths(1), Value = 1.06m }
                    }
                },
                new PlanningDriver
                {
                    Name = "Headcount",
                    Type = "Operational",
                    Weight = 0.7m,
                    Formula = "Per Employee",
                    Values = new List<DriverValue>
                    {
                        new DriverValue { Date = DateTime.Today, Value = 100 },
                        new DriverValue { Date = DateTime.Today.AddMonths(1), Value = 105 }
                    }
                }
            },
            Constraints = new List<PlanningConstraint>
            {
                new PlanningConstraint
                {
                    ConstraintType = "Resource",
                    Description = "Maximum headcount",
                    Limit = 120,
                    Formula = "Headcount <= 120"
                }
            }
        };

        // Act
        var result = await _service.CreateDriverBasedPlanningModelAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEmpty(result.Outputs);
        Assert.Equal(2, result.Outputs.Count);
        Assert.Equal("Market Growth", result.Outputs.First().OutputName);
        Assert.Equal("Headcount", result.Outputs.Last().OutputName);
        Assert.Equal("Revenue", result.Outputs.First().OutputType);
        Assert.Equal("Headcount", result.Outputs.Last().OutputType);
        Assert.Equal(2, result.Outputs.First().Values.Count);
        Assert.Equal(2, result.Outputs.Last().Values.Count);
        Assert.Equal(2, result.AppliedDrivers.Count);
        Assert.Equal(1, result.AppliedConstraints.Count);
        Assert.Equal(2, result.Assumptions.Count);
        Assert.Contains(result.Assumptions, a => a.AssumptionName == "Economic Growth Rate");
        Assert.Contains(result.Assumptions, a => a.AssumptionName == "Inflation Rate");
    }

    [Fact]
    public async Task PerformSensitivityAnalysisAsync_ValidRequest_ReturnsAnalysisResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new SensitivityAnalysisRequest
        {
            CompanyId = companyId,
            AsOfDate = DateTime.Today,
            ChangePercentage = 10.0m,
            OutputIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            Parameters = new List<SensitivityParameter>
            {
                new SensitivityParameter
                {
                    ParameterName = "Market Growth",
                    ParameterType = "Economic",
                    BaseValue = 1.05m,
                    MinValue = 0.95m,
                    MaxValue = 1.15m,
                    SensitivityCoefficient = 0.8m
                },
                new SensitivityParameter
                {
                    ParameterName = "Cost Per Unit",
                    ParameterType = "Operational",
                    BaseValue = 50.0m,
                    MinValue = 40.0m,
                    MaxValue = 60.0m,
                    SensitivityCoefficient = -0.6m
                }
            }
        };

        // Act
        var result = await _service.PerformSensitivityAnalysisAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(2, result.Outputs.Count);
        Assert.Equal("Market Growth", result.Outputs.First().OutputName);
        Assert.Equal("Cost Per Unit", result.Outputs.Last().OutputName);
        Assert.Equal(0.8m, result.Outputs.First().SensitivityCoefficient);
        Assert.Equal(-0.6m, result.Outputs.Last().SensitivityCoefficient);
        Assert.Equal("High", result.Outputs.First().ImpactLevel);
        Assert.Equal("Medium", result.Outputs.Last().ImpactLevel);
        Assert.Equal(1, result.SensitivityMatrices.Count); // One correlation between the two parameters
        Assert.Equal(3, result.ScenarioResults.Count); // Best, Base, Worst case
        Assert.Equal("Best Case", result.ScenarioResults.First().ScenarioName);
        Assert.Equal("Base Case", result.ScenarioResults.Skip(1).First().ScenarioName);
        Assert.Equal("Worst Case", result.ScenarioResults.Last().ScenarioName);
    }

    [Fact]
    public async Task AutomateVarianceAnalysisAsync_SignificantVariance_ReturnsAnalysisResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var revenueAccountId = Guid.NewGuid();
        var expenseAccountId = Guid.NewGuid();

        var request = new VarianceAnalysisRequest
        {
            CompanyId = companyId,
            BudgetId = budgetId,
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            AccountIds = new List<Guid> { revenueAccountId, expenseAccountId },
            VarianceThreshold = 5.0m, // 5% threshold
            IncludeCommentary = true,
            VarianceType = "Both"
        };

        var budget = new Budget
        {
            Id = budgetId,
            CompanyId = companyId,
            Name = "Test Budget",
            FiscalYear = DateTime.Today.Year,
            TotalBudgetAmount = 100000,
            Lines = new List<BudgetLine>
            {
                new BudgetLine { Id = Guid.NewGuid(), BudgetId = budgetId, AccountId = revenueAccountId, BudgetAmount = 60000, ActualAmount = 0, Variance = 0, VariancePercentage = 0 },
                new BudgetLine { Id = Guid.NewGuid(), BudgetId = budgetId, AccountId = expenseAccountId, BudgetAmount = 40000, ActualAmount = 0, Variance = 0, VariancePercentage = 0 }
            }
        };

        var account = new Account
        {
            Id = revenueAccountId,
            AccountNumber = "4000",
            Name = "Sales Revenue",
            AccountType = Domain.Enums.AccountType.Revenue,
            CompanyId = companyId
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = revenueAccountId, Debit = 0, Credit = 65000, EntryDate = DateTime.Today.AddDays(-15) }, // 5000 more than budget
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = expenseAccountId, Debit = 45000, Credit = 0, EntryDate = DateTime.Today.AddDays(-10) } // 5000 more than budget
        };

        _mockContext.Setup(c => c.Budgets.Include(It.IsAny<string>()).FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Budget, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(budget);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(account);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.AutomateVarianceAnalysisAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEmpty(result.VarianceLines);
        Assert.Equal(2, result.VarianceLines.Count); // Both accounts exceed 5% threshold

        var revenueVariance = result.VarianceLines.First(vl => vl.AccountId == revenueAccountId);
        Assert.Equal(60000, revenueVariance.BudgetedAmount);
        Assert.Equal(65000, revenueVariance.ActualAmount);
        Assert.Equal(5000, revenueVariance.Variance);
        Assert.Equal(8.33m, Math.Round(revenueVariance.VariancePercentage, 2));
        Assert.Equal("Favorable", revenueVariance.VarianceType);
        Assert.Equal("Revenue", revenueVariance.AccountNumber);

        var expenseVariance = result.VarianceLines.First(vl => vl.AccountId == expenseAccountId);
        Assert.Equal(40000, expenseVariance.BudgetedAmount);
        Assert.Equal(45000, expenseVariance.ActualAmount);
        Assert.Equal(5000, expenseVariance.Variance);
        Assert.Equal(12.5m, expenseVariance.VariancePercentage);
        Assert.Equal("Unfavorable", expenseVariance.VarianceType);
        Assert.Equal("Expense", expenseVariance.AccountNumber);

        Assert.Equal(10000, result.TotalVariance);
        Assert.Equal(10.42m, Math.Round(result.TotalVariancePercentage, 2));
        Assert.NotNull(result.Commentary);
        Assert.Contains("Variance Analysis Commentary", result.Commentary);
        Assert.Contains("Overall favorable variance", result.Commentary);
        Assert.NotEmpty(result.DrillDownDetails);
        Assert.Equal(2, result.DrillDownDetails.Count);
        Assert.NotEmpty(result.Investigations);
        Assert.Equal(1, result.Investigations.Count); // Only expense variance exceeds double threshold
    }

    [Fact]
    public async Task TrackForecastAccuracyAsync_ValidRequest_ReturnsAccuracyResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var forecastId = Guid.NewGuid();

        var request = new ForecastAccuracyRequest
        {
            CompanyId = companyId,
            StartDate = DateTime.Today.AddDays(-90),
            EndDate = DateTime.Today,
            ForecastIds = new List<Guid> { forecastId },
            AccuracyMetric = "MAPE",
            AccountIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var result = await _service.TrackForecastAccuracyAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEmpty(result.AccuracyLines);
        Assert.Equal(1, result.AccuracyLines.Count);
        Assert.Equal(forecastId, result.AccuracyLines.First().ForecastId);
        Assert.Equal(100000, result.AccuracyLines.First().ActualValue); // Placeholder value
        Assert.Equal(95000, result.AccuracyLines.First().ForecastValue); // Placeholder value
        Assert.Equal(5000, result.AccuracyLines.First().AbsoluteError);
        Assert.Equal(5.0m, result.AccuracyLines.First().PercentageError);
        Assert.Equal(95.0m, result.OverallAccuracy);
        Assert.Equal(5.0m, result.MeanAbsolutePercentageError);
        Assert.Equal(5000, result.MeanAbsoluteError);
        Assert.Equal(5000, result.RootMeanSquareError); // Simplified
        Assert.NotEmpty(result.Recommendations);
        Assert.Equal(2, result.Recommendations.Count);
        Assert.Contains(result.Recommendations, r => r.RecommendationType == "Model");
        Assert.Contains(result.Recommendations, r => r.RecommendationType == "Data");
    }

    [Fact]
    public async Task CreateRollingForecastAsync_ValidRequest_ReturnsForecastResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new RollingForecastRequest
        {
            CompanyId = companyId,
            StartDate = DateTime.Today,
            ForecastHorizonMonths = 12,
            ForecastMethod = "TimeSeries",
            IncludePredictiveAnalytics = true,
            ConfidenceInterval = "90%",
            BusinessUnitIds = new List<Guid> { Guid.NewGuid() },
            Drivers = new List<ForecastDriver>
            {
                new ForecastDriver
                {
                    Name = "Market Trend",
                    Type = "Economic",
                    Weight = 0.6m,
                    Formula = "Linear Trend",
                    HistoricalData = new List<HistoricalDataPoint>
                    {
                        new HistoricalDataPoint { Date = DateTime.Today.AddMonths(-3), Value = 100000 },
                        new HistoricalDataPoint { Date = DateTime.Today.AddMonths(-2), Value = 105000 },
                        new HistoricalDataPoint { Date = DateTime.Today.AddMonths(-1), Value = 110000 }
                    }
                }
            }
        };

        // Act
        var result = await _service.CreateRollingForecastAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(12, result.ForecastPeriods.Count);
        Assert.Equal(request.ForecastMethod, result.ModelUsed);
        Assert.Equal(1, result.ConfidenceIntervals.Count);
        Assert.Equal(request.ConfidenceInterval, result.ConfidenceIntervals.First().Level);
        Assert.NotEqual(0, result.ConfidenceIntervals.First().LowerBound);
        Assert.NotEqual(0, result.ConfidenceIntervals.First().UpperBound);

        // Check that forecast values are increasing based on trend
        for (int i = 1; i < result.ForecastPeriods.Count; i++)
        {
            Assert.True(result.ForecastPeriods[i].ForecastedValue >= result.ForecastPeriods[i - 1].ForecastedValue);
        }
    }

    [Fact]
    public async Task GenerateManagementCommentaryAsync_ValidRequest_ReturnsCommentaryResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new ManagementCommentaryRequest
        {
            CompanyId = companyId,
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            Topics = new List<CommentaryTopic>
            {
                new CommentaryTopic
                {
                    TopicName = "Revenue Analysis",
                    TopicType = "Revenue",
                    Analysis = "Revenue increased due to new product launches",
                    KeyPoints = "Strong growth in Q4",
                    Outlook = "Expected to continue growing"
                },
                new CommentaryTopic
                {
                    TopicName = "Expense Analysis",
                    TopicType = "Expense",
                    Analysis = "Expenses increased due to expansion",
                    KeyPoints = "Higher operational costs",
                    Outlook = "Cost control measures planned"
                }
            },
            CommentaryType = "Monthly",
            AccountIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var result = await _service.GenerateManagementCommentaryAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(4, result.Sections.Count);
        Assert.Equal("Executive Summary", result.Sections.First().SectionTitle);
        Assert.Equal("Revenue Analysis", result.Sections.Skip(1).First().SectionTitle);
        Assert.Equal("Expense Analysis", result.Sections.Skip(2).First().SectionTitle);
        Assert.Equal("Profitability Analysis", result.Sections.Last().SectionTitle);
        Assert.Contains("Financial performance summary", result.Commentary);
        Assert.Equal(3, result.KeyMetrics.Count);
        Assert.Equal("Revenue Growth", result.KeyMetrics.First().MetricName);
        Assert.Equal("Net Profit Margin", result.KeyMetrics.Skip(1).First().MetricName);
        Assert.Equal("Operating Expenses", result.KeyMetrics.Last().MetricName);
        Assert.Equal(2, result.OutlookItems.Count);
        Assert.Equal("New Product Launch", result.OutlookItems.First().ItemName);
        Assert.Equal("Market Expansion", result.OutlookItems.Last().ItemName);
    }

    [Fact]
    public async Task CreateExecutiveDashboardAsync_ValidRequest_ReturnsDashboardResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new ExecutiveDashboardRequest
        {
            CompanyId = companyId,
            AsOfDate = DateTime.Today,
            Kpis = new List<KpiDefinition>
            {
                new KpiDefinition
                {
                    KpiName = "Revenue Growth",
                    KpiType = "Financial",
                    Formula = "((Current - Previous) / Previous) * 100",
                    TargetValue = "5%",
                    ThresholdValues = "Green:90,Yellow:80,Red:70",
                    TrendDirection = "Positive"
                },
                new KpiDefinition
                {
                    KpiName = "Net Profit Margin",
                    KpiType = "Financial",
                    Formula = "(NetProfit / Revenue) * 100",
                    TargetValue = "15%",
                    ThresholdValues = "Green:15,Yellow:10,Red:5",
                    TrendDirection = "Positive"
                }
            },
            BusinessUnitIds = new List<Guid> { Guid.NewGuid() },
            DashboardType = "Monthly"
        };

        // Act
        var result = await _service.CreateExecutiveDashboardAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(2, result.KpiResults.Count);
        Assert.Equal("Revenue Growth", result.KpiResults.First().KpiName);
        Assert.Equal("Net Profit Margin", result.KpiResults.Last().KpiName);
        Assert.Equal(100000, result.KpiResults.First().CurrentValue); // Placeholder
        Assert.Equal(100000, result.KpiResults.Last().CurrentValue); // Placeholder
        Assert.Equal("Green", result.KpiResults.First().Status);
        Assert.Equal("Up", result.KpiResults.First().Trend);
        Assert.Equal(3, result.Widgets.Count);
        Assert.Equal("Revenue Trend", result.Widgets.First().WidgetName);
        Assert.Equal("Profit Margin", result.Widgets.Skip(1).First().WidgetName);
        Assert.Equal("Key Metrics Table", result.Widgets.Last().WidgetName);
        Assert.Equal($"/dashboard/executive/{companyId}", result.DashboardUrl);
        Assert.Empty(result.Alerts); // No alerts since KPIs are in green zone
    }

    [Fact]
    public async Task PerformFinancialRatioAnalysisAsync_ValidRequest_ReturnsAnalysisResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new FinancialRatioAnalysisRequest
        {
            CompanyId = companyId,
            AsOfDate = DateTime.Today,
            ComparisonPeriod = "PreviousYear",
            AccountIds = new List<Guid> { Guid.NewGuid() },
            RatioCategories = new List<RatioCategory>
            {
                new RatioCategory
                {
                    CategoryName = "Liquidity",
                    Ratios = new List<RatioDefinition>
                    {
                        new RatioDefinition
                        {
                            RatioName = "Current Ratio",
                            Formula = "CurrentAssets / CurrentLiabilities",
                            Description = "Measures ability to pay short-term obligations",
                            IndustryBenchmark = "2.0",
                            Interpretation = "Higher is better"
                        }
                    }
                },
                new RatioCategory
                {
                    CategoryName = "Profitability",
                    Ratios = new List<RatioDefinition>
                    {
                        new RatioDefinition
                        {
                            RatioName = "ROE",
                            Formula = "NetIncome / ShareholdersEquity",
                            Description = "Measures profitability relative to shareholders' equity",
                            IndustryBenchmark = "15%",
                            Interpretation = "Higher is better"
                        }
                    }
                }
            }
        };

        // Act
        var result = await _service.PerformFinancialRatioAnalysisAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(2, result.RatioResults.Count);
        Assert.Equal("Current Ratio", result.RatioResults.First().RatioName);
        Assert.Equal("ROE", result.RatioResults.Last().RatioName);
        Assert.Equal("Liquidity", result.RatioResults.First().Category);
        Assert.Equal("Profitability", result.RatioResults.Last().Category);
        Assert.Equal(2.5m, result.RatioResults.First().Value); // Placeholder
        Assert.Equal(2.5m, result.RatioResults.Last().Value); // Placeholder
        Assert.Equal("Strong", result.RatioResults.First().Status);
        Assert.Equal("Strong", result.RatioResults.Last().Status);
        Assert.Equal(2, result.RatioTrends.Count);
        Assert.Equal(2, result.Comparisons.Count);
        Assert.Equal("Current Ratio", result.Comparisons.First().RatioName);
        Assert.Equal("ROE", result.Comparisons.Last().RatioName);
        Assert.Equal(2, result.Insights.Count);
        Assert.Contains(result.Insights, i => i.InsightType == "Strength");
    }
}
