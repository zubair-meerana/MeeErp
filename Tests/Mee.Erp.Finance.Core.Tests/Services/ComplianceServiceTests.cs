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

public class ComplianceServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly ComplianceService _service;

    public ComplianceServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new ComplianceService(_mockContext.Object);
    }

    [Fact]
    public async Task PerformAutomatedComplianceCheckAsync_CompliantJournal_ReturnsTrue()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Compliant transaction",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Draft,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 1000
                }
            }
        };

        var account = new Account
        {
            Id = journal.Entries.First().AccountId,
            AccountNumber = "1000",
            Name = "Cash",
            AccountType = AccountType.Asset,
            CompanyId = companyId,
            IsActive = true
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(account);
        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(new List<Account> { account }.AsQueryable());
        _mockContext.Setup(c => c.Journals.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>()))
            .Returns(new List<Journal>().AsQueryable());
        _mockContext.Setup(c => c.Journals.AverageAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, decimal>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(500.0);
        _mockContext.Setup(c => c.FinancialPeriods.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FinancialPeriod, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new FinancialPeriod { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Q1", StartDate = DateTime.Today.AddDays(-30), EndDate = DateTime.Today.AddDays(30), IsClosed = false });

        // Act
        var result = await _service.PerformAutomatedComplianceCheckAsync(journal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task PerformAutomatedComplianceCheckAsync_NonCompliantJournal_ReturnsFalse()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "", // Empty description - non-compliant
            JournalDate = DateTime.Today,
            Status = JournalStatus.Draft,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 500 // Unbalanced - non-compliant
                }
            }
        };

        var account = new Account
        {
            Id = journal.Entries.First().AccountId,
            AccountNumber = "1000",
            Name = "Cash",
            AccountType = AccountType.Asset,
            CompanyId = companyId,
            IsActive = true
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(account);
        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(new List<Account> { account }.AsQueryable());
        _mockContext.Setup(c => c.Journals.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>()))
            .Returns(new List<Journal>().AsQueryable());
        _mockContext.Setup(c => c.Journals.AverageAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, decimal>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(500.0);
        _mockContext.Setup(c => c.FinancialPeriods.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FinancialPeriod, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new FinancialPeriod { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Q1", StartDate = DateTime.Today.AddDays(-30), EndDate = DateTime.Today.AddDays(30), IsClosed = false });

        // Act
        var result = await _service.PerformAutomatedComplianceCheckAsync(journal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EnsureAuditTrailCompletenessAsync_CompleteAuditTrail_ReturnsTrue()
    {
        // Arrange
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            Description = "Test journal",
            JournalDate = DateTime.Today,
            CreatedDate = DateTime.Today.AddDays(-1),
            CreatedBy = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid(),
            UpdatedDate = DateTime.Today
        };

        // Act
        var result = await _service.EnsureAuditTrailCompletenessAsync(journal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EnsureAuditTrailCompletenessAsync_IncompleteAuditTrail_ReturnsFalse()
    {
        // Arrange
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            Description = "Test journal",
            JournalDate = DateTime.Today,
            CreatedDate = default(DateTime), // Missing created date
            CreatedBy = Guid.NewGuid()
        };

        // Act
        var result = await _service.EnsureAuditTrailCompletenessAsync(journal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ManagePeriodClosingChecklistAsync_CompleteChecklist_ReturnsTrue()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var periodEndDate = DateTime.Today;

        _mockContext.Setup(c => c.Journals.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>()))
            .Returns(new List<Journal>().AsQueryable());
        _mockContext.Setup(c => c.BankTransactions.Where(It.IsAny<System.Linq.Expressions.Expression<Func<BankTransaction, bool>>>()))
            .Returns(new List<BankTransaction>().AsQueryable());
        _mockContext.Setup(c => c.FinancialPeriods.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FinancialPeriod, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new FinancialPeriod { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Q1", StartDate = DateTime.Today.AddDays(-30), EndDate = DateTime.Today.AddDays(30), IsClosed = false });
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.ManagePeriodClosingChecklistAsync(companyId, periodEndDate);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateLocalRegulatoryComplianceAsync_USCompliance_ReturnsTrue()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "US GAAP compliant transaction",
            JournalDate = DateTime.Today,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 1000
                }
            }
        };

        // Act
        var result = await _service.ValidateLocalRegulatoryComplianceAsync(journal, "US");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateLocalRegulatoryComplianceAsync_UAECompliance_ReturnsTrue()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "UAE VAT compliant transaction",
            JournalDate = DateTime.Today,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 1000
                }
            }
        };

        // Act
        var result = await _service.ValidateLocalRegulatoryComplianceAsync(journal, "AE");

        // Assert
        Assert.True(result);
    }
}
