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

public class BudgetingPlanningServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly BudgetingPlanningService _service;

    public BudgetingPlanningServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new BudgetingPlanningService(_mockContext.Object);
    }

    [Fact]
    public async Task CreateRollingForecastAsync_ValidRequest_ReturnsForecast()
    {
        // Arrange
        var request = new RollingForecastRequest
        {
            CompanyId = Guid.NewGuid(),
            ForecastHorizonMonths = 12,
            StartDate = DateTime.Today,
            ForecastMethod = "TimeSeries",
            IncludePredictiveAnalytics = true,
            ConfidenceInterval = "90%",
            Drivers = new List<ForecastDriver>
            {
                new ForecastDriver
                {
                    Name = "Market Growth",
                    Type = "Economic",
                    Weight = 30,
                    Formula = "Linear Trend",
                    HistoricalData = new List<HistoricalDataPoint>
                    {
                        new HistoricalDataPoint { Date = DateTime.Today.AddMonths(-3), Value = 10000 },
                        new HistoricalDataPoint { Date = DateTime.Today.AddMonths(-2), Value = 10500 },
                        new HistoricalDataPoint { Date = DateTime.Today.AddMonths(-1), Value = 11000 }
                    }
                }
            }
        };

        // Act
        var result = await _service.CreateRollingForecastAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(request.ForecastMethod, result.ModelUsed);
        Assert.Equal(12, result.ForecastPeriods.Count);
        Assert.NotEqual(0, result.OverallAccuracy);
        Assert.Equal(12, result.ConfidenceIntervals.Count);
    }

    [Fact]
    public async Task CreateDriverBasedBudgetAsync_ValidRequest_ReturnsBudget()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var request = new DriverBasedBudgetRequest
        {
            CompanyId = companyId,
            BudgetId = budgetId,
            BudgetYear = DateTime.Today,
            CalculationMethod = "ActivityBased",
            Allocations = new List<DriverBasedAllocation>
            {
                new DriverBasedAllocation
                {
                    AccountId = Guid.NewGuid(),
                    DriverName = "Headcount",
                    DriverValue = 10,
                    RatePerUnit = 5000,
                    Formula = "Headcount * RatePerUnit"
                }
            }
        };

        // Act
        var result = await _service.CreateDriverBasedBudgetAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(budgetId, result.BudgetId);
        Assert.True(result.IsCalculated);
        Assert.Equal(request.CalculationMethod, result.CalculationMethod);
        Assert.Single(result.AllocationResults);

        var allocationResult = result.AllocationResults.First();
        Assert.Equal(50000, allocationResult.CalculatedAmount); // 10 * 5000
    }

    [Fact]
    public async Task PerformVarianceAnalysisAsync_ValidRequest_ReturnsAnalysis()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var budget = new Budget
        {
            Id = budgetId,
            CompanyId = companyId,
            Name = "Test Budget",
            FiscalYear = DateTime.Today.Year,
            TotalBudgetAmount = 100000,
            Status = Domain.Enums.BudgetStatus.Active,
            Lines = new List<BudgetLine>
            {
                new BudgetLine
                {
                    Id = Guid.NewGuid(),
                    BudgetId = budgetId,
                    AccountId = accountId,
                    BudgetAmount = 50000,
                    ActualAmount = 0,
                    Variance = 0,
                    VariancePercentage = 0
                }
            }
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Debit = 55000,
                Credit = 0,
                EntryDate = DateTime.Today,
                Description = "Test expense"
            }
        };

        var account = new Account
        {
            Id = accountId,
            AccountNumber = "1000",
            Name = "Test Account",
            AccountType = Domain.Enums.AccountType.Expense,
            CompanyId = companyId
        };

        _mockContext.Setup(c => c.Budgets.Include(It.IsAny<string>()).FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Budget, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(budget);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(account);

        var request = new VarianceAnalysisRequest
        {
            CompanyId = companyId,
            BudgetId = budgetId,
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            AccountIds = new List<Guid> { accountId },
            VarianceThreshold = 5,
            IncludeDrillDown = true
        };

        // Act
        var result = await _service.PerformVarianceAnalysisAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(budgetId, result.RequestId);
        Assert.Single(result.VarianceLines);

        var varianceLine = result.VarianceLines.First();
        Assert.Equal(accountId, varianceLine.AccountId);
        Assert.Equal(50000, varianceLine.BudgetedAmount);
        Assert.Equal(55000, varianceLine.ActualAmount);
        Assert.Equal(5000, varianceLine.Variance);
        Assert.Equal(10, varianceLine.VariancePercentage);
        Assert.Equal("Unfavorable", varianceLine.VarianceType);
    }

    [Fact]
    public async Task ProcessBudgetApprovalWorkflowAsync_ValidRequest_ReturnsResult()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var requestorId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var request = new BudgetApprovalRequest
        {
            BudgetId = budgetId,
            RequestorId = requestorId,
            CompanyId = companyId,
            ApprovalLevel = "Department",
            TotalAmount = 50000,
            Justification = "Annual department budget",
            ApprovalSteps = new List<ApprovalStep>
            {
                new ApprovalStep
                {
                    StepNumber = 1,
                    ApproverId = approverId,
                    Role = "Department Manager",
                    ApprovalLimit = 100000,
                    Status = "Pending"
                }
            }
        };

        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.ProcessBudgetApprovalWorkflowAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(budgetId, result.BudgetId);
        Assert.Equal("Approved", result.OverallStatus);
        Assert.Single(result.ApprovalSteps);

        var approvalStep = result.ApprovalSteps.First();
        Assert.Equal(approverId, approvalStep.ApproverId);
        Assert.Equal("Approved", approvalStep.Status);
        Assert.NotNull(approvalStep.ApprovalDate);
    }

    [Fact]
    public async Task CreateScenarioModelAsync_ValidRequest_ReturnsModel()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var request = new ScenarioModelingRequest
        {
            CompanyId = companyId,
            ScenarioName = "Best Case Scenario",
            ScenarioType = "BestCase",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1),
            AccountIds = new List<Guid> { accountId },
            Assumptions = new List<ScenarioAssumption>
            {
                new ScenarioAssumption
                {
                    Factor = "Market Growth",
                    Value = 1.1m,
                    Unit = "Multiplier",
                    ImpactFormula = "BaseValue * Factor",
                    Sensitivity = 0.8m
                }
            }
        };

        var account = new Account
        {
            Id = accountId,
            AccountNumber = "1000",
            Name = "Test Account",
            AccountType = Domain.Enums.AccountType.Revenue,
            CompanyId = companyId
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(account);

        // Act
        var result = await _service.CreateScenarioModelAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.ScenarioName, result.ScenarioName);
        Assert.Single(result.Outcomes);
        Assert.Single(result.SensitivityAnalyses);

        var outcome = result.Outcomes.First();
        Assert.Equal(accountId, outcome.AccountId);
        Assert.Equal(account.Name, outcome.AccountName);

        var sensitivity = result.SensitivityAnalyses.First();
        Assert.Equal("Market Growth", sensitivity.Factor);
        Assert.Equal(0.8m, sensitivity.SensitivityCoefficient);
    }

    [Fact]
    public async Task PerformBudgetAllocationAsync_ProportionalMethod_ReturnsAllocation()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var targetId1 = Guid.NewGuid();
        var targetId2 = Guid.NewGuid();

        var request = new BudgetAllocationRequest
        {
            CompanyId = companyId,
            BudgetId = budgetId,
            AllocationMethod = "Proportional",
            TotalBudgetAmount = 100000,
            Rules = new List<AllocationRule>(),
            Targets = new List<AllocationTarget>
            {
                new AllocationTarget { TargetId = targetId1, TargetType = "Department", AllocatedAmount = 0, RemainingAmount = 0 },
                new AllocationTarget { TargetId = targetId2, TargetType = "Department", AllocatedAmount = 0, RemainingAmount = 0 }
            }
        };

        // Act
        var result = await _service.PerformBudgetAllocationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(budgetId, result.BudgetId);
        Assert.Equal("Proportional", result.AllocationMethod);
        Assert.Equal(2, result.Allocations.Count);
        Assert.Equal(100000, result.TotalAllocated);
        Assert.Equal(0, result.TotalRemaining);
    }

    [Fact]
    public async Task CreateZeroBasedBudgetAsync_ValidRequest_ReturnsBudget()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var activityId = Guid.NewGuid();

        var request = new ZeroBasedBudgetRequest
        {
            CompanyId = companyId,
            BudgetId = budgetId,
            BudgetYear = DateTime.Today,
            Activities = new List<ZbbActivity>
            {
                new ZbbActivity
                {
                    ActivityId = activityId,
                    ActivityName = "Office Supplies",
                    Description = "Monthly office supplies",
                    Resources = new List<ZbbResource>
                    {
                        new ZbbResource
                        {
                            ResourceName = "Pens",
                            ResourceType = "Material",
                            Quantity = 100,
                            UnitCost = 2,
                            TotalCost = 200,
                            Justification = "Required for daily operations"
                        }
                    },
                    TotalCost = 200
                }
            },
            DecisionPackages = new List<ZbbDecisionPackage>
            {
                new ZbbDecisionPackage
                {
                    PackageId = Guid.NewGuid(),
                    PackageName = "Essential Supplies",
                    Alternatives = new List<ZbbAlternative>
                    {
                        new ZbbAlternative
                        {
                            AlternativeName = "Premium Pens",
                            Cost = 500,
                            Benefit = "Better quality",
                            Rationale = "Justified for client-facing materials"
                        }
                    },
                    Priority = "MustHave"
                }
            }
        };

        // Act
        var result = await _service.CreateZeroBasedBudgetAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(budgetId, result.BudgetId);
        Assert.Single(result.Activities);
        Assert.Single(result.DecisionPackages);
        Assert.Equal(200, result.TotalBudgetedAmount);

        var activity = result.Activities.First();
        Assert.Equal(activityId, activity.ActivityId);
        Assert.Equal("Office Supplies", activity.ActivityName);
        Assert.Single(activity.Resources);
    }
}
