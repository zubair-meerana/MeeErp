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

public class FinancialControlServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly FinancialControlService _service;

    public FinancialControlServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new FinancialControlService(_mockContext.Object);
    }

    [Fact]
    public async Task SetSpendingAuthorizationLimitAsync_ValidInput_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var limit = 50000m;

        // Act
        var result = await _service.SetSpendingAuthorizationLimitAsync(userId, limit, companyId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EnforceSegregationOfDutiesAsync_SameUserCreatedAndUpdated_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Test journal",
            JournalDate = DateTime.Today,
            CreatedBy = userId,
            UpdatedBy = userId // Same user created and updated
        };

        // Act
        var result = await _service.EnforceSegregationOfDutiesAsync(journal, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EnforceSegregationOfDutiesAsync_DifferentUsers_ReturnsTrue()
    {
        // Arrange
        var creatingUserId = Guid.NewGuid();
        var updatingUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Test journal",
            JournalDate = DateTime.Today,
            CreatedBy = creatingUserId,
            UpdatedBy = updatingUserId // Different user updated
        };

        // Act
        var result = await _service.EnforceSegregationOfDutiesAsync(journal, updatingUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ApplyDualControlForOperationAsync_SameUserInitiatesAndApproves_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Test journal",
            JournalDate = DateTime.Today
        };

        _mockContext.Setup(c => c.Journals.FindAsync(It.IsAny<object[]>())).ReturnsAsync(journal);
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.ApplyDualControlForOperationAsync(journal, userId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ApplyDualControlForOperationAsync_DifferentUsers_ReturnsTrue()
    {
        // Arrange
        var initiatingUserId = Guid.NewGuid();
        var approvingUserId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Test journal",
            JournalDate = DateTime.Today
        };

        _mockContext.Setup(c => c.Journals.Update(It.IsAny<Journal>())).Returns(It.IsAny<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Journal>>());
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.ApplyDualControlForOperationAsync(journal, initiatingUserId, approvingUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task MonitorUnusualTransactionPatternsAsync_NormalTransaction_ReturnsTrue()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Normal transaction",
            JournalDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 10, 0, 0), // Business hours
            JournalDate = DateTime.Today,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 100, // Reasonable amount
                    Credit = 0
                }
            }
        };

        // Mock average transaction amount
        _mockContext.Setup(c => c.Journals.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>()))
            .Returns(new List<Journal>().AsQueryable());
        _mockContext.Setup(c => c.Journals.Select(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, decimal>>>()))
            .Returns(new List<decimal> { 50 }.AsQueryable());
        _mockContext.Setup(c => c.Journals.AverageAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, decimal>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(50.0);

        // Act
        var result = await _service.MonitorUnusualTransactionPatternsAsync(journal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task MonitorUnusualTransactionPatternsAsync_LargeTransaction_ReturnsFalse()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Large transaction",
            JournalDate = DateTime.Today,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000000, // Very large amount
                    Credit = 0
                }
            }
        };

        // Mock average transaction amount
        _mockContext.Setup(c => c.Journals.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, bool>>>()))
            .Returns(new List<Journal>().AsQueryable());
        _mockContext.Setup(c => c.Journals.AverageAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Journal, decimal>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(100.0); // Much smaller average

        // Act
        var result = await _service.MonitorUnusualTransactionPatternsAsync(journal);

        // Assert
        Assert.False(result);
    }
}
