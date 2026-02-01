using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IAccountsPayableService
{
	Task<PurchaseInvoice> CreateInvoiceAsync(PurchaseInvoice invoice);
	Task PostInvoiceAsync(Guid invoiceId, Guid userId);
	Task PayInvoiceAsync(Guid invoiceId, Guid bankAccountId, DateTime paymentDate, Guid userId);
}