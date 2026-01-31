using Mee.Erp.Shared.Kernel.Domain.Entities;
using System;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class SupplierPayment : BaseEntity
{
    public Guid SupplierId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal AmountPaid { get; set; }
    public string? Reference { get; set; } // e.g., Cheque number

    /// <summary>
    /// The Bank or Cash account the payment was made from.
    /// </summary>
    public Guid BankAccountId { get; set; }
    public Guid CompanyId { get; set; }
    
    // In a full implementation, you'd have a linking table here
    // to track which invoices this payment was applied to.
}