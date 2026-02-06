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

public class PartnerServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task CreateCustomerAsync_Should_Create_Customer()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new PartnerService(context);

        var companyId = Guid.NewGuid();
        var customer = new Customer
        {
            Name = "Test Customer",
            Email = "customer@test.com",
            Phone = "123-456-7890",
            Address = "123 Main St",
            TaxId = "TAX-12345",
            CompanyId = companyId
        };

        // 2. ACT
        var result = await service.CreateCustomerAsync(customer);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Customer");
        result.Email.Should().Be("customer@test.com");
        result.CompanyId.Should().Be(companyId);

        var savedCustomer = await context.Customers.FindAsync(result.Id);
        savedCustomer.Should().NotBeNull();
        savedCustomer.Name.Should().Be("Test Customer");
    }

    [Fact]
    public async Task CreateSupplierAsync_Should_Create_Supplier()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new PartnerService(context);

        var companyId = Guid.NewGuid();
        var supplier = new Supplier
        {
            Name = "Test Supplier",
            Email = "supplier@test.com",
            Phone = "098-765-4321",
            Address = "456 Market St",
            TaxId = "TAX-54321",
            CompanyId = companyId
        };

        // 2. ACT
        var result = await service.CreateSupplierAsync(supplier);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Supplier");
        result.Email.Should().Be("supplier@test.com");
        result.CompanyId.Should().Be(companyId);

        var savedSupplier = await context.Suppliers.FindAsync(result.Id);
        savedSupplier.Should().NotBeNull();
        savedSupplier.Name.Should().Be("Test Supplier");
    }

    [Fact]
    public async Task GetCustomerByIdAsync_Should_Return_Customer_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new PartnerService(context);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Test Customer",
            Email = "customer@test.com",
            CompanyId = Guid.NewGuid()
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetCustomerByIdAsync(customer.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(customer.Id);
        result.Name.Should().Be("Test Customer");
        result.Email.Should().Be("customer@test.com");
    }

    [Fact]
    public async Task GetSupplierByIdAsync_Should_Return_Supplier_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new PartnerService(context);

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier",
            Email = "supplier@test.com",
            CompanyId = Guid.NewGuid()
        };

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetSupplierByIdAsync(supplier.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(supplier.Id);
        result.Name.Should().Be("Test Supplier");
        result.Email.Should().Be("supplier@test.com");
    }

    [Fact]
    public async Task GetCustomersByCompanyAsync_Should_Return_Customers()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new PartnerService(context);

        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();

        var customers = new List<Customer>
        {
            new Customer { Id = Guid.NewGuid(), Name = "Customer 1", Email = "c1@test.com", CompanyId = companyId },
            new Customer { Id = Guid.NewGuid(), Name = "Customer 2", Email = "c2@test.com", CompanyId = companyId },
            new Customer { Id = Guid.NewGuid(), Name = "Customer 3", Email = "c3@test.com", CompanyId = otherCompanyId }
        };

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetCustomersByCompanyAsync(companyId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(c => c.Name == "Customer 1");
        result.Should().ContainSingle(c => c.Name == "Customer 2");
        result.Should().NotContain(c => c.Name == "Customer 3");
    }

    [Fact]
    public async Task GetSuppliersByCompanyAsync_Should_Return_Suppliers()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new PartnerService(context);

        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();

        var suppliers = new List<Supplier>
        {
            new Supplier { Id = Guid.NewGuid(), Name = "Supplier 1", Email = "s1@test.com", CompanyId = companyId },
            new Supplier { Id = Guid.NewGuid(), Name = "Supplier 2", Email = "s2@test.com", CompanyId = companyId },
            new Supplier { Id = Guid.NewGuid(), Name = "Supplier 3", Email = "s3@test.com", CompanyId = otherCompanyId }
        };

        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetSuppliersByCompanyAsync(companyId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(s => s.Name == "Supplier 1");
        result.Should().ContainSingle(s => s.Name == "Supplier 2");
        result.Should().NotContain(s => s.Name == "Supplier 3");
    }
}
