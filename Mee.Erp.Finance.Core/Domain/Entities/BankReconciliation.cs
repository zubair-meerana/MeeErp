using Mee.Erp.Shared.Kernel.Domain.Entities;
using System;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class BankReconciliation : BaseEntity
{
    public Guid BankAccountId { get; set; }
    public DateTime StatementDate { get; set; }
    public string? Reference { get; set; }

    /// <summary>
    /// The closing balance from the official bank statement.
    /// </summary>
    public decimal StatementEndingBalance { get; set; }
    
    /// <summary>
    /// The closing balance calculated from the transactions in the ERP.
    /// </summary>
    public decimal ErpEndingBalance { get; set; }

    public bool IsReconciled => StatementEndingBalance == ErpEndingBalance;
}