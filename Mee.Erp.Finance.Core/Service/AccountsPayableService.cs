using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class AccountsPayableService : IAccountsPayableService
{
	private readonly FinanceDbContext _context;
	private readonly IJournalPostingService _postingService;
	private readonly ITaxCalculator _taxCalculator;

	public AccountsPayableService(
		FinanceDbContext context,
		IJournalPostingService postingService,
		ITaxCalculator taxCalculator)
	{
		_context = context;
		_postingService = postingService;
		_taxCalculator = taxCalculator;
	}

	public async Task<PurchaseInvoice> CreateInvoiceAsync(PurchaseInvoice invoice)
	{
		// Simple logic: Calculate totals and save
		invoice.Status = PurchaseInvoiceStatus.Draft;
		invoice.SubTotal = invoice.Lines.Sum(l => l.Amount);
		// Note: In real app, we'd loop lines to calc exact tax based on TaxCodes
		// For simplicity here, assuming lines are pre-calculated or we do it later

		_context.PurchaseInvoices.Add(invoice);
		await _context.SaveChangesAsync();
		return invoice;
	}

	public async Task PostInvoiceAsync(Guid invoiceId, Guid userId)
	{
		// 1. Fetch Invoice
		var invoice = await _context.PurchaseInvoices
			.Include(i => i.Lines)
			.Include(i => i.Supplier) // We need Supplier to know which AP account to use
			.FirstOrDefaultAsync(i => i.Id == invoiceId);

		if (invoice == null) throw new Exception("Invoice not found");
		if (invoice.Status != PurchaseInvoiceStatus.Draft) throw new Exception("Already posted");

		// 2. Create Journal Header
		var journal = new Journal
		{
			JournalDate = invoice.InvoiceDate,
			Description = $"Purchase Invoice: {invoice.InvoiceNumber} - {invoice.Supplier?.Name}",
			ReferenceNumber = invoice.InvoiceNumber,
			CompanyId = invoice.CompanyId,
			Status = JournalStatus.Draft,
			CreatedBy = userId
		};

		decimal totalCredits = 0;
		decimal totalDebits = 0;

		// 3. Process Lines (Debits)
		foreach (var line in invoice.Lines)
		{
			// Expense Entry
			var expenseEntry = new JournalEntry
			{
				AccountId = line.AccountId,
				Debit = line.Amount,
				Credit = 0,
				Description = line.Description,
				TaxCodeId = line.TaxCodeId,
				BusinessUnitId = null // Would come from line or header
			};
			journal.Entries.Add(expenseEntry);
			totalDebits += line.Amount;

			// Tax Logic (Simplified)
			if (line.TaxCodeId.HasValue)
			{
				// In a real scenario, we fetch the TaxCode entity to get the Rate and the GL Account
				// For this code example, let's assume 5% tax and a hardcoded VAT Account
				decimal taxAmt = line.Amount * 0.05m;

				var taxEntry = new JournalEntry
				{
					AccountId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // REPLACE with real VAT Input Account ID
					Debit = taxAmt,
					Credit = 0,
					Description = "VAT Input",
					BusinessUnitId = null
				};
				journal.Entries.Add(taxEntry);
				totalDebits += taxAmt;
			}
		}

		// 4. Create Liability Entry (Credit)
		// We credit the Supplier's default AP Account
		var apEntry = new JournalEntry
		{
			AccountId = invoice.Supplier.DefaultAccountsPayableAccountId,
			Debit = 0,
			Credit = totalDebits, // Total Liability = Cost + Tax
			Description = "Accounts Payable"
		};
		journal.Entries.Add(apEntry);

		// 5. Save and Post
		_context.Journals.Add(journal);
		await _context.SaveChangesAsync(); // Save Journal first to get an ID

		// Call the posting engine! This reuses all our validation logic.
		await _postingService.PostJournalAsync(journal.Id, userId);

		// 6. Update Invoice Status
		invoice.Status = PurchaseInvoiceStatus.AwaitingPayment;
		invoice.JournalId = journal.Id;
		await _context.SaveChangesAsync();
	}

	public async Task PayInvoiceAsync(Guid invoiceId, Guid bankAccountId, DateTime paymentDate, Guid userId)
	{
		var invoice = await _context.PurchaseInvoices
			.Include(i => i.Supplier)
			.FirstOrDefaultAsync(i => i.Id == invoiceId);

		if (invoice.Status != PurchaseInvoiceStatus.AwaitingPayment)
			throw new Exception("Invoice not ready for payment");

		// 1. Create Payment Record
		var payment = new SupplierPayment
		{
			SupplierId = invoice.SupplierId,
			PaymentDate = paymentDate,
			AmountPaid = invoice.TotalAmount,
			BankAccountId = bankAccountId,
			CompanyId = invoice.CompanyId,
			CreatedBy = userId
		};
		_context.SupplierPayments.Add(payment);

		// 2. Create Journal for Payment
		// Debit AP Control Account (Reducing Liability)
		// Credit Bank Account (Reducing Asset)
		var journal = new Journal
		{
			JournalDate = paymentDate,
			Description = $"Payment to {invoice.Supplier.Name}",
			CompanyId = invoice.CompanyId,
			Status = JournalStatus.Draft
		};

		// Debit AP
		journal.Entries.Add(new JournalEntry
		{
			AccountId = invoice.Supplier.DefaultAccountsPayableAccountId,
			Debit = invoice.TotalAmount,
			Credit = 0
		});

		// Credit Bank (We need to fetch the AccountId linked to the BankAccount)
		var bankAcct = await _context.BankAccounts.FindAsync(bankAccountId);
		journal.Entries.Add(new JournalEntry
		{
			AccountId = bankAcct.AccountId,
			Debit = 0,
			Credit = invoice.TotalAmount
		});

		_context.Journals.Add(journal);
		await _context.SaveChangesAsync();

		await _postingService.PostJournalAsync(journal.Id, userId);

		invoice.Status = PurchaseInvoiceStatus.Paid;
		await _context.SaveChangesAsync();
	}
}