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

public class AccountsReceivableServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task CreateSalesInvoiceAsync_Should_Create_Invoice()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsReceivableService(context);

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoice = new SalesInvoice
        {
            CustomerId = customerId,
            InvoiceNumber = "SINV-001",
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            TotalAmount = 1000,
            AmountPaid = 0,
            Status = SalesInvoiceStatus.Draft,
            CompanyId = companyId,
            Lines = new List<SalesInvoiceLine>
            {
                new SalesInvoiceLine
                {
                    Description = "Test Item",
                    Quantity = 1,
                    UnitPrice = 1000,
                    LineTotal = 1000
                }
            }
        };

        // 2. ACT
        var result = await service.CreateSalesInvoiceAsync(invoice);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.InvoiceNumber.Should().Be("SINV-001");
        result.TotalAmount.Should().Be(1000);
        result.Status.Should().Be(SalesInvoiceStatus.Draft);
        result.Lines.Should().HaveCount(1);

        var savedInvoice = await context.SalesInvoices.FindAsync(result.Id);
        savedInvoice.Should().NotBeNull();
        savedInvoice.InvoiceNumber.Should().Be("SINV-001");
    }

    [Fact]
    public async Task GetSalesInvoiceByIdAsync_Should_Return_Invoice_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsReceivableService(context);

        var invoice = new SalesInvoice
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            InvoiceNumber = "SINV-001",
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            TotalAmount = 1000,
            AmountPaid = 0,
            Status = SalesInvoiceStatus.Draft,
            CompanyId = Guid.NewGuid()
        };

        context.SalesInvoices.Add(invoice);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetSalesInvoiceByIdAsync(invoice.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(invoice.Id);
        result.InvoiceNumber.Should().Be("SINV-001");
        result.TotalAmount.Should().Be(1000);
    }

    [Fact]
    public async Task GetSalesInvoiceByIdAsync_Should_ThrowException_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsReceivableService(context);

        var nonExistentId = Guid.NewGuid();

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.GetSalesInvoiceByIdAsync(nonExistentId)
        );
    }

    [Fact]
    public async Task GetSalesInvoicesByCustomerAsync_Should_Return_Invoices()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsReceivableService(context);

        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var invoices = new List<SalesInvoice>
        {
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customerId, InvoiceNumber = "SINV-001", TotalAmount = 1000, CompanyId = companyId, Status = SalesInvoiceStatus.Draft },
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customerId, InvoiceNumber = "SINV-002", TotalAmount = 2000, CompanyId = companyId, Status = SalesInvoiceStatus.Paid },
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = otherCustomerId, InvoiceNumber = "SINV-003", TotalAmount = 3000, CompanyId = companyId, Status = SalesInvoiceStatus.Draft }
        };

        context.SalesInvoices.AddRange(invoices);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetSalesInvoicesByCustomerAsync(customerId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(inv => inv.InvoiceNumber == "SINV-001");
        result.Should().ContainSingle(inv => inv.InvoiceNumber == "SINV-002");
        result.Should().NotContain(inv => inv.InvoiceNumber == "SINV-003");
    }

    [Fact]
    public async Task ProcessCustomerPaymentAsync_Should_Process_Payment()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsReceivableService(context);

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var invoice = new SalesInvoice
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            InvoiceNumber = "SINV-001",
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            TotalAmount = 1000,
            AmountPaid = 0,
            Status = SalesInvoiceStatus.Authorized,
            CompanyId = companyId
        };

        context.SalesInvoices.Add(invoice);
        await context.SaveChangesAsync();

        var payment = new CustomerPayment
        {
            CustomerId = customerId,
            InvoiceId = invoice.Id,
            PaymentDate = DateTime.Today,
            Amount = 1000,
            PaymentMethod = "Credit Card",
            ReferenceNumber = "CC-001",
            CompanyId = companyId
        };

        // 2. ACT
        var result = await service.ProcessCustomerPaymentAsync(payment);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Amount.Should().Be(1000);
        result.PaymentMethod.Should().Be("Credit Card");

        // Verify invoice status updated
        var updatedInvoice = await context.SalesInvoices.FindAsync(invoice.Id);
        updatedInvoice.Status.Should().Be(SalesInvoiceStatus.Paid);
        updatedInvoice.AmountPaid.Should().Be(1000);
    }

    [Fact]
    public async Task GetOutstandingReceivablesAsync_Should_Return_Outstanding_Amounts()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsReceivableService(context);

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var invoices = new List<SalesInvoice>
        {
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customerId, InvoiceNumber = "PAID-001", TotalAmount = 1000, AmountPaid = 1000, Status = SalesInvoiceStatus.Paid, CompanyId = companyId },
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customerId, InvoiceNumber = "OUT-001", TotalAmount = 2000, AmountPaid = 0, Status = SalesInvoiceStatus.Authorized, CompanyId = companyId },
            new SalesInvoice { Id = Guid.NewGuid(), CustomerId = customerId, InvoiceNumber = "OUT-002", TotalAmount = 3000, AmountPaid = 1000, Status = SalesInvoiceStatus.PartiallyPaid, CompanyId = companyId }
        };

        context.SalesInvoices.AddRange(invoices);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetOutstandingReceivablesAsync(companyId);

        // 3. ASSERT
        result.Should().HaveCount(1); // Only one customer
        result.Should().ContainKey(customerId);

        var outstandingAmount = result[customerId];
        outstandingAmount.Should().Be(4000); // 2000 (OUT-001) + 2000 (OUT-002 remaining)
    }
}
