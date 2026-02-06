using FluentAssertions;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class AssetDisposalServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task ProcessAssetDisposalAsync_Should_Dispose_Asset()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AssetDisposalService(context);

        var companyId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var asset = new FixedAsset
        {
            Id = Guid.NewGuid(),
            Name = "Test Equipment",
            PurchaseDate = DateTime.Today.AddYears(-2),
            PurchasePrice = 10000,
            SalvageValue = 1000,
            UsefulLifeYears = 5,
            DepreciationMethod = DepreciationMethod.StraightLine,
            AssetCategoryId = categoryId,
            CompanyId = companyId,
            IsActive = true
        };

        context.FixedAssets.Add(asset);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.ProcessAssetDisposalAsync(asset.Id, DateTime.Today, 3000, "Sold to another company");

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(asset.Id);
        result.IsActive.Should().BeFalse(); // Asset should be marked as disposed

        var savedAsset = await context.FixedAssets.FindAsync(asset.Id);
        savedAsset.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAssetDisposalAsync_Should_ThrowException_When_Asset_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AssetDisposalService(context);

        var nonExistentAssetId = Guid.NewGuid();

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.ProcessAssetDisposalAsync(nonExistentAssetId, DateTime.Today, 3000, "Disposed")
        );
    }

    [Fact]
    public async Task CalculateDisposalGainLossAsync_Should_Calculate_Gain_Loss()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AssetDisposalService(context);

        var companyId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var asset = new FixedAsset
        {
            Id = Guid.NewGuid(),
            Name = "Test Equipment",
            PurchaseDate = DateTime.Today.AddYears(-1), // Owned for 1 year
            PurchasePrice = 10000,
            SalvageValue = 1000,
            UsefulLifeYears = 5,
            DepreciationMethod = DepreciationMethod.StraightLine,
            AssetCategoryId = categoryId,
            CompanyId = companyId,
            IsActive = true
        };

        context.FixedAssets.Add(asset);
        await context.SaveChangesAsync();

        // For straight-line: annual depreciation = (10000 - 1000) / 5 = 1800
        // After 1 year: accumulated depreciation = 1800, book value = 8200

        // 2. ACT
        var result = await service.CalculateDisposalGainLossAsync(asset.Id, 7000); // Sale price

        // 3. ASSERT
        result.Should().NotBeNull();
        result.AssetBookValue.Should().BeApproximately(8200, 10); // Purchase price - accumulated depreciation
        result.DisposalProceeds.Should().Be(7000);
        result.GainLoss.Should().BeApproximately(-1200, 10); // Loss = 7000 - 8200 = -1200
    }

    [Fact]
    public async Task CalculateDisposalGainLossAsync_Should_ThrowException_When_Asset_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AssetDisposalService(context);

        var nonExistentAssetId = Guid.NewGuid();

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.CalculateDisposalGainLossAsync(nonExistentAssetId, 5000)
        );
    }

    [Fact]
    public async Task GetAssetDisposalByIdAsync_Should_Return_Disposal_Record()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AssetDisposalService(context);

        var disposal = new FixedAssetDisposal
        {
            Id = Guid.NewGuid(),
            AssetId = Guid.NewGuid(),
            DisposalDate = DateTime.Today,
            Proceeds = 3000,
            Description = "Sold to another company"
        };

        context.FixedAssetDisposals.Add(disposal);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetAssetDisposalByIdAsync(disposal.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(disposal.Id);
        result.Proceeds.Should().Be(3000);
        result.Description.Should().Be("Sold to another company");
    }

    [Fact]
    public async Task GetAssetDisposalByIdAsync_Should_ThrowException_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AssetDisposalService(context);

        var nonExistentId = Guid.NewGuid();

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.GetAssetDisposalByIdAsync(nonExistentId)
        );
    }
}
