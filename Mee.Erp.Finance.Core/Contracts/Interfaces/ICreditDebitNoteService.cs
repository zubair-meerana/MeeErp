using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface ICreditDebitNoteService
{
    Task<CreditNote> CreateCreditNoteAsync(CreditNote creditNote);
    Task<CreditNote> PostCreditNoteAsync(Guid creditNoteId);
    Task<DebitNote> CreateDebitNoteAsync(DebitNote debitNote);
    Task<DebitNote> PostDebitNoteAsync(Guid debitNoteId);
    Task<CreditNote> ApplyCreditNoteToInvoiceAsync(Guid creditNoteId, Guid invoiceId, decimal amount);
    Task<DebitNote> ApplyDebitNoteToInvoiceAsync(Guid debitNoteId, Guid invoiceId, decimal amount);
}