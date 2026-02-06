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

public class AccountsPayableServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task CreatePurchaseInvoiceAsync_Should_Create_Invoice()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsPayableService(context);

        var companyId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var invoice = new PurchaseInvoice
        {
            SupplierId = supplierId,
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            TotalAmount = 1000,
            AmountPaid = 0,
            Status = PurchaseInvoiceStatus.Draft,
            CompanyId = companyId,
            Lines = new List<PurchaseInvoiceLine>
            {
                new PurchaseInvoiceLine
                {
                    Description = "Test Item",
                    Quantity = 1,
                    UnitPrice = 1000,
                    LineTotal = 1000
                }
            }
        };

        // 2. ACT
        var result = await service.CreatePurchaseInvoiceAsync(invoice);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.InvoiceNumber.Should().Be("INV-001");
        result.TotalAmount.Should().Be(1000);
        result.Status.Should().Be(PurchaseInvoiceStatus.Draft);
        result.Lines.Should().HaveCount(1);

        var savedInvoice = await context.PurchaseInvoices.FindAsync(result.Id);
        savedInvoice.Should().NotBeNull();
        savedInvoice.InvoiceNumber.Should().Be("INV-001");
    }

    [Fact]
    public async Task GetPurchaseInvoiceByIdAsync_Should_Return_Invoice_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsPayableService(context);

        var invoice = new PurchaseInvoice
        {
            Id = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            TotalAmount = 1000,
            AmountPaid = 0,
            Status = PurchaseInvoiceStatus.Draft,
            CompanyId = Guid.NewGuid()
        };

        context.PurchaseInvoices.Add(invoice);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetPurchaseInvoiceByIdAsync(invoice.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(invoice.Id);
        result.InvoiceNumber.Should().Be("INV-001");
        result.TotalAmount.Should().Be(1000);
    }

    [Fact]
    public async Task GetPurchaseInvoiceByIdAsync_Should_ThrowException_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsPayableService(context);

        var nonExistentId = Guid.NewGuid();

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.GetPurchaseInvoiceByIdAsync(nonExistentId)
        );
    }

    [Fact]
    public async Task GetPurchaseInvoicesBySupplierAsync_Should_Return_Invoices()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsPayableService(context);

        var supplierId = Guid.NewGuid();
        var otherSupplierId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var invoices = new List<PurchaseInvoice>
        {
            new PurchaseInvoice { Id = Guid.NewGuid(), SupplierId = supplierId, InvoiceNumber = "INV-001", TotalAmount = 1000, CompanyId = companyId, Status = PurchaseInvoiceStatus.Draft },
            new PurchaseInvoice { Id = Guid.NewGuid(), SupplierId = supplierId, InvoiceNumber = "INV-002", TotalAmount = 2000, CompanyId = companyId, Status = PurchaseInvoiceStatus.Paid },
            new PurchaseInvoice { Id = Guid.NewGuid(), SupplierId = otherSupplierId, InvoiceNumber = "INV-003", TotalAmount = 3000, CompanyId = companyId, Status = PurchaseInvoiceStatus.Draft }
        };

        context.PurchaseInvoices.AddRange(invoices);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetPurchaseInvoicesBySupplierAsync(supplierId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(inv => inv.InvoiceNumber == "INV-001");
        result.Should().ContainSingle(inv => inv.InvoiceNumber == "INV-002");
        result.Should().NotContain(inv => inv.InvoiceNumber == "INV-003");
    }

    [Fact]
    public async Task ProcessSupplierPaymentAsync_Should_Process_Payment()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsPayableService(context);

        var companyId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        var invoice = new PurchaseInvoice
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            TotalAmount = 1000,
            AmountPaid = 0,
            Status = PurchaseInvoiceStatus.Authorized,
            CompanyId = companyId
        };

        context.PurchaseInvoices.Add(invoice);
        await context.SaveChangesAsync();

        var payment = new SupplierPayment
        {
            SupplierId = supplierId,
            InvoiceId = invoice.Id,
            PaymentDate = DateTime.Today,
            Amount = 1000,
            PaymentMethod = "Check",
            ReferenceNumber = "CHK-001",
            CompanyId = companyId
        };

        // 2. ACT
        var result = await service.ProcessSupplierPaymentAsync(payment);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Amount.Should().Be(1000);
        result.PaymentMethod.Should().Be("Check");

        // Verify invoice status updated
        var updatedInvoice = await context.PurchaseInvoices.FindAsync(invoice.Id);
        updatedInvoice.Status.Should().Be(PurchaseInvoiceStatus.Paid);
        updatedInvoice.AmountPaid.Should().Be(1000);
    }

    [Fact]
    public async Task GetOutstandingPayablesAsync_Should_Return_Outstanding_Amounts()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountsPayableService(context);

        var companyId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        var invoices = new List<PurchaseInvoice>
        {
            new PurchaseInvoice { Id = Guid.NewGuid(), SupplierId = supplierId, InvoiceNumber = "PAID-001", TotalAmount = 1000, AmountPaid = 1000, Status = PurchaseInvoiceStatus.Paid, CompanyId = companyId },
            new PurchaseInvoice { Id = Guid.NewGuid(), SupplierId = supplierId, InvoiceNumber = "OUT-001", TotalAmount = 2000, AmountPaid = 0, Status = PurchaseInvoiceStatus.Authorized, CompanyId = companyId },
            new PurchaseInvoice { Id = Guid.NewGuid(), SupplierId = supplierId, InvoiceNumber = "OUT-002", TotalAmount = 3000, AmountPaid = 1000, Status = PurchaseInvoiceStatus.PartiallyPaid, CompanyId = companyId }
        };

        context.PurchaseInvoices.AddRange(invoices);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetOutstandingPayablesAsync(companyId);

        // 3. ASSERT
        result.Should().HaveCount(1); // Only one supplier
        result.Should().ContainKey(supplierId);

        var outstandingAmount = result[supplierId];
        outstandingAmount.Should().Be(4000); // 2000 (OUT-001) + 2000 (OUT-002 remaining)
    }
}
