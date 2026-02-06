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

public class ErrorCorrectionServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly ErrorCorrectionService _service;

    public ErrorCorrectionServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new ErrorCorrectionService(_mockContext.Object);
    }

    [Fact]
    public async Task GenerateReversalJournalAsync_ValidJournal_GeneratesReversal()
    {
        // Arrange
        var journalId = Guid.NewGuid();
        var reason = "Incorrect entry";
        var correctedByUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var originalJournal = new Journal
        {
            Id = journalId,
            CompanyId = companyId,
            Description = "Original journal",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Posted,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 0,
                    Description = "Original debit"
                },
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 0,
                    Credit = 1000,
                    Description = "Original credit"
                }
            }
        };

        _mockContext.Setup(c => c.Journals.Include(It.IsAny<string>()).FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(originalJournal);
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.GenerateReversalJournalAsync(journalId, reason, correctedByUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("REVERSAL:", result.Description);
        Assert.Contains(reason, result.Description);
        Assert.Equal(JournalStatus.Draft, result.Status);
        Assert.Equal(2, result.Entries.Count);
        // Verify entries are reversed
        var reversedDebitEntry = result.Entries.First(e => e.AccountId == originalJournal.Entries.First().AccountId);
        Assert.Equal(0, reversedDebitEntry.Debit);
        Assert.Equal(1000, reversedDebitEntry.Credit);
    }

    [Fact]
    public async Task ProcessErrorCorrectionAsync_ValidCorrection_ReturnsTrue()
    {
        // Arrange
        var journalId = Guid.NewGuid();
        var correctionDetails = "Amount was incorrect";
        var correctedByUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var journalToCorrect = new Journal
        {
            Id = journalId,
            CompanyId = companyId,
            Description = "Journal to correct",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Posted
        };

        _mockContext.Setup(c => c.Journals.Include(It.IsAny<string>()).FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(journalToCorrect);
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.ProcessErrorCorrectionAsync(journalId, correctionDetails, correctedByUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ProcessJournalReclassificationAsync_ValidReclassification_ReturnsJournal()
    {
        // Arrange
        var journalId = Guid.NewGuid();
        var newAccountIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var newAmounts = new[] { 750m, 250m };
        var reason = "Wrong account classification";
        var reclassifiedByUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var originalJournal = new Journal
        {
            Id = journalId,
            CompanyId = companyId,
            Description = "Original journal",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Posted,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 0
                },
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 0,
                    Credit = 1000
                }
            }
        };

        _mockContext.Setup(c => c.Journals.Include(It.IsAny<string>()).FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(originalJournal);
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(2); // Two saves: reversal and reclassification

        // Act
        var result = await _service.ProcessJournalReclassificationAsync(journalId, newAccountIds, newAmounts, reason, reclassifiedByUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("RECLASSIFICATION:", result.Description);
        Assert.Contains(reason, result.Description);
        Assert.Equal(JournalStatus.Draft, result.Status);
    }

    [Fact]
    public async Task ProcessPeriodAdjustmentAsync_ValidAdjustment_ReturnsJournal()
    {
        // Arrange
        var journalId = Guid.NewGuid();
        var newEffectiveDate = DateTime.Today.AddDays(5);
        var reason = "Date correction";
        var adjustedByUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var journalToAdjust = new Journal
        {
            Id = journalId,
            CompanyId = companyId,
            Description = "Journal to adjust",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Posted,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 0
                },
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 0,
                    Credit = 1000
                }
            }
        };

        var financialPeriod = new FinancialPeriod
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = "Current Period",
            StartDate = DateTime.Today.AddDays(-10),
            EndDate = DateTime.Today.AddDays(10),
            IsClosed = false
        };

        _mockContext.Setup(c => c.Journals.Include(It.IsAny<string>()).FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(journalToAdjust);
        _mockContext.Setup(c => c.FinancialPeriods.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FinancialPeriod, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(financialPeriod);
        _mockContext.Setup(c => c.Journals.Add(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(2); // Two saves: reversal and adjustment

        // Act
        var result = await _service.ProcessPeriodAdjustmentAsync(journalId, newEffectiveDate, reason, adjustedByUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("PERIOD ADJUSTMENT:", result.Description);
        Assert.Contains(reason, result.Description);
        Assert.Equal(newEffectiveDate, result.JournalDate);
        Assert.Equal(JournalStatus.Draft, result.Status);
    }

    [Fact]
    public async Task ValidateCorrectionEntryAsync_ValidCorrection_ReturnsTrue()
    {
        // Arrange
        var validatorUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var correctionJournal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "CORRECTION: Wrong amount entered",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Draft,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 500,
                    Credit = 0
                },
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 0,
                    Credit = 500
                }
            }
        };

        var accountIds = correctionJournal.Entries.Select(e => e.AccountId).ToList();
        var accounts = accountIds.Select(id => new Account { Id = id, CompanyId = companyId }).ToList();

        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(accounts.AsQueryable());

        // Act
        var result = await _service.ValidateCorrectionEntryAsync(correctionJournal, validatorUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateCorrectionEntryAsync_UnbalancedJournal_ReturnsFalse()
    {
        // Arrange
        var validatorUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var correctionJournal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "CORRECTION: Wrong amount entered",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Draft,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 500,
                    Credit = 0
                },
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 0,
                    Credit = 300 // Unbalanced: 500 debit vs 300 credit
                }
            }
        };

        var accountIds = correctionJournal.Entries.Select(e => e.AccountId).ToList();
        var accounts = accountIds.Select(id => new Account { Id = id, CompanyId = companyId }).ToList();

        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(accounts.AsQueryable());

        // Act
        var result = await _service.ValidateCorrectionEntryAsync(correctionJournal, validatorUserId);

        // Assert
        Assert.False(result);
    }
}
