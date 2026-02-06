using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class AdvancedAccountingServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly AdvancedAccountingService _service;

    public AdvancedAccountingServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new AdvancedAccountingService(_mockContext.Object);
    }

    [Fact]
    public async Task CreateAccrualAdjustmentAsync_ValidExpenseAccrual_CreatesJournal()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var amount = 1000m;
        var description = "Utilities accrual";
        var asOfDate = DateTime.Today;

        var expenseAccount = new Account
        {
            Id = accountId,
            AccountNumber = "5000",
            Name = "Utilities Expense",
            AccountType = AccountType.Expense,
            CompanyId = companyId
        };

        var accruedLiabilityAccount = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "2100",
            Name = "Accrued Utilities",
            AccountType = AccountType.Liability,
            CompanyId = companyId
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(expenseAccount);
        _mockContext.Setup(c => c.Accounts.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(accruedLiabilityAccount);
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.CreateAccrualAdjustmentAsync(accountId, amount, description, asOfDate, companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(asOfDate, result.JournalDate);
        Assert.Contains("Accrual Adjustment", result.Description);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(JournalStatus.Draft, result.Status);
    }

    [Fact]
    public async Task CreateAccrualAdjustmentAsync_ValidRevenueAccrual_CreatesJournal()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var amount = 1500m;
        var description = "Service revenue accrual";
        var asOfDate = DateTime.Today;

        var revenueAccount = new Account
        {
            Id = accountId,
            AccountNumber = "4000",
            Name = "Service Revenue",
            AccountType = AccountType.Revenue,
            CompanyId = companyId
        };

        var accruedAssetAccount = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "1200",
            Name = "Accrued Receivables",
            AccountType = AccountType.Asset,
            CompanyId = companyId
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(revenueAccount);
        _mockContext.Setup(c => c.Accounts.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(accruedAssetAccount);
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.CreateAccrualAdjustmentAsync(accountId, amount, description, asOfDate, companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(asOfDate, result.JournalDate);
        Assert.Contains("Accrual Adjustment", result.Description);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(JournalStatus.Draft, result.Status);
    }

    [Fact]
    public async Task CreatePrepaymentDeferralAsync_ValidDeferral_CreatesJournal()
    {
        // Arrange
        var prepaidAccountId = Guid.NewGuid();
        var expenseAccountId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var amount = 12000m;
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddYears(1);

        var prepaidAccount = new Account
        {
            Id = prepaidAccountId,
            AccountNumber = "1100",
            Name = "Prepaid Insurance",
            AccountType = AccountType.Asset,
            CompanyId = companyId
        };

        var expenseAccount = new Account
        {
            Id = expenseAccountId,
            AccountNumber = "5100",
            Name = "Insurance Expense",
            AccountType = AccountType.Expense,
            CompanyId = companyId
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(prepaidAccount);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(expenseAccount);
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.CreatePrepaymentDeferralAsync(prepaidAccountId, expenseAccountId, amount, startDate, endDate, companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.JournalDate);
        Assert.Contains("Prepayment Deferral", result.Description);
        Assert.Equal(JournalStatus.Draft, result.Status);
    }

    [Fact]
    public async Task CreateMultiPeriodAllocationAsync_ValidAllocation_CreatesJournal()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var targetAccountIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var allocationPercentages = new[] { 60.0m, 40.0m }; // Should sum to 100%
        var effectiveDate = DateTime.Today;
        var companyId = Guid.NewGuid();

        var sourceAccount = new Account
        {
            Id = sourceAccountId,
            AccountNumber = "1000",
            Name = "Cash",
            AccountType = AccountType.Asset,
            CompanyId = companyId
        };

        var targetAccounts = new List<Account>
        {
            new Account { Id = targetAccountIds[0], AccountNumber = "1100", Name = "Petty Cash", AccountType = AccountType.Asset, CompanyId = companyId },
            new Account { Id = targetAccountIds[1], AccountNumber = "1200", Name = "Travel Fund", AccountType = AccountType.Asset, CompanyId = companyId }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(sourceAccount);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(targetAccounts[0]);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(targetAccounts[1]);
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.CreateMultiPeriodAllocationAsync(sourceAccountId, targetAccountIds, allocationPercentages, effectiveDate, companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(effectiveDate, result.JournalDate);
        Assert.Contains("Multi-period allocation", result.Description);
        Assert.Equal(JournalStatus.Draft, result.Status);
    }

    [Fact]
    public async Task CreateIntercompanyEntryAsync_ValidEntry_CreatesJournal()
    {
        // Arrange
        var sourceCompanyId = Guid.NewGuid();
        var targetCompanyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var amount = 5000m;
        var description = "Transfer to sister company";
        var effectiveDate = DateTime.Today;

        var sourceClearingAccount = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "1900",
            Name = "Due From Subsidiary A",
            AccountType = AccountType.Asset,
            CompanyId = sourceCompanyId
        };

        _mockContext.Setup(c => c.Accounts.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(sourceClearingAccount);
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.CreateIntercompanyEntryAsync(sourceCompanyId, targetCompanyId, accountId, amount, description, effectiveDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(effectiveDate, result.JournalDate);
        Assert.Contains("Intercompany transaction", result.Description);
        Assert.Equal(sourceCompanyId, result.CompanyId);
        Assert.Equal(JournalStatus.Draft, result.Status);
    }

    [Fact]
    public async Task ProcessPeriodEndClosingAsync_ValidPeriod_ClosesSuccessfully()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var periodEndDate = DateTime.Today;

        var revenueAccounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), AccountNumber = "4000", Name = "Sales Revenue", AccountType = AccountType.Revenue, CompanyId = companyId }
        };

        var expenseAccounts = new List<Account>
        {
            new Account { Id = Guid.NewGuid(), AccountNumber = "5000", Name = "Operating Expenses", AccountType = AccountType.Expense, CompanyId = companyId }
        };

        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(revenueAccounts.AsQueryable());
        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(expenseAccounts.AsQueryable());
        _mockContext.Setup(c => c.Accounts.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new Account { Id = Guid.NewGuid(), AccountNumber = "3999", Name = "Income Summary", AccountType = AccountType.Equity, CompanyId = companyId });
        _mockContext.Setup(c => c.Accounts.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new Account { Id = Guid.NewGuid(), AccountNumber = "3000", Name = "Retained Earnings", AccountType = AccountType.Equity, CompanyId = companyId });
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<LedgerEntry, bool>>>()))
            .Returns(new List<LedgerEntry>().AsQueryable());

        // Act
        var result = await _service.ProcessPeriodEndClosingAsync(companyId, periodEndDate);

        // Assert
        Assert.True(result);
    }
}
