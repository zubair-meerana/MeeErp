using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class CreditDebitNoteService : ICreditDebitNoteService
{
    private readonly FinanceDbContext _context;
    private readonly IJournalPostingService _journalPostingService;
    private readonly IAccountService _accountService;

    public CreditDebitNoteService(FinanceDbContext context, IJournalPostingService journalPostingService, IAccountService accountService)
    {
        _context = context;
        _journalPostingService = journalPostingService;
        _accountService = accountService;
    }

    public async Task<CreditNote> CreateCreditNoteAsync(CreditNote creditNote)
    {
        creditNote.Status = CreditNoteStatus.Draft;
        creditNote.CreditNoteDate = DateTime.UtcNow;
        
        // Generate credit note number
        creditNote.CreditNoteNumber = await GenerateCreditNoteNumberAsync(creditNote.CompanyId);
        
        // Calculate totals
        CalculateCreditNoteTotals(creditNote);
        
        _context.CreditNotes.Add(creditNote);
        await _context.SaveChangesAsync();
        
        return creditNote;
    }

    public async Task<CreditNote> PostCreditNoteAsync(Guid creditNoteId)
    {
        var creditNote = await _context.CreditNotes
            .Include(cn => cn.Lines)
            .ThenInclude(l => l.Account)
            .Include(cn => cn.Customer)
            .FirstOrDefaultAsync(cn => cn.Id == creditNoteId)
            ?? throw new ArgumentException("Credit note not found");

        if (creditNote.Status != CreditNoteStatus.Draft)
        {
            throw new InvalidOperationException("Only draft credit notes can be posted");
        }

        // Get customer's receivable account
        var receivableAccount = await _accountService.GetAccountByIdAsync(creditNote.Customer.DefaultAccountsReceivableAccountId)
            ?? throw new InvalidOperationException($"Default receivable account not found for customer {creditNote.Customer.Name}");

        // Create journal entries
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            Description = $"Credit Note {creditNote.CreditNoteNumber} for {creditNote.Customer.Name}",
            JournalDate = creditNote.CreditNoteDate,
            Status = JournalStatus.Draft,
            CompanyId = creditNote.CompanyId
        };

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        // Credit revenue accounts and debit receivable account
        foreach (var line in creditNote.Lines)
        {
            // Credit the original revenue account
            _context.JournalEntries.Add(new JournalEntry
            {
                Id = Guid.NewGuid(),
                JournalId = journal.Id,
                AccountId = line.AccountId,
                Description = line.Description,
                Debit = 0,
                Credit = line.LineAmount + line.TaxAmount
            });

            // Debit the customer's receivable account
            _context.JournalEntries.Add(new JournalEntry
            {
                Id = Guid.NewGuid(),
                JournalId = journal.Id,
                AccountId = receivableAccount.Id,
                Description = $"Credit note {creditNote.CreditNoteNumber}",
                Debit = line.LineAmount + line.TaxAmount,
                Credit = 0
            });
        }

        // Post the journal
        await _journalPostingService.PostJournalAsync(journal.Id, Guid.NewGuid()); // TODO: Get actual user ID
        
        creditNote.JournalId = journal.Id;
        creditNote.Status = CreditNoteStatus.Posted;
        
        await _context.SaveChangesAsync();
        return creditNote;
    }

    public async Task<DebitNote> CreateDebitNoteAsync(DebitNote debitNote)
    {
        debitNote.Status = DebitNoteStatus.Draft;
        debitNote.DebitNoteDate = DateTime.UtcNow;
        
        // Generate debit note number
        debitNote.DebitNoteNumber = await GenerateDebitNoteNumberAsync(debitNote.CompanyId);
        
        // Calculate totals
        CalculateDebitNoteTotals(debitNote);
        
        _context.DebitNotes.Add(debitNote);
        await _context.SaveChangesAsync();
        
        return debitNote;
    }

    public async Task<DebitNote> PostDebitNoteAsync(Guid debitNoteId)
    {
        var debitNote = await _context.DebitNotes
            .Include(dn => dn.Lines)
            .ThenInclude(l => l.Account)
            .Include(dn => dn.Supplier)
            .FirstOrDefaultAsync(dn => dn.Id == debitNoteId)
            ?? throw new ArgumentException("Debit note not found");

        if (debitNote.Status != DebitNoteStatus.Draft)
        {
            throw new InvalidOperationException("Only draft debit notes can be posted");
        }

        // Get supplier's payable account
        var payableAccount = await _accountService.GetAccountByIdAsync(debitNote.Supplier.DefaultAccountsPayableAccountId)
            ?? throw new InvalidOperationException($"Default payable account not found for supplier {debitNote.Supplier.Name}");

        // Create journal entries
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            Description = $"Debit Note {debitNote.DebitNoteNumber} for {debitNote.Supplier.Name}",
            JournalDate = debitNote.DebitNoteDate,
            Status = JournalStatus.Draft,
            CompanyId = debitNote.CompanyId
        };

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        // Debit expense accounts and credit payable account
        foreach (var line in debitNote.Lines)
        {
            // Debit the original expense account
            _context.JournalEntries.Add(new JournalEntry
            {
                Id = Guid.NewGuid(),
                JournalId = journal.Id,
                AccountId = line.AccountId,
                Description = line.Description,
                Debit = line.LineAmount + line.TaxAmount,
                Credit = 0
            });

            // Credit the supplier's payable account
            _context.JournalEntries.Add(new JournalEntry
            {
                Id = Guid.NewGuid(),
                JournalId = journal.Id,
                AccountId = payableAccount.Id,
                Description = $"Debit note {debitNote.DebitNoteNumber}",
                Debit = 0,
                Credit = line.LineAmount + line.TaxAmount
            });
        }

        // Post the journal
        await _journalPostingService.PostJournalAsync(journal.Id, Guid.NewGuid()); // TODO: Get actual user ID
        
        debitNote.JournalId = journal.Id;
        debitNote.Status = DebitNoteStatus.Posted;
        
        await _context.SaveChangesAsync();
        return debitNote;
    }

    public async Task<CreditNote> ApplyCreditNoteToInvoiceAsync(Guid creditNoteId, Guid invoiceId, decimal amount)
    {
        var creditNote = await _context.CreditNotes
            .FirstOrDefaultAsync(cn => cn.Id == creditNoteId)
            ?? throw new ArgumentException("Credit note not found");

        var invoice = await _context.SalesInvoices
            .FirstOrDefaultAsync(si => si.Id == invoiceId)
            ?? throw new ArgumentException("Invoice not found");

        if (creditNote.Status != CreditNoteStatus.Posted)
        {
            throw new InvalidOperationException("Credit note must be posted before applying to invoice");
        }

        if (amount > creditNote.TotalAmount)
        {
            throw new InvalidOperationException("Application amount exceeds credit note total");
        }

        // In a full implementation, you would create a payment-invoice application record
        // For now, we'll update the credit note status
        creditNote.Status = CreditNoteStatus.Applied;
        
        await _context.SaveChangesAsync();
        return creditNote;
    }

    public async Task<DebitNote> ApplyDebitNoteToInvoiceAsync(Guid debitNoteId, Guid invoiceId, decimal amount)
    {
        var debitNote = await _context.DebitNotes
            .FirstOrDefaultAsync(dn => dn.Id == debitNoteId)
            ?? throw new ArgumentException("Debit note not found");

        var invoice = await _context.PurchaseInvoices
            .FirstOrDefaultAsync(pi => pi.Id == invoiceId)
            ?? throw new ArgumentException("Invoice not found");

        if (debitNote.Status != DebitNoteStatus.Posted)
        {
            throw new InvalidOperationException("Debit note must be posted before applying to invoice");
        }

        if (amount > debitNote.TotalAmount)
        {
            throw new InvalidOperationException("Application amount exceeds debit note total");
        }

        // In a full implementation, you would create a payment-invoice application record
        // For now, we'll update the debit note status
        debitNote.Status = DebitNoteStatus.Applied;
        
        await _context.SaveChangesAsync();
        return debitNote;
    }

    private async Task<string> GenerateCreditNoteNumberAsync(Guid companyId)
    {
        var prefix = "CN";
        var year = DateTime.UtcNow.Year;
        var count = await _context.CreditNotes
            .CountAsync(cn => cn.CompanyId == companyId && cn.CreditNoteDate.Year == year) + 1;
        
        return $"{prefix}{year}{count:D5}";
    }

    private async Task<string> GenerateDebitNoteNumberAsync(Guid companyId)
    {
        var prefix = "DN";
        var year = DateTime.UtcNow.Year;
        var count = await _context.DebitNotes
            .CountAsync(dn => dn.CompanyId == companyId && dn.DebitNoteDate.Year == year) + 1;
        
        return $"{prefix}{year}{count:D5}";
    }

    private void CalculateCreditNoteTotals(CreditNote creditNote)
    {
        decimal subTotal = 0;
        decimal taxTotal = 0;

        foreach (var line in creditNote.Lines)
        {
            line.LineAmount = line.Quantity * line.UnitPrice;
            line.TaxAmount = line.LineAmount * (line.TaxRate / 100);
            subTotal += line.LineAmount;
            taxTotal += line.TaxAmount;
        }

        creditNote.SubTotal = subTotal;
        creditNote.TaxAmount = taxTotal;
        creditNote.TotalAmount = subTotal + taxTotal;
    }

    private void CalculateDebitNoteTotals(DebitNote debitNote)
    {
        decimal subTotal = 0;
        decimal taxTotal = 0;

        foreach (var line in debitNote.Lines)
        {
            line.LineAmount = line.Quantity * line.UnitPrice;
            line.TaxAmount = line.LineAmount * (line.TaxRate / 100);
            subTotal += line.LineAmount;
            taxTotal += line.TaxAmount;
        }

        debitNote.SubTotal = subTotal;
        debitNote.TaxAmount = taxTotal;
        debitNote.TotalAmount = subTotal + taxTotal;
    }
}