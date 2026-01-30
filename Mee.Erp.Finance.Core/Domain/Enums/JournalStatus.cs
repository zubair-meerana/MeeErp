namespace Mee.Erp.Finance.Core.Domain.Enums;

/// <summary>
/// Represents the status of a journal, controlling whether it affects the general ledger.
/// </summary>
public enum JournalStatus
{
    /// <summary>
    /// The journal is a work-in-progress and has no financial impact. It can be edited.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// The journal has been finalized and its entries are posted to the general ledger. It cannot be edited.
    /// </summary>
    Posted = 2,

    /// <summary>
    /// The journal has been cancelled and has no financial impact. A posted journal cannot be cancelled; it must be reversed.
    /// </summary>
    Cancelled = 3
}