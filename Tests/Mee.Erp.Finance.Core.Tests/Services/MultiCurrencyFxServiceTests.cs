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

public class MultiCurrencyFxServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly MultiCurrencyFxService _service;

    public MultiCurrencyFxServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new MultiCurrencyFxService(_mockContext.Object);
    }

    [Fact]
    public async Task GetExchangeRateWithHistoryAsync_ValidCurrencyPair_ReturnsRate()
    {
        // Arrange
        var exchangeRate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            FromCurrencyCode = "USD",
            ToCurrencyCode = "EUR",
            Rate = 0.85m,
            EffectiveDate = DateTime.Today.AddDays(-5),
            ExpiryDate = DateTime.Today.AddDays(25)
        };

        var rates = new List<ExchangeRate> { exchangeRate };
        _mockContext.Setup(c => c.ExchangeRates.Where(It.IsAny<System.Linq.Expressions.Expression<Func<ExchangeRate, bool>>>()))
            .Returns(rates.AsQueryable());
        _mockContext.Setup(c => c.ExchangeRates.OrderByDescending(It.IsAny<System.Linq.Expressions.Expression<Func<ExchangeRate, DateTime>>>()))
            .Returns(rates.OrderByDescending(er => er.EffectiveDate).AsQueryable());

        // Act
        var result = await _service.GetExchangeRateWithHistoryAsync("USD", "EUR", DateTime.Today);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("USD", result.FromCurrencyCode);
        Assert.Equal("EUR", result.ToCurrencyCode);
        Assert.Equal(0.85m, result.Rate);
    }

    [Fact]
    public async Task ConvertCurrencyWithHistoryAsync_ValidConversion_ReturnsConvertedAmount()
    {
        // Arrange
        var exchangeRate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            FromCurrencyCode = "USD",
            ToCurrencyCode = "EUR",
            Rate = 0.85m,
            EffectiveDate = DateTime.Today.AddDays(-1),
            ExpiryDate = DateTime.Today.AddMonths(1)
        };

        var rates = new List<ExchangeRate> { exchangeRate };
        _mockContext.Setup(c => c.ExchangeRates.Where(It.IsAny<System.Linq.Expressions.Expression<Func<ExchangeRate, bool>>>()))
            .Returns(rates.AsQueryable());
        _mockContext.Setup(c => c.ExchangeRates.OrderByDescending(It.IsAny<System.Linq.Expressions.Expression<Func<ExchangeRate, DateTime>>>()))
            .Returns(rates.OrderByDescending(er => er.EffectiveDate).AsQueryable());

        // Act
        var result = await _service.ConvertCurrencyWithHistoryAsync(100, "USD", "EUR", DateTime.Today);

        // Assert
        Assert.Equal(85, result); // 100 * 0.85
    }

    [Fact]
    public async Task CalculateFxGainLossAsync_ValidTransaction_ReturnsGainLossResult()
    {
        // Arrange
        var transactionRate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            FromCurrencyCode = "USD",
            ToCurrencyCode = "EUR",
            Rate = 0.85m,
            EffectiveDate = DateTime.Today.AddDays(-10),
            ExpiryDate = DateTime.Today.AddDays(15)
        };

        var settlementRate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            FromCurrencyCode = "USD",
            ToCurrencyCode = "EUR",
            Rate = 0.90m, // Rate increased (USD weakened against EUR)
            EffectiveDate = DateTime.Today.AddDays(-2),
            ExpiryDate = DateTime.Today.AddDays(23)
        };

        var rates = new List<ExchangeRate> { transactionRate, settlementRate };
        _mockContext.Setup(c => c.ExchangeRates.Where(It.IsAny<System.Linq.Expressions.Expression<Func<ExchangeRate, bool>>>()))
            .Returns(rates.AsQueryable());
        _mockContext.Setup(c => c.ExchangeRates.OrderByDescending(It.IsAny<System.Linq.Expressions.Expression<Func<ExchangeRate, DateTime>>>()))
            .Returns(rates.OrderByDescending(er => er.EffectiveDate).AsQueryable());

        var transactionId = Guid.NewGuid();
        var transactionDate = DateTime.Today.AddDays(-5);
        var settlementDate = DateTime.Today;

        // Act
        var result = await _service.CalculateFxGainLossAsync(transactionId, "USD", "EUR", 1000, transactionDate, settlementDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.OriginalAmount);
        Assert.Equal(900, result.ConvertedAmount); // 1000 * 0.90
        Assert.Equal(50, result.GainLossAmount); // (1000 * 0.90) - (1000 * 0.85) = 900 - 850 = 50
        Assert.Equal("Loss", result.GainLossType); // Since settlement value is higher than transaction value
        Assert.Equal(0.85m, result.ExchangeRateAtTransaction);
        Assert.Equal(0.90m, result.ExchangeRateAtSettlement);
        Assert.Equal(transactionDate, result.TransactionDate);
        Assert.Equal(settlementDate, result.SettlementDate);
    }

    [Fact]
    public async Task ProcessHedgeInstrumentAsync_ForwardInstrument_ReturnsResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var hedgeInstrument = new HedgeInstrument
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            InstrumentType = "Forward",
            CurrencyPair = "USD/EUR",
            NotionalAmount = 100000,
            StrikeRate = 0.85m,
            ExpirationDate = DateTime.Today.AddMonths(3),
            Premium = 1000,
            Purpose = "Cash Flow Hedge"
        };

        // Act
        var result = await _service.ProcessHedgeInstrumentAsync(hedgeInstrument, companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(hedgeInstrument.Id, result.InstrumentId);
        Assert.True(result.IsProcessed);
        Assert.Equal("Processed", result.ProcessingStatus);
        Assert.NotEqual(0, result.FairValue);
        Assert.Equal(result.FairValue * 0.9m, result.EffectivePortion);
        Assert.Equal(result.FairValue * 0.1m, result.IneffectivePortion);
        Assert.NotEmpty(result.AccountingEntries);
    }

    [Fact]
    public async Task ProcessMultiBookAccountingAsync_ValidJournal_ReturnsTrue()
    {
        // Arrange
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Description = "Test journal",
            JournalDate = DateTime.Today,
            Status = Domain.Enums.JournalStatus.Draft
        };

        var options = new MultiBookOptions
        {
            BookType = "Statutory",
            ReportingCurrency = "USD",
            IncludeInConsolidation = true
        };

        // Act
        var result = await _service.ProcessMultiBookAccountingAsync(journal, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GenerateFxExposureReportAsync_ValidCompany_ReturnsReport()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var asOfDate = DateTime.Today;
        var baseCurrency = "USD";

        // Act
        var result = await _service.GenerateFxExposureReportAsync(companyId, asOfDate, baseCurrency);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(companyId, result.CompanyId);
        Assert.Equal(asOfDate, result.ReportDate);
        Assert.Equal(baseCurrency, result.BaseCurrency);
        Assert.NotEmpty(result.ExposureLines);
        Assert.NotEqual(0, result.TotalExposure);
        Assert.NotEqual(0, result.NetExposure);
    }
}
