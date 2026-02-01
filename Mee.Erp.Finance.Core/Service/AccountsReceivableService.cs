using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class AccountsReceivableService : IAccountsReceivableService
{
	private readonly FinanceDbContext _context;
	private readonly IJournalPostingService _postingService;

	public AccountsReceivableService(FinanceDbContext context, IJournalPostingService postingService)
	{
		_context = context;
		_postingService = postingService;
	}

	// --- MISSING IMPLEMENTATION 1: CreateInvoiceAsync ---
	public async Task<SalesInvoice> CreateInvoiceAsync(SalesInvoice invoice)
	{
		// 1. Set initial status
		invoice.Status = SalesInvoiceStatus.Draft;

		// 2. Calculate Totals (Simple version)
		// In a real scenario, you would calculate Tax per line here using ITaxCalculator
		invoice.SubTotal = invoice.Lines.Sum(l => l.Amount);

		// Assuming 5% tax for simplicity in this calculation, 
		// normally this comes from the TaxCode on each line.
		invoice.TaxAmount = invoice.SubTotal * 0.05m;
		invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;

		_context.SalesInvoices.Add(invoice);
		await _context.SaveChangesAsync();
		return invoice;
	}

	// --- Existing Implementation: PostInvoiceAsync ---
	public async Task PostInvoiceAsync(Guid invoiceId, Guid userId)
	{
		var invoice = await _context.SalesInvoices
			.Include(i => i.Lines)
			.Include(i => i.Customer)
			.FirstOrDefaultAsync(i => i.Id == invoiceId);

		if (invoice == null) throw new Exception("Invoice not found");
		if (invoice.Status != SalesInvoiceStatus.Draft) throw new Exception("Invoice must be in Draft status to post.");

		var journal = new Journal
		{
			JournalDate = invoice.InvoiceDate,
			Description = $"Sales Invoice: {invoice.InvoiceNumber} - {invoice.Customer.Name}",
			ReferenceNumber = invoice.InvoiceNumber,
			CompanyId = invoice.CompanyId,
			Status = JournalStatus.Draft,
			CreatedBy = userId
		};

		decimal totalRevenue = 0;

		// 1. Revenue Lines (Credits)
		foreach (var line in invoice.Lines)
		{
			journal.Entries.Add(new JournalEntry
			{
				AccountId = line.AccountId, // Revenue Account
				Debit = 0,
				Credit = line.Amount,
				Description = line.Description,
				TaxCodeId = line.TaxCodeId,
				BusinessUnitId = null // Could be passed from line
			});
			totalRevenue += line.Amount;
		}

		// 2. Tax (Credit VAT Payable)
		// Ideally calculated per line, simplified here
		decimal taxAmt = totalRevenue * 0.05m;
		if (taxAmt > 0)
		{
			// Placeholder for "VAT Output" Account ID
			var vatAccountId = Guid.Parse("00000000-0000-0000-0000-000000000002");

			journal.Entries.Add(new JournalEntry
			{
				AccountId = vatAccountId,
				Debit = 0,
				Credit = taxAmt,
				Description = "VAT Output"
			});
		}

		// 3. AR Control (Debit)
		journal.Entries.Add(new JournalEntry
		{
			AccountId = invoice.Customer.DefaultAccountsReceivableAccountId,
			Debit = totalRevenue + taxAmt,
			Credit = 0,
			Description = "Accounts Receivable"
		});

		_context.Journals.Add(journal);
		await _context.SaveChangesAsync();

		// Use the Posting Service to validate and commit to Ledger
		await _postingService.PostJournalAsync(journal.Id, userId);

		invoice.Status = SalesInvoiceStatus.AwaitingPayment;
		invoice.JournalId = journal.Id;
		await _context.SaveChangesAsync();
	}

	// --- MISSING IMPLEMENTATION 2: ReceivePaymentAsync ---
	public async Task ReceivePaymentAsync(Guid invoiceId, Guid bankAccountId, DateTime paymentDate, Guid userId)
	{
		var invoice = await _context.SalesInvoices
			.Include(i => i.Customer)
			.FirstOrDefaultAsync(i => i.Id == invoiceId);

		if (invoice == null) throw new Exception("Invoice not found");
		if (invoice.Status != SalesInvoiceStatus.AwaitingPayment)
			throw new Exception("Invoice is not awaiting payment.");

		// 1. Record the Customer Payment entity
		var payment = new CustomerPayment
		{
			CustomerId = invoice.CustomerId,
			PaymentDate = paymentDate,
			AmountReceived = invoice.TotalAmount,
			BankAccountId = bankAccountId,
			CompanyId = invoice.CompanyId,
			CreatedBy = userId
		};
		_context.CustomerPayments.Add(payment);

		// 2. Create the Journal Entry
		// Debit: Bank (Asset increases)
		// Credit: Accounts Receivable (Asset decreases/Client debt cleared)
		var journal = new Journal
		{
			JournalDate = paymentDate,
			Description = $"Payment from {invoice.Customer.Name}",
			ReferenceNumber = invoice.InvoiceNumber,
			CompanyId = invoice.CompanyId,
			Status = JournalStatus.Draft,
			CreatedBy = userId
		};

		// Fetch Bank Account to get the GL Account ID
		var bankAcct = await _context.BankAccounts.FindAsync(bankAccountId);
		if (bankAcct == null) throw new Exception("Bank Account not found");

		// Debit Bank
		journal.Entries.Add(new JournalEntry
		{
			AccountId = bankAcct.AccountId,
			Debit = invoice.TotalAmount,
			Credit = 0,
			Description = "Payment Received"
		});

		// Credit AR
		journal.Entries.Add(new JournalEntry
		{
			AccountId = invoice.Customer.DefaultAccountsReceivableAccountId,
			Debit = 0,
			Credit = invoice.TotalAmount,
			Description = "AR Settlement"
		});

		_context.Journals.Add(journal);
		await _context.SaveChangesAsync();

		// Post to Ledger
		await _postingService.PostJournalAsync(journal.Id, userId);

		// 3. Update Invoice Status
		invoice.Status = SalesInvoiceStatus.Paid;
		await _context.SaveChangesAsync();
	}
}