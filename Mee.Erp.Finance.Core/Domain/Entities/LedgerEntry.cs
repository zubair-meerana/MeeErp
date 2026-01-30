using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

/// <summary>
/// Represents a single, immutable entry in the General Ledger.
/// This is the permanent financial record created after a journal is posted.
/// </summary>
public class LedgerEntry : BaseEntity
{
    /// <summary>
    /// The effective date of the entry.
    /// </summary>
    public DateTime EntryDate { get; set; }

    /// <summary>
    /// The account that was affected.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// The business unit (department/store) associated with this entry.
    /// </summary>
    public Guid? BusinessUnitId { get; set; }

    /// <summary>
    /// The debit amount.
    /// </summary>
    public decimal Debit { get; set; }

    /// <summary>
    /// The credit amount.
    /// </summary>
    public decimal Credit { get; set; }
    
    /// <summary>
    /// A copy of the description for reporting purposes.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// A reference back to the source Journal for auditing.
    /// </summary>
    public Guid JournalId { get; set; }
}