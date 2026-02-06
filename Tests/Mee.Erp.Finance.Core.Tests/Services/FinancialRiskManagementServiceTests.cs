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

public class FinancialRiskManagementServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly FinancialRiskManagementService _service;

    public FinancialRiskManagementServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new FinancialRiskManagementService(_mockContext.Object);
    }

    [Fact]
    public async Task MeasureInterestRateRiskAsync_ValidRequest_ReturnsRiskResult()
    {
        // Arrange
        var request = new InterestRateRiskRequest
        {
            CompanyId = Guid.NewGuid(),
            AsOfDate = DateTime.Today,
            PortfolioAssetIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            InterestRateChangeScenario = 0.5m, // 50 basis points
            RiskMetric = "DV01"
        };

        var accounts = new List<Account>
        {
            new Account { Id = request.PortfolioAssetIds[0], AccountNumber = "1000", Name = "Cash", AccountType = Domain.Enums.AccountType.Asset, CompanyId = request.CompanyId },
            new Account { Id = request.PortfolioAssetIds[1], AccountNumber = "1100", Name = "Investments", AccountType = Domain.Enums.AccountType.Asset, CompanyId = request.CompanyId }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.First);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.Skip(1).First);

        // Act
        var result = await _service.MeasureInterestRateRiskAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEqual(0, result.PortfolioValue);
        Assert.NotEqual(0, result.ValueAtRisk);
        Assert.NotEmpty(result.Sensitivities);
        Assert.Equal(2, result.Sensitivities.Count);
    }

    [Fact]
    public async Task AssessCreditRiskAsync_ValidRequest_ReturnsRiskResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        var request = new CreditRiskRequest
        {
            CompanyId = companyId,
            CustomerIds = new List<Guid> { customerId },
            SupplierIds = new List<Guid> { supplierId },
            AsOfDate = DateTime.Today,
            RiskModel = "ProbabilityOfDefault"
        };

        var customers = new List<Customer>
        {
            new Customer { Id = customerId, Name = "Test Customer", CompanyId = companyId }
        };

        var suppliers = new List<Supplier>
        {
            new Supplier { Id = supplierId, Name = "Test Supplier", CompanyId = companyId }
        };

        var salesInvoices = new List<SalesInvoice>
        {
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customerId, CompanyId = companyId, TotalAmount = 10000, AmountPaid = 8000, Status = Domain.Enums.SalesInvoiceStatus.PartiallyPaid }
        };

        var purchaseInvoices = new List<PurchaseInvoice>
        {
            new PurchaseInvoice { Id = Guid.NewGuid(), SupplierId = supplierId, CompanyId = companyId, TotalAmount = 5000, AmountPaid = 3000, Status = Domain.Enums.PurchaseInvoiceStatus.PartiallyPaid }
        };

        _mockContext.Setup(c => c.Customers.FindAsync(It.IsAny<object[]>())).ReturnsAsync(customers.First);
        _mockContext.Setup(c => c.Suppliers.FindAsync(It.IsAny<object[]>())).ReturnsAsync(suppliers.First);
        _mockContext.Setup(c => c.SalesInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<SalesInvoice, bool>>>()))
            .Returns(salesInvoices.AsQueryable());
        _mockContext.Setup(c => c.PurchaseInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<PurchaseInvoice, bool>>>()))
            .Returns(purchaseInvoices.AsQueryable());

        // Act
        var result = await _service.AssessCreditRiskAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEmpty(result.CustomerRisks);
        Assert.NotEmpty(result.SupplierRisks);
        Assert.Equal(1, result.CustomerRisks.Count);
        Assert.Equal(1, result.SupplierRisks.Count);
        Assert.NotEqual(0, result.TotalExpectedCreditLoss);
    }

    [Fact]
    public async Task ManageCounterpartyRiskAsync_ValidRequest_ReturnsRiskResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var counterpartyId = Guid.NewGuid();

        var request = new CounterpartyRiskRequest
        {
            CompanyId = companyId,
            CounterpartyIds = new List<Guid> { counterpartyId },
            AsOfDate = DateTime.Today,
            ThresholdAmount = 10000
        };

        var customers = new List<Customer>
        {
            new Customer { Id = counterpartyId, Name = "Test Customer", CompanyId = companyId }
        };

        var salesInvoices = new List<SalesInvoice>
        {
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = counterpartyId, CompanyId = companyId, TotalAmount = 15000, AmountPaid = 5000, Status = Domain.Enums.SalesInvoiceStatus.PartiallyPaid }
        };

        _mockContext.Setup(c => c.Customers.FindAsync(It.IsAny<object[]>())).ReturnsAsync(customers.First);
        _mockContext.Setup(c => c.SalesInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<SalesInvoice, bool>>>()))
            .Returns(salesInvoices.AsQueryable());

        // Act
        var result = await _service.ManageCounterpartyRiskAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEmpty(result.CounterpartyProfiles);
        Assert.Equal(1, result.CounterpartyProfiles.Count);
        Assert.NotEqual(0, result.TotalCounterpartyExposure);
        Assert.Equal(1, result.Alerts.Count);
        Assert.Equal("ExposureThreshold", result.Alerts.First().AlertType);
    }

    [Fact]
    public async Task PerformMarketRiskAnalyticsAsync_ValidRequest_ReturnsRiskResult()
    {
        // Arrange
        var request = new MarketRiskRequest
        {
            CompanyId = Guid.NewGuid(),
            AsOfDate = DateTime.Today,
            PortfolioAssetIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            VaRCalculationMethod = "Historical",
            ConfidenceLevel = 0.95m,
            HoldingPeriodDays = 1
        };

        var accounts = new List<Account>
        {
            new Account { Id = request.PortfolioAssetIds[0], AccountNumber = "1000", Name = "Cash", AccountType = Domain.Enums.AccountType.Asset, CompanyId = request.CompanyId },
            new Account { Id = request.PortfolioAssetIds[1], AccountNumber = "1100", Name = "Investments", AccountType = Domain.Enums.AccountType.Asset, CompanyId = request.CompanyId }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.First);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.Skip(1).First);

        // Act
        var result = await _service.PerformMarketRiskAnalyticsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEqual(0, result.PortfolioValue);
        Assert.NotEqual(0, result.ValueAtRisk);
        Assert.NotEmpty(result.RiskFactors);
        Assert.NotEmpty(result.StressTestResults);
        Assert.Equal(3, result.RiskFactors.Count);
        Assert.Equal(2, result.StressTestResults.Count);
    }

    [Fact]
    public async Task ManageCreditLossProvisioningAsync_ValidRequest_ReturnsProvisionResult()
    {
        // Arrange
        var request = new CreditLossProvisionRequest
        {
            CompanyId = Guid.NewGuid(),
            AsOfDate = DateTime.Today,
            AccountingStandard = "CECL",
            FinancialAssetIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        var accounts = new List<Account>
        {
            new Account { Id = request.FinancialAssetIds[0], AccountNumber = "1200", Name = "Accounts Receivable", AccountType = Domain.Enums.AccountType.Asset, CompanyId = request.CompanyId },
            new Account { Id = request.FinancialAssetIds[1], AccountNumber = "1201", Name = "Loans Receivable", AccountType = Domain.Enums.AccountType.Asset, CompanyId = request.CompanyId }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.First);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.Skip(1).First);

        // Act
        var result = await _service.ManageCreditLossProvisioningAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEmpty(result.Provisions);
        Assert.Equal(2, result.Provisions.Count);
        Assert.NotEqual(0, result.TotalExpectedCreditLoss);
        Assert.NotEmpty(result.AccountingEntries);
    }

    [Fact]
    public async Task GenerateConcentrationRiskReportAsync_CustomerRisk_ReturnsReport()
    {
        // Arrange
        var request = new ConcentrationRiskRequest
        {
            CompanyId = Guid.NewGuid(),
            AsOfDate = DateTime.Today,
            RiskType = "Customer"
        };

        var customers = new List<Customer>
        {
            new Customer { Id = Guid.NewGuid(), Name = "Major Customer 1", CompanyId = request.CompanyId },
            new Customer { Id = Guid.NewGuid(), Name = "Major Customer 2", CompanyId = request.CompanyId },
            new Customer { Id = Guid.NewGuid(), Name = "Small Customer", CompanyId = request.CompanyId }
        };

        var salesInvoices = new List<SalesInvoice>
        {
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customers[0].Id, CompanyId = request.CompanyId, TotalAmount = 50000, AmountPaid = 0, Status = Domain.Enums.SalesInvoiceStatus.AWaitingPayment },
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customers[1].Id, CompanyId = request.CompanyId, TotalAmount = 30000, AmountPaid = 0, Status = Domain.Enums.SalesInvoiceStatus.AWaitingPayment },
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customers[2].Id, CompanyId = request.CompanyId, TotalAmount = 5000, AmountPaid = 0, Status = Domain.Enums.SalesInvoiceStatus.AWaitingPayment }
        };

        _mockContext.Setup(c => c.Customers.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>()))
            .Returns(customers.AsQueryable());
        _mockContext.Setup(c => c.SalesInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<SalesInvoice, bool>>>()))
            .Returns(salesInvoices.AsQueryable());

        // Act
        var result = await _service.GenerateConcentrationRiskReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal("Customer", result.RiskType);
        Assert.NotEmpty(result.Buckets);
        Assert.Equal(3, result.Buckets.Count);
        Assert.NotEqual(0, result.TotalExposure);
        Assert.NotEqual(0, result.LargestExposure);
        Assert.NotEmpty(result.Alerts);
        Assert.Equal(2, result.Alerts.Count); // Both concentration alerts should trigger
    }

    [Fact]
    public async Task ManageLiquidityRiskAsync_ValidRequest_ReturnsRiskResult()
    {
        // Arrange
        var request = new LiquidityRiskRequest
        {
            CompanyId = Guid.NewGuid(),
            AsOfDate = DateTime.Today,
            StressPeriodDays = 90
        };

        var accounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), AccountNumber = "1000", Name = "Cash", AccountType = Domain.Enums.AccountType.Asset, CompanyId = request.CompanyId },
            new Account { Id = Guid.NewGuid(), AccountNumber = "1001", Name = "Checking", AccountType = Domain.Enums.AccountType.Asset, CompanyId = request.CompanyId }
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = accounts[0].Id, Debit = 100000, Credit = 0, EntryDate = DateTime.Today.AddDays(-1) },
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = accounts[1].Id, Debit = 50000, Credit = 0, EntryDate = DateTime.Today.AddDays(-1) }
        };

        var purchaseInvoices = new List<PurchaseInvoice>
        {
            new PurchaseInvoice { Id = Guid.NewGuid(), CompanyId = request.CompanyId, TotalAmount = 20000, DueDate = DateTime.Today.AddDays(30), Status = Domain.Enums.PurchaseInvoiceStatus.AWaitingPayment },
            new PurchaseInvoice { Id = Guid.NewGuid(), CompanyId = request.CompanyId, TotalAmount = 15000, DueDate = DateTime.Today.AddDays(60), Status = Domain.Enums.PurchaseInvoiceStatus.AWaitingPayment }
        };

        var salesInvoices = new List<SalesInvoice>
        {
            new SalesInvoice { Id = Guid.NewGuid(), CompanyId = request.CompanyId, TotalAmount = 25000, DueDate = DateTime.Today.AddDays(15), Status = Domain.Enums.SalesInvoiceStatus.AWaitingPayment },
            new SalesInvoice { Id = Guid.NewGuid(), CompanyId = request.CompanyId, TotalAmount = 18000, DueDate = DateTime.Today.AddDays(45), Status = Domain.Enums.SalesInvoiceStatus.AWaitingPayment }
        };

        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(accounts.AsQueryable());
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());
        _mockContext.Setup(c => c.PurchaseInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<PurchaseInvoice, bool>>>()))
            .Returns(purchaseInvoices.AsQueryable());
        _mockContext.Setup(c => c.SalesInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<SalesInvoice, bool>>>()))
            .Returns(salesInvoices.AsQueryable());

        // Act
        var result = await _service.ManageLiquidityRiskAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEqual(0, result.CurrentCashBalance);
        Assert.NotEqual(0, result.CommittedCashOutflows);
        Assert.NotEqual(0, result.CommittedCashInflows);
        Assert.NotEqual(0, result.NetCashFlow30Days);
        Assert.NotEqual(0, result.NetCashFlow90Days);
        Assert.NotEqual(0, result.NetCashFlow180Days);
        Assert.Equal(1.0m, result.LiquidityCoverageRatio); // Simplified calculation
        Assert.Equal(1.0m, result.NetStableFundingRatio); // Simplified calculation
        Assert.NotEmpty(result.Alerts);
        Assert.Contains(result.Alerts, a => a.AlertType == "LowLiquidity");
    }
}
