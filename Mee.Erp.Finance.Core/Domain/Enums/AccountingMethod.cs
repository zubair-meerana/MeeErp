namespace Mee.Erp.Finance.Core.Domain.Enums;

/// <summary>
/// Defines the accounting method to be used.
/// </summary>
public enum AccountingMethod
{
    /// <summary>
    /// Revenue and expenses are recognized when the transaction occurs, not when cash is exchanged.
    /// </summary>
    Accrual = 1,

    /// <summary>
    /// Revenue and expenses are recognized only when cash is received or paid.
    /// </summary>
    Cash = 2
}