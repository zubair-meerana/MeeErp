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

public class CashManagementServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly CashManagementService _service;

    public CashManagementServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new CashManagementService(_mockContext.Object);
    }

    [Fact]
    public async Task GenerateCashFlowForecastAsync_ValidDates_GeneratesForecast()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(7);

        _mockContext.Setup(c => c.SalesInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<SalesInvoice, bool>>>()))
            .Returns(new List<SalesInvoice>().AsQueryable());
        _mockContext.Setup(c => c.PurchaseInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<PurchaseInvoice, bool>>>()))
            .Returns(new List<PurchaseInvoice>().AsQueryable());
        _mockContext.Setup(c => c.Journals.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>()))
            .Returns(new List<Journal>().AsQueryable());
        _mockContext.Setup(c => c.Journals.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>()))
            .Returns(new List<Journal>().AsQueryable());
        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(new List<Account>().AsQueryable());
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<LedgerEntry, bool>>>()))
            .Returns(new List<LedgerEntry>().AsQueryable());

        // Act
        var result = await _service.GenerateCashFlowForecastAsync(companyId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(8, result.Count()); // 7 days + 1 for start date
    }

    [Fact]
    public async Task GetLiquidityPositionAsync_ValidCompany_ReturnsPosition()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var asOfDate = DateTime.Today;

        var cashAccounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), AccountNumber = "1000", Name = "Cash", AccountType = Domain.Enums.AccountType.Asset, CompanyId = companyId },
            new Account { Id = Guid.NewGuid(), AccountNumber = "1001", Name = "Checking", AccountType = Domain.Enums.AccountType.Asset, CompanyId = companyId }
        };

        var investmentAccounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), AccountNumber = "1100", Name = "Short Term Investments", AccountType = Domain.Enums.AccountType.Asset, CompanyId = companyId }
        };

        var currentLiabilityAccounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), AccountNumber = "2000", Name = "Accounts Payable", AccountType = Domain.Enums.AccountType.Liability, CompanyId = companyId }
        };

        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(cashAccounts.AsQueryable());
        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(investmentAccounts.AsQueryable());
        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(currentLiabilityAccounts.AsQueryable());
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<LedgerEntry, bool>>>()))
            .Returns(new List<LedgerEntry>().AsQueryable());

        // Act
        var result = await _service.GetLiquidityPositionAsync(companyId, asOfDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(500000, result.AvailableCreditLines); // Default value from implementation
    }

    [Fact]
    public async Task ProcessCashPoolingAsync_ValidAccounts_ReturnsTrue()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var bankAccountIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var bankAccounts = new List<BankAccount>
        {
            new BankAccount { Id = bankAccountIds[0], CompanyId = companyId, AccountName = "Primary Checking", CurrentBalance = 50000 },
            new BankAccount { Id = bankAccountIds[1], CompanyId = companyId, AccountName = "Secondary Checking", CurrentBalance = 25000 }
        };

        _mockContext.Setup(c => c.BankAccounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<BankAccount, bool>>>()))
            .Returns(bankAccounts.AsQueryable());
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.ProcessCashPoolingAsync(companyId, bankAccountIds);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetInvestmentIncomeAsync_ValidDates_ReturnsIncome()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        var investmentJournals = new List<Journal>
        {
            new Journal
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Description = "Dividend Income",
                JournalDate = DateTime.Today.AddDays(-10),
                Entries = new List<JournalEntry>
                {
                    new JournalEntry { Id = Guid.NewGuid(), AccountId = Guid.NewGuid(), Credit = 500, Debit = 0 }
                }
            }
        };

        var investmentAccount = new Account
        {
            Id = investmentJournals.First().Entries.First().AccountId,
            Name = "Dividend Income",
            AccountType = Domain.Enums.AccountType.Revenue,
            CompanyId = companyId
        };

        _mockContext.Setup(c => c.Journals.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>()).Include(It.IsAny<string>()).ThenInclude(It.IsAny<string>()))
            .Returns(investmentJournals.AsQueryable());
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(investmentAccount);

        // Act
        var result = await _service.GetInvestmentIncomeAsync(companyId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GenerateCashPositionReportAsync_ValidCompany_ReturnsReport()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var asOfDate = DateTime.Today;

        var bankAccounts = new List<BankAccount>
        {
            new BankAccount { Id = Guid.NewGuid(), CompanyId = companyId, AccountName = "Operating Account", CurrentBalance = 100000 },
            new BankAccount { Id = Guid.NewGuid(), CompanyId = companyId, AccountName = "Payroll Account", CurrentBalance = 50000 }
        };

        _mockContext.Setup(c => c.BankAccounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<BankAccount, bool>>>()))
            .Returns(bankAccounts.AsQueryable());
        _mockContext.Setup(c => c.SalesInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<SalesInvoice, bool>>>()))
            .Returns(new List<SalesInvoice>().AsQueryable());
        _mockContext.Setup(c => c.PurchaseInvoices.Where(It.IsAny<System.Linq.Expressions.Expression<Func<PurchaseInvoice, bool>>>()))
            .Returns(new List<PurchaseInvoice>().AsQueryable());
        _mockContext.Setup(c => c.Journals.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>()))
            .Returns(new List<Journal>().AsQueryable());
        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(new List<Account>().AsQueryable());
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<LedgerEntry, bool>>>()))
            .Returns(new List<LedgerEntry>().AsQueryable());

        // Act
        var result = await _service.GenerateCashPositionReportAsync(companyId, asOfDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(asOfDate, result.ReportDate);
        Assert.Equal(150000, result.TotalCashPosition);
    }
}
