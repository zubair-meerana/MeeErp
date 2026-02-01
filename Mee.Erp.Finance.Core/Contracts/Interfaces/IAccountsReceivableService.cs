using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IAccountsReceivableService
{
	/// <summary>
	/// Validates and saves a new sales invoice in Draft status.
	/// </summary>
	Task<SalesInvoice> CreateInvoiceAsync(SalesInvoice invoice);

	/// <summary>
	/// Finalizes the invoice, generates the GL journal entries (Revenue + VAT), 
	/// and updates the customer's balance.
	/// </summary>
	Task PostInvoiceAsync(Guid invoiceId, Guid userId);

	/// <summary>
	/// Records a payment received from a customer against a specific invoice.
	/// Creates the journal entry (Debit Bank, Credit AR).
	/// </summary>
	Task ReceivePaymentAsync(Guid invoiceId, Guid bankAccountId, DateTime paymentDate, Guid userId);
}