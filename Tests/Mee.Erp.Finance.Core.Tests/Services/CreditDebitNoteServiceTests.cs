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

public class CreditDebitNoteServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_Should_Create_Credit_Note()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CreditDebitNoteService(context);

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var creditNote = new CreditNote
        {
            CustomerId = customerId,
            NoteNumber = "CN-001",
            IssueDate = DateTime.Today,
            TotalAmount = 500,
            Description = "Return of defective goods",
            CompanyId = companyId,
            Lines = new List<CreditNoteLine>
            {
                new CreditNoteLine
                {
                    Description = "Returned Item",
                    Quantity = 1,
                    UnitPrice = 500,
                    LineTotal = 500
                }
            }
        };

        // 2. ACT
        var result = await service.CreateCreditNoteAsync(creditNote);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.NoteNumber.Should().Be("CN-001");
        result.TotalAmount.Should().Be(500);
        result.Description.Should().Be("Return of defective goods");
        result.Lines.Should().HaveCount(1);

        var savedNote = await context.CreditNotes.FindAsync(result.Id);
        savedNote.Should().NotBeNull();
        savedNote.NoteNumber.Should().Be("CN-001");
    }

    [Fact]
    public async Task CreateDebitNoteAsync_Should_Create_Debit_Note()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CreditDebitNoteService(context);

        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var debitNote = new DebitNote
        {
            CustomerId = customerId,
            NoteNumber = "DN-001",
            IssueDate = DateTime.Today,
            TotalAmount = 250,
            Description = "Additional charges",
            CompanyId = companyId,
            Lines = new List<DebitNoteLine>
            {
                new DebitNoteLine
                {
                    Description = "Shipping charges",
                    Quantity = 1,
                    UnitPrice = 250,
                    LineTotal = 250
                }
            }
        };

        // 2. ACT
        var result = await service.CreateDebitNoteAsync(debitNote);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.NoteNumber.Should().Be("DN-001");
        result.TotalAmount.Should().Be(250);
        result.Description.Should().Be("Additional charges");
        result.Lines.Should().HaveCount(1);

        var savedNote = await context.DebitNotes.FindAsync(result.Id);
        savedNote.Should().NotBeNull();
        savedNote.NoteNumber.Should().Be("DN-001");
    }

    [Fact]
    public async Task GetCreditNoteByIdAsync_Should_Return_Credit_Note_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CreditDebitNoteService(context);

        var creditNote = new CreditNote
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            NoteNumber = "CN-001",
            IssueDate = DateTime.Today,
            TotalAmount = 500,
            Description = "Return of defective goods",
            CompanyId = Guid.NewGuid()
        };

        context.CreditNotes.Add(creditNote);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetCreditNoteByIdAsync(creditNote.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(creditNote.Id);
        result.NoteNumber.Should().Be("CN-001");
        result.TotalAmount.Should().Be(500);
    }

    [Fact]
    public async Task GetDebitNoteByIdAsync_Should_Return_Debit_Note_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CreditDebitNoteService(context);

        var debitNote = new DebitNote
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            NoteNumber = "DN-001",
            IssueDate = DateTime.Today,
            TotalAmount = 250,
            Description = "Additional charges",
            CompanyId = Guid.NewGuid()
        };

        context.DebitNotes.Add(debitNote);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetDebitNoteByIdAsync(debitNote.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(debitNote.Id);
        result.NoteNumber.Should().Be("DN-001");
        result.TotalAmount.Should().Be(250);
    }

    [Fact]
    public async Task GetCreditNotesByCustomerAsync_Should_Return_Credit_Notes()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CreditDebitNoteService(context);

        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var creditNotes = new List<CreditNote>
        {
            new CreditNote { Id = Guid.NewGuid(), CustomerId = customerId, NoteNumber = "CN-001", TotalAmount = 500, CompanyId = companyId },
            new CreditNote { Id = Guid.NewGuid(), CustomerId = customerId, NoteNumber = "CN-002", TotalAmount = 750, CompanyId = companyId },
            new CreditNote { Id = Guid.NewGuid(), CustomerId = otherCustomerId, NoteNumber = "CN-003", TotalAmount = 1000, CompanyId = companyId }
        };

        context.CreditNotes.AddRange(creditNotes);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetCreditNotesByCustomerAsync(customerId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(note => note.NoteNumber == "CN-001");
        result.Should().ContainSingle(note => note.NoteNumber == "CN-002");
        result.Should().NotContain(note => note.NoteNumber == "CN-003");
    }

    [Fact]
    public async Task GetDebitNotesByCustomerAsync_Should_Return_Debit_Notes()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new CreditDebitNoteService(context);

        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var debitNotes = new List<DebitNote>
        {
            new DebitNote { Id = Guid.NewGuid(), CustomerId = customerId, NoteNumber = "DN-001", TotalAmount = 250, CompanyId = companyId },
            new DebitNote { Id = Guid.NewGuid(), CustomerId = customerId, NoteNumber = "DN-002", TotalAmount = 350, CompanyId = companyId },
            new DebitNote { Id = Guid.NewGuid(), CustomerId = otherCustomerId, NoteNumber = "DN-003", TotalAmount = 450, CompanyId = companyId }
        };

        context.DebitNotes.AddRange(debitNotes);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetDebitNotesByCustomerAsync(customerId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(note => note.NoteNumber == "DN-001");
        result.Should().ContainSingle(note => note.NoteNumber == "DN-002");
        result.Should().NotContain(note => note.NoteNumber == "DN-003");
    }
}
