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

public class FinancialValidationServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly FinancialValidationService _service;

    public FinancialValidationServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new FinancialValidationService(_mockContext.Object);
    }

    [Fact]
    public async Task ValidateInterModuleDependenciesAsync_ValidInventoryTransaction_ReturnsTrue()
    {
        // Arrange
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Description = "Test inventory transaction",
            JournalDate = DateTime.Today,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 0,
                    Description = "Inventory COGS"
                }
            }
        };

        // Act
        var result = await _service.ValidateInterModuleDependenciesAsync(journal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateARAPCrossValidationAsync_ValidARTransaction_ReturnsTrue()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Test AR transaction",
            JournalDate = DateTime.Today,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 0,
                    Description = "Accounts Receivable"
                }
            }
        };

        // Mock customer existence
        _mockContext.Setup(x => x.Customers.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ValidateARAPCrossValidationAsync(journal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateCurrencyExchangeAsync_NoFXTransactions_ReturnsValid()
    {
        // Arrange
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Description = "Test regular transaction",
            JournalDate = DateTime.Today,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 1000,
                    Credit = 0,
                    Description = "Regular entry"
                }
            }
        };

        // Act
        var result = await _service.ValidateCurrencyExchangeAsync(journal);

        // Assert
        Assert.True(result.isValid);
        Assert.Equal(0, result.gainLossAmount);
        Assert.Null(result.errorMessage);
    }
}
