using Mee.Erp.Shared.Kernel.Domain.Entities;
using System;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class CustomerPayment : BaseEntity
{
    public Guid CustomerId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal AmountReceived { get; set; }
    public string? Reference { get; set; } // e.g., Deposit reference

    /// <summary>
    /// The Bank or Cash account the payment was received into.
    /// </summary>
    public Guid BankAccountId { get; set; }
    public Guid CompanyId { get; set; }
}