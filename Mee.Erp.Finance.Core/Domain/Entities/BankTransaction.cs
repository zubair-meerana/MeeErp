using Mee.Erp.Shared.Kernel.Domain.Entities;
using System;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class BankTransaction : BaseEntity
{
    public Guid BankAccountId { get; set; }
    public DateTime TransactionDate { get; set; }
    
    /// <summary>
    /// Description of the transaction.
    /// </summary>
    public required string Description { get; set; }
    
    /// <summary>
    /// Amount deposited into the account (positive value).
    /// </summary>
    public decimal Deposit { get; set; }

    /// <summary>
    /// Amount withdrawn from the account (positive value).
    /// </summary>
    public decimal Withdrawal { get; set; }
    
    /// <summary>
    /// The source document for this transaction, for drill-down purposes.
    /// e.g., A CustomerPayment ID or SupplierPayment ID.
    /// </summary>
    public Guid? SourceDocumentId { get; set; }

    /// <summary>
    /// The ID of the bank reconciliation this transaction has been cleared on.
    /// Null if not yet reconciled.
    /// </summary>
    public Guid? BankReconciliationId { get; set; }
}