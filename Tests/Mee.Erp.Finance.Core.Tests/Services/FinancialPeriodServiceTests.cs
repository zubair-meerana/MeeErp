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

public class FinancialPeriodServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task CreateFinancialPeriodAsync_Should_Create_Period()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FinancialPeriodService(context);

        var companyId = Guid.NewGuid();
        var period = new FinancialPeriod
        {
            Name = "Q1 2026",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31),
            CompanyId = companyId,
            IsClosed = false
        };

        // 2. ACT
        var result = await service.CreateFinancialPeriodAsync(period);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Name.Should().Be("Q1 2026");
        result.StartDate.Should().Be(new DateTime(2026, 1, 1));
        result.EndDate.Should().Be(new DateTime(2026, 3, 31));
        result.CompanyId.Should().Be(companyId);
        result.IsClosed.Should().BeFalse();

        var savedPeriod = await context.FinancialPeriods.FindAsync(result.Id);
        savedPeriod.Should().NotBeNull();
        savedPeriod.Name.Should().Be("Q1 2026");
    }

    [Fact]
    public async Task GetFinancialPeriodByIdAsync_Should_Return_Period_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FinancialPeriodService(context);

        var period = new FinancialPeriod
        {
            Id = Guid.NewGuid(),
            Name = "Q1 2026",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31),
            CompanyId = Guid.NewGuid(),
            IsClosed = false
        };

        context.FinancialPeriods.Add(period);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetFinancialPeriodByIdAsync(period.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(period.Id);
        result.Name.Should().Be("Q1 2026");
        result.StartDate.Should().Be(new DateTime(2026, 1, 1));
        result.EndDate.Should().Be(new DateTime(2026, 3, 31));
    }

    [Fact]
    public async Task GetFinancialPeriodByIdAsync_Should_ThrowException_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FinancialPeriodService(context);

        var nonExistentId = Guid.NewGuid();

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.GetFinancialPeriodByIdAsync(nonExistentId)
        );
    }

    [Fact]
    public async Task GetFinancialPeriodsByCompanyAsync_Should_Return_Periods()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FinancialPeriodService(context);

        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();

        var periods = new List<FinancialPeriod>
        {
            new FinancialPeriod { Id = Guid.NewGuid(), Name = "Q1 2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 3, 31), CompanyId = companyId, IsClosed = false },
            new FinancialPeriod { Id = Guid.NewGuid(), Name = "Q2 2026", StartDate = new DateTime(2026, 4, 1), EndDate = new DateTime(2026, 6, 30), CompanyId = companyId, IsClosed = false },
            new FinancialPeriod { Id = Guid.NewGuid(), Name = "Q1 2026 Other", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 3, 31), CompanyId = otherCompanyId, IsClosed = false }
        };

        context.FinancialPeriods.AddRange(periods);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetFinancialPeriodsByCompanyAsync(companyId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(p => p.Name == "Q1 2026");
        result.Should().ContainSingle(p => p.Name == "Q2 2026");
        result.Should().NotContain(p => p.Name == "Q1 2026 Other");
    }

    [Fact]
    public async Task GetCurrentFinancialPeriodAsync_Should_Return_Current_Period()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FinancialPeriodService(context);

        var companyId = Guid.NewGuid();

        var periods = new List<FinancialPeriod>
        {
            new FinancialPeriod { Id = Guid.NewGuid(), Name = "Past Period", StartDate = DateTime.Today.AddDays(-30), EndDate = DateTime.Today.AddDays(-1), CompanyId = companyId, IsClosed = false },
            new FinancialPeriod { Id = Guid.NewGuid(), Name = "Current Period", StartDate = DateTime.Today.AddDays(-5), EndDate = DateTime.Today.AddDays(25), CompanyId = companyId, IsClosed = false }, // Current period
            new FinancialPeriod { Id = Guid.NewGuid(), Name = "Future Period", StartDate = DateTime.Today.AddDays(30), EndDate = DateTime.Today.AddDays(60), CompanyId = companyId, IsClosed = false }
        };

        context.FinancialPeriods.AddRange(periods);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetCurrentFinancialPeriodAsync(companyId);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Name.Should().Be("Current Period");
        result.StartDate.Should().Be(DateTime.Today.AddDays(-5));
        result.EndDate.Should().Be(DateTime.Today.AddDays(25));
    }

    [Fact]
    public async Task CloseFinancialPeriodAsync_Should_Close_Period()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FinancialPeriodService(context);

        var period = new FinancialPeriod
        {
            Id = Guid.NewGuid(),
            Name = "Q1 2026",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31),
            CompanyId = Guid.NewGuid(),
            IsClosed = false
        };

        context.FinancialPeriods.Add(period);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.CloseFinancialPeriodAsync(period.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(period.Id);
        result.IsClosed.Should().BeTrue();

        var savedPeriod = await context.FinancialPeriods.FindAsync(period.Id);
        savedPeriod.IsClosed.Should().BeTrue();
    }
}
