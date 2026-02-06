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

public class TreasuryCashManagementServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly TreasuryCashManagementService _service;

    public TreasuryCashManagementServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new TreasuryCashManagementService(_mockContext.Object);
    }

    [Fact]
    public async Task CreateCashFlowForecastAsync_ValidRequest_ReturnsForecast()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new CashFlowForecastRequest
        {
            CompanyId = companyId,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(1),
            ForecastHorizon = "Weekly",
            ForecastMethod = "Historical",
            IncludePredictiveAnalytics = true,
            ConfidenceLevel = "90%",
            Drivers = new List<CashFlowDriver>
            {
                new CashFlowDriver
                {
                    Name = "Sales Growth",
                    Type = "Revenue",
                    Weight = 40,
                    Formula = "Linear Trend",
                    HistoricalData = new List<HistoricalCashFlowData>
                    {
                        new HistoricalCashFlowData { Date = DateTime.Today.AddDays(-7), Amount = 50000, Category = "Operating", DataSource = "Sales" },
                        new HistoricalCashFlowData { Date = DateTime.Today.AddDays(-14), Amount = 52000, Category = "Operating", DataSource = "Sales" }
                    }
                }
            }
        };

        var cashAccounts = new List<Account>
        {
            new Account
            {
                Id = Guid.NewGuid(),
                AccountNumber = "1000",
                Name = "Operating Cash",
                AccountType = Domain.Enums.AccountType.Asset,
                CompanyId = companyId
            }
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = cashAccounts.First().Id,
                Debit = 100000,
                Credit = 50000,
                EntryDate = DateTime.Today.AddDays(-1),
                Description = "Opening balance"
            }
        };

        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(cashAccounts.AsQueryable());
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.CreateCashFlowForecastAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(request.ForecastMethod, result.ModelUsed);
        Assert.NotEqual(0, result.BeginningCashBalance);
        Assert.NotEmpty(result.ForecastPeriods);
        Assert.NotEqual(0, result.NetCashFlow);
        Assert.NotEqual(0, result.EndingCashBalance);
        Assert.NotEqual(0, result.AccuracyRating);
    }

    [Fact]
    public async Task ManageInvestmentPortfolioAsync_ValidRequest_ReturnsPortfolio()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new InvestmentPortfolioRequest
        {
            CompanyId = companyId,
            AsOfDate = DateTime.Today,
            PortfolioStrategy = "Balanced",
            RiskTolerance = 0.5m,
            TargetReturn = 8.0m,
            Holdings = new List<InvestmentHolding>
            {
                new InvestmentHolding
                {
                    InvestmentId = Guid.NewGuid(),
                    InvestmentType = "Bond",
                    Issuer = "US Treasury",
                    Ticker = "UST2030",
                    Quantity = 100,
                    UnitPrice = 1050,
                    TotalValue = 105000,
                    PurchaseDate = DateTime.Today.AddYears(-1),
                    PurchasePrice = 1000,
                    CurrentYield = 0.03m,
                    MaturityDate = DateTime.Today.AddYears(5),
                    CouponRate = 0.03m,
                    RiskRating = "AAA"
                }
            }
        };

        // Act
        var result = await _service.ManageInvestmentPortfolioAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Single(result.Holdings);
        Assert.Equal(105000, result.TotalPortfolioValue);
        Assert.Equal(3150, result.TotalAnnualIncome); // 105000 * 3%
        Assert.Equal(3, result.PortfolioYield); // (3150 / 105000) * 100
        Assert.NotEqual(0, result.PortfolioRiskScore);
        Assert.NotEmpty(result.Metrics);
        Assert.NotEmpty(result.AccountingEntries);
    }

    [Fact]
    public async Task ManageBankingRelationshipsAsync_ValidRequest_ReturnsRelationships()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new BankingRelationshipRequest
        {
            CompanyId = companyId,
            AsOfDate = DateTime.Today,
            PrimaryBank = "Bank of America",
            RelationshipManager = "John Smith",
            Arrangements = new List<BankingArrangement>
            {
                new BankingArrangement
                {
                    ArrangementId = Guid.NewGuid(),
                    BankName = "Bank of America",
                    AccountType = "Checking",
                    AccountNumber = "1234567890",
                    CurrentBalance = 50000,
                    AvailableCredit = 100000,
                    InterestRate = 0.02m,
                    Fees = 25,
                    LastStatementDate = DateTime.Today.AddDays(-10),
                    RelationshipManager = "John Smith",
                    ServiceLevel = "Premium",
                    Services = new List<BankingService>
                    {
                        new BankingService { ServiceName = "Wire Transfer", ServiceType = "WireTransfer", Fee = 25, LastUsed = DateTime.Today.AddDays(-5) },
                        new BankingService { ServiceName = "ACH", ServiceType = "ACH", Fee = 0.5m, LastUsed = DateTime.Today.AddDays(-3) }
                    }
                }
            }
        };

        // Act
        var result = await _service.ManageBankingRelationshipsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Single(result.Arrangements);
        Assert.Equal(50000, result.TotalBankBalances);
        Assert.Equal(100000, result.TotalAvailableCredit);
        Assert.Equal(25, result.TotalAnnualFees);
        Assert.Equal("Bank of America", result.PrimaryBank);
        Assert.Equal("John Smith", result.RelationshipManager);
    }

    [Fact]
    public async Task ManageCreditFacilitiesAsync_ValidRequest_ReturnsFacilities()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new CreditFacilityRequest
        {
            CompanyId = companyId,
            AsOfDate = DateTime.Today,
            CreditRating = "A",
            TotalDebtCapacity = 1000000,
            Facilities = new List<CreditFacility>
            {
                new CreditFacility
                {
                    FacilityId = Guid.NewGuid(),
                    FacilityType = "RevolvingCredit",
                    Lender = "Chase Bank",
                    FacilityAmount = 500000,
                    AvailableAmount = 300000,
                    OutstandingAmount = 200000,
                    InterestRate = 0.05m,
                    InterestRateType = "Variable",
                    MaturityDate = DateTime.Today.AddYears(5),
                    NextReviewDate = DateTime.Today.AddMonths(6),
                    Covenants = "Debt to Equity < 0.5",
                    Collateral = "Real Estate",
                    CreditRating = "A",
                    CovenantCompliance = new List<CovenantCompliance>
                    {
                        new CovenantCompliance
                        {
                            CovenantName = "Debt to Equity",
                            CovenantType = "Financial",
                            Requirement = "Debt to Equity < 0.5",
                            ActualValue = 0.4m,
                            RequiredValue = 0.5m,
                            IsCompliant = true,
                            ComplianceDate = DateTime.Today
                        }
                    }
                }
            }
        };

        // Act
        var result = await _service.ManageCreditFacilitiesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Single(result.Facilities);
        Assert.Equal(500000, result.TotalCreditFacilities);
        Assert.Equal(300000, result.TotalAvailableCredit);
        Assert.Equal(200000, result.TotalOutstandingCredit);
        Assert.Equal("Compliant", result.OverallComplianceStatus);
        Assert.Empty(result.ComplianceAlerts);
    }

    [Fact]
    public async Task ManageCreditFacilitiesAsync_NonCompliantFacility_ReturnsAlerts()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new CreditFacilityRequest
        {
            CompanyId = companyId,
            AsOfDate = DateTime.Today,
            CreditRating = "A",
            TotalDebtCapacity = 1000000,
            Facilities = new List<CreditFacility>
            {
                new CreditFacility
                {
                    FacilityId = Guid.NewGuid(),
                    FacilityType = "RevolvingCredit",
                    Lender = "Chase Bank",
                    FacilityAmount = 500000,
                    AvailableAmount = 300000,
                    OutstandingAmount = 200000,
                    InterestRate = 0.05m,
                    InterestRateType = "Variable",
                    MaturityDate = DateTime.Today.AddYears(5),
                    NextReviewDate = DateTime.Today.AddMonths(6),
                    Covenants = "Debt to Equity < 0.5",
                    Collateral = "Real Estate",
                    CreditRating = "A",
                    CovenantCompliance = new List<CovenantCompliance>
                    {
                        new CovenantCompliance
                        {
                            CovenantName = "Debt to Equity",
                            CovenantType = "Financial",
                            Requirement = "Debt to Equity < 0.5",
                            ActualValue = 0.6m, // Above required value
                            RequiredValue = 0.5m,
                            IsCompliant = false,
                            ComplianceDate = DateTime.Today
                        }
                    }
                }
            }
        };

        // Act
        var result = await _service.ManageCreditFacilitiesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Non-Compliant", result.OverallComplianceStatus);
        Assert.Single(result.ComplianceAlerts);

        var alert = result.ComplianceAlerts.First();
        Assert.Equal("Breach", alert.AlertType);
        Assert.Equal("High", alert.Severity);
    }

    [Fact]
    public async Task OptimizeLiquidityAsync_YieldMaximizingStrategy_ReturnsActions()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        var request = new LiquidityOptimizationRequest
        {
            CompanyId = companyId,
            MinimumCashBalance = 10000,
            TargetCashBalance = 50000,
            MaximumCashBalance = 100000,
            OptimizationStrategy = "MaximizeYield",
            InvestmentOpportunities = new List<InvestmentOpportunity>
            {
                new InvestmentOpportunity
                {
                    OpportunityName = "CD 6-month",
                    Type = "CD",
                    Amount = 20000,
                    InterestRate = 0.04m,
                    MaturityDate = DateTime.Today.AddMonths(6),
                    RiskLevel = "Low",
                    MinimumInvestment = 1000
                }
            }
        };

        var cashAccounts = new List<Account>
        {
            new Account
            {
                Id = Guid.NewGuid(),
                AccountNumber = "1000",
                Name = "Operating Cash",
                AccountType = Domain.Enums.AccountType.Asset,
                CompanyId = companyId
            }
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = cashAccounts.First().Id,
                Debit = 80000,
                Credit = 0,
                EntryDate = DateTime.Today,
                Description = "Current balance"
            }
        };

        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(cashAccounts.AsQueryable());
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.OptimizeLiquidityAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(80000, result.CurrentCashBalance);
        Assert.Equal("MaximizeYield", result.OptimizationStrategy);
        Assert.NotEmpty(result.Actions);

        var action = result.Actions.First();
        Assert.Equal("Invest", action.ActionType);
        Assert.Equal("CD 6-month", action.Target);
    }

    [Fact]
    public async Task ManageCashPoolingAsync_ValidRequest_ReturnsPool()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var account1Id = Guid.NewGuid();
        var account2Id = Guid.NewGuid();

        var request = new CashPoolingRequest
        {
            CompanyId = companyId,
            PoolType = "Notional",
            PoolStructure = "ZeroBalance",
            TargetBalance = 0,
            AsOfDate = DateTime.Today,
            PoolMemberAccountIds = new List<Guid> { account1Id, account2Id }
        };

        var accounts = new List<BankAccount>
        {
            new BankAccount { Id = account1Id, CompanyId = companyId, AccountName = "Operating Account", CurrentBalance = 10000 },
            new BankAccount { Id = account2Id, CompanyId = companyId, AccountName = "Payroll Account", CurrentBalance = 5000 }
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = account1Id, Debit = 10000, Credit = 0, EntryDate = DateTime.Today.AddDays(-1) },
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = account2Id, Debit = 5000, Credit = 0, EntryDate = DateTime.Today.AddDays(-1) }
        };

        _mockContext.Setup(c => c.BankAccounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.First);
        _mockContext.Setup(c => c.BankAccounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.Skip(1).First);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.ManageCashPoolingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(2, result.Members.Count);
        Assert.Equal(15000, result.TotalPoolBalance);
    }

    [Fact]
    public async Task ManageSweepAccountsAsync_ValidRequest_ReturnsSweep()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var sweepAccountId = Guid.NewGuid();
        var sourceAccountId = Guid.NewGuid();

        var request = new SweepAccountRequest
        {
            CompanyId = companyId,
            SweepAccountId = sweepAccountId,
            SourceAccountIds = new List<Guid> { sourceAccountId },
            MinimumBalance = 1000,
            SweepThreshold = 5000,
            SweepFrequency = "Daily",
            AsOfDate = DateTime.Today
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = sourceAccountId, Debit = 8000, Credit = 0, EntryDate = DateTime.Today }
        };

        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.ManageSweepAccountsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(sweepAccountId, result.SweepAccountId);
        Assert.Single(result.SweepTransactions);
        Assert.Equal(7000, result.TotalSweptAmount); // 8000 - 1000 (minimum)

        var transaction = result.SweepTransactions.First();
        Assert.Equal(sourceAccountId, transaction.SourceAccountId);
        Assert.Equal(7000, transaction.Amount);
        Assert.Equal("Completed", transaction.Status);
    }
}
