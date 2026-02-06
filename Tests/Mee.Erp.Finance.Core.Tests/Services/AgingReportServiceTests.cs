using FluentAssertions;
using Mee.Erp.Finance.Core.Contracts.Dtos;
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

public class AgingReportServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task GenerateAgingReportAsync_Should_Generate_Report()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AgingReportService(context);

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var invoices = new List<SalesInvoice>
        {
            new SalesInvoice
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                InvoiceNumber = "SINV-001",
                InvoiceDate = DateTime.Today.AddDays(-45), // 45 days old
                DueDate = DateTime.Today.AddDays(-15), // 15 days past due
                TotalAmount = 1000,
                AmountPaid = 0,
                Status = SalesInvoiceStatus.Authorized,
                CompanyId = companyId
            },
            new SalesInvoice
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                InvoiceNumber = "SINV-002",
                InvoiceDate = DateTime.Today.AddDays(-10), // 10 days old
                DueDate = DateTime.Today.AddDays(20), // Not yet due
                TotalAmount = 500,
                AmountPaid = 0,
                Status = SalesInvoiceStatus.Authorized,
                CompanyId = companyId
            },
            new SalesInvoice
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                InvoiceNumber = "SINV-003",
                InvoiceDate = DateTime.Today.AddDays(-90), // 90 days old
                DueDate = DateTime.Today.AddDays(-60), // 60 days past due
                TotalAmount = 2000,
                AmountPaid = 0,
                Status = SalesInvoiceStatus.Authorized,
                CompanyId = companyId
            }
        };

        context.SalesInvoices.AddRange(invoices);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GenerateAgingReportAsync(companyId);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.CustomerAgingData.Should().NotBeEmpty();
        result.CustomerAgingData.Should().ContainKey(customerId);

        var customerData = result.CustomerAgingData[customerId];
        customerData.CustomerId.Should().Be(customerId);
        customerData.Current.Should().Be(500); // SINV-002 (not yet due)
        customerData.Days30.Should().Be(1000); // SINV-001 (15 days past due)
        customerData.Days60.Should().Be(0); // No invoices in 31-60 day range
        customerData.Days90.Should().Be(2000); // SINV-003 (60 days past due)
        customerData.Over90.Should().Be(0); // No invoices over 90 days past due
    }

    [Fact]
    public async Task GenerateAgingReportAsync_Should_Handle_Empty_Data()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AgingReportService(context);

        var companyId = Guid.NewGuid();

        // No invoices in the database

        // 2. ACT
        var result = await service.GenerateAgingReportAsync(companyId);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.CustomerAgingData.Should().BeEmpty();
        result.ReportDate.Should().BeCloseTo(DateTime.Today, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GenerateAgingReportAsync_Should_Filter_By_Company()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AgingReportService(context);

        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();

        var invoices = new List<SalesInvoice>
        {
            new SalesInvoice
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                InvoiceNumber = "SINV-001",
                InvoiceDate = DateTime.Today.AddDays(-45),
                DueDate = DateTime.Today.AddDays(-15),
                TotalAmount = 1000,
                AmountPaid = 0,
                Status = SalesInvoiceStatus.Authorized,
                CompanyId = companyId
            },
            new SalesInvoice
            {
                Id = Guid.NewGuid(),
                CustomerId = otherCustomerId,
                InvoiceNumber = "SINV-002",
                InvoiceDate = DateTime.Today.AddDays(-45),
                DueDate = DateTime.Today.AddDays(-15),
                TotalAmount = 500,
                AmountPaid = 0,
                Status = SalesInvoiceStatus.Authorized,
                CompanyId = otherCompanyId
            }
        };

        context.SalesInvoices.AddRange(invoices);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GenerateAgingReportAsync(companyId);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.CustomerAgingData.Should().HaveCount(1); // Only one customer for this company
        result.CustomerAgingData.Should().ContainKey(customerId);
        result.CustomerAgingData.Should().NotContainKey(otherCustomerId);
    }

    [Fact]
    public async Task GetAgingSummaryAsync_Should_Return_Summary()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AgingReportService(context);

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var invoices = new List<SalesInvoice>
        {
            new SalesInvoice
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                InvoiceNumber = "SINV-001",
                InvoiceDate = DateTime.Today.AddDays(-45),
                DueDate = DateTime.Today.AddDays(-15),
                TotalAmount = 1000,
                AmountPaid = 0,
                Status = SalesInvoiceStatus.Authorized,
                CompanyId = companyId
            },
            new SalesInvoice
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                InvoiceNumber = "SINV-002",
                InvoiceDate = DateTime.Today.AddDays(-90),
                DueDate = DateTime.Today.AddDays(-60),
                TotalAmount = 2000,
                AmountPaid = 0,
                Status = SalesInvoiceStatus.Authorized,
                CompanyId = companyId
            }
        };

        context.SalesInvoices.AddRange(invoices);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetAgingSummaryAsync(companyId);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.TotalOutstanding.Should().Be(3000); // 1000 + 2000
        result.Current.Should().Be(0); // No current amounts
        result.Days30.Should().Be(1000); // SINV-001
        result.Days60.Should().Be(0); // No amounts in 31-60 range
        result.Days90.Should().Be(2000); // SINV-002
        result.Over90.Should().Be(0); // No amounts over 90 days
    }
}
