using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Shared.Kernel.Domain.Entities;
using System.Collections.Generic;

namespace Mee.Erp.Finance.Core.Domain.Entities;

/// <summary>
/// Represents the header of a general journal voucher.
/// It groups a set of balanced debit and credit entries.
/// </summary>
public class Journal : BaseEntity
{
    /// <summary>
    /// The effective date of the financial transaction.
    /// </summary>
    public DateTime JournalDate { get; set; }

    /// <summary>
    /// A general description of the transaction's purpose.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// An optional external reference number (e.g., invoice number, cheque number).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// The current status of the journal (Draft, Posted).
    /// </summary>
    public JournalStatus Status { get; set; } = JournalStatus.Draft;

    /// <summary>
    /// The ID of the company this journal belongs to.
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// The collection of individual entry lines for this journal.
    /// </summary>
    public ICollection<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
}