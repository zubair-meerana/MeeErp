using FluentAssertions;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class FixedAssetServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task CreateFixedAssetAsync_Should_Create_Asset()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FixedAssetService(context);

        var companyId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var asset = new FixedAsset
        {
            Name = "Test Equipment",
            Description = "Test equipment for production",
            SerialNumber = "SN-001",
            PurchaseDate = DateTime.Today.AddDays(-30),
            PurchasePrice = 10000,
            SalvageValue = 1000,
            UsefulLifeYears = 5,
            DepreciationMethod = DepreciationMethod.StraightLine,
            AssetCategoryId = categoryId,
            CompanyId = companyId
        };

        // 2. ACT
        var result = await service.CreateFixedAssetAsync(asset);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Equipment");
        result.PurchasePrice.Should().Be(10000);
        result.DepreciationMethod.Should().Be(DepreciationMethod.StraightLine);

        var savedAsset = await context.FixedAssets.FindAsync(result.Id);
        savedAsset.Should().NotBeNull();
        savedAsset.Name.Should().Be("Test Equipment");
    }

    [Fact]
    public async Task GetFixedAssetByIdAsync_Should_Return_Asset_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FixedAssetService(context);

        var asset = new FixedAsset
        {
            Id = Guid.NewGuid(),
            Name = "Test Equipment",
            Description = "Test equipment for production",
            SerialNumber = "SN-001",
            PurchaseDate = DateTime.Today.AddDays(-30),
            PurchasePrice = 10000,
            SalvageValue = 1000,
            UsefulLifeYears = 5,
            DepreciationMethod = DepreciationMethod.StraightLine,
            AssetCategoryId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid()
        };

        context.FixedAssets.Add(asset);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetFixedAssetByIdAsync(asset.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(asset.Id);
        result.Name.Should().Be("Test Equipment");
        result.PurchasePrice.Should().Be(10000);
    }

    [Fact]
    public async Task GetFixedAssetByIdAsync_Should_ThrowException_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FixedAssetService(context);

        var nonExistentId = Guid.NewGuid();

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.GetFixedAssetByIdAsync(nonExistentId)
        );
    }

    [Fact]
    public async Task GetFixedAssetsByCompanyAsync_Should_Return_Assets()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FixedAssetService(context);

        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();

        var assets = new List<FixedAsset>
        {
            new FixedAsset { Id = Guid.NewGuid(), Name = "Asset 1", PurchasePrice = 10000, CompanyId = companyId },
            new FixedAsset { Id = Guid.NewGuid(), Name = "Asset 2", PurchasePrice = 20000, CompanyId = companyId },
            new FixedAsset { Id = Guid.NewGuid(), Name = "Asset 3", PurchasePrice = 30000, CompanyId = otherCompanyId }
        };

        context.FixedAssets.AddRange(assets);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetFixedAssetsByCompanyAsync(companyId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(asset => asset.Name == "Asset 1");
        result.Should().ContainSingle(asset => asset.Name == "Asset 2");
        result.Should().NotContain(asset => asset.Name == "Asset 3");
    }

    [Fact]
    public async Task CalculateAssetDepreciationAsync_Should_Calculate_Depreciation()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FixedAssetService(context);

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
            CompanyId = companyId
        };

        context.FixedAssets.Add(asset);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.CalculateAssetDepreciationAsync(asset.Id, DateTime.Today);

        // 3. ASSERT
        result.Should().NotBeNull();
        // For straight-line: (10000 - 1000) / 5 = 1800 per year
        // After 1 year, accumulated depreciation should be ~1800
        result.AccumulatedDepreciation.Should().BeApproximately(1800, 10);
        result.NetBookValue.Should().BeApproximately(8200, 10); // 10000 - 1800
    }

    [Fact]
    public async Task DisposeFixedAssetAsync_Should_Dispose_Asset()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new FixedAssetService(context);

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
        var result = await service.DisposeFixedAssetAsync(asset.Id, DateTime.Today, 3000, "Sold to another company");

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(asset.Id);
        result.IsActive.Should().BeFalse(); // Asset should be marked as disposed

        var savedAsset = await context.FixedAssets.FindAsync(asset.Id);
        savedAsset.IsActive.Should().BeFalse();
    }
}
