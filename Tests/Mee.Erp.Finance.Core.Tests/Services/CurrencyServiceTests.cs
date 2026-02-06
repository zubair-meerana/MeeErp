using FluentAssertions;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class CurrencyServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task CreateExchangeRateAsync_Should_Create_Exchange_Rate()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CurrencyService(context);

        var exchangeRate = new ExchangeRate
        {
            FromCurrencyCode = "USD",
            ToCurrencyCode = "EUR",
            Rate = 0.85m,
            EffectiveDate = DateTime.Today,
            ExpiryDate = DateTime.Today.AddMonths(1)
        };

        // 2. ACT
        var result = await service.CreateExchangeRateAsync(exchangeRate);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.FromCurrencyCode.Should().Be("USD");
        result.ToCurrencyCode.Should().Be("EUR");
        result.Rate.Should().Be(0.85m);
        result.EffectiveDate.Should().Be(DateTime.Today);

        var savedRate = await context.ExchangeRates.FindAsync(result.Id);
        savedRate.Should().NotBeNull();
        savedRate.FromCurrencyCode.Should().Be("USD");
    }

    [Fact]
    public async Task GetExchangeRateAsync_Should_Return_Exchange_Rate_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CurrencyService(context);

        var exchangeRate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            FromCurrencyCode = "USD",
            ToCurrencyCode = "EUR",
            Rate = 0.85m,
            EffectiveDate = DateTime.Today.AddDays(-1),
            ExpiryDate = DateTime.Today.AddMonths(1)
        };

        context.ExchangeRates.Add(exchangeRate);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetExchangeRateAsync("USD", "EUR", DateTime.Today);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.FromCurrencyCode.Should().Be("USD");
        result.ToCurrencyCode.Should().Be("EUR");
        result.Rate.Should().Be(0.85m);
    }

    [Fact]
    public async Task GetExchangeRateAsync_Should_Return_Null_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CurrencyService(context);

        // No exchange rates in the database

        // 2. ACT
        var result = await service.GetExchangeRateAsync("USD", "EUR", DateTime.Today);

        // 3. ASSERT
        result.Should().BeNull();
    }

    [Fact]
    public async Task ConvertCurrencyAsync_Should_Convert_Amount()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CurrencyService(context);

        var exchangeRate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            FromCurrencyCode = "USD",
            ToCurrencyCode = "EUR",
            Rate = 0.85m,
            EffectiveDate = DateTime.Today.AddDays(-1),
            ExpiryDate = DateTime.Today.AddMonths(1)
        };

        context.ExchangeRates.Add(exchangeRate);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.ConvertCurrencyAsync(100, "USD", "EUR", DateTime.Today);

        // 3. ASSERT
        result.Should().Be(85); // 100 * 0.85
    }

    [Fact]
    public async Task GetCurrencyRatesForDateRangeAsync_Should_Return_Rates()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CurrencyService(context);

        var rates = new List<ExchangeRate>
        {
            new ExchangeRate { Id = Guid.NewGuid(), FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 0.85m, EffectiveDate = DateTime.Today.AddDays(-5), ExpiryDate = DateTime.Today.AddDays(25) },
            new ExchangeRate { Id = Guid.NewGuid(), FromCurrencyCode = "USD", ToCurrencyCode = "GBP", Rate = 0.75m, EffectiveDate = DateTime.Today.AddDays(-3), ExpiryDate = DateTime.Today.AddDays(27) },
            new ExchangeRate { Id = Guid.NewGuid(), FromCurrencyCode = "EUR", ToCurrencyCode = "GBP", Rate = 0.88m, EffectiveDate = DateTime.Today.AddDays(-10), ExpiryDate = DateTime.Today.AddDays(20) }
        };

        context.ExchangeRates.AddRange(rates);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetCurrencyRatesForDateRangeAsync(DateTime.Today, DateTime.Today.AddDays(5));

        // 3. ASSERT
        result.Should().HaveCount(2); // USD-EUR and USD-GBP rates are effective during this period
    }

    [Fact]
    public async Task UpdateExchangeRateAsync_Should_Update_Exchange_Rate()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CurrencyService(context);

        var exchangeRate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            FromCurrencyCode = "USD",
            ToCurrencyCode = "EUR",
            Rate = 0.85m,
            EffectiveDate = DateTime.Today.AddDays(-1),
            ExpiryDate = DateTime.Today.AddMonths(1)
        };

        context.ExchangeRates.Add(exchangeRate);
        await context.SaveChangesAsync();

        // Update the rate
        exchangeRate.Rate = 0.87m;
        exchangeRate.EffectiveDate = DateTime.Today;

        // 2. ACT
        var result = await service.UpdateExchangeRateAsync(exchangeRate);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Rate.Should().Be(0.87m);
        result.EffectiveDate.Should().Be(DateTime.Today);

        var savedRate = await context.ExchangeRates.FindAsync(exchangeRate.Id);
        savedRate.Rate.Should().Be(0.87m);
    }
}
