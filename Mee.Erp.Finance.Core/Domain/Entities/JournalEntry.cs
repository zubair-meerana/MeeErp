using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

/// <summary>
/// Represents a single line item within a Journal.
/// This is a debit or a credit to a specific account.
/// </summary>
public class JournalEntry : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent Journal.
    /// </summary>
    public Guid JournalId { get; set; }

    /// <summary>
    /// Foreign key to the account being affected in the Chart of Accounts.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// The debit amount. Should be 0 if Credit has a value.
    /// </summary>
    public decimal Debit { get; set; }

    /// <summary>
    /// The credit amount. Should be 0 if Debit has a value.
    /// </summary>
    public decimal Credit { get; set; }

    /// <summary>
    /// Optional description for this specific line item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional ID of the business unit (department, store) this entry is for.
    /// Essential for departmental reporting.
    /// </summary>
    public Guid? BusinessUnitId { get; set; }

    /// <summary>
    /// Optional reference to a Tax Code from a localization module.
    /// </summary>
    public Guid? TaxCodeId { get; set; }

    /// <summary>
    /// The tax rate percentage applied at the time of the transaction.
    /// Stored for historical accuracy, even if the tax code's rate changes later.
    /// </summary>
    public decimal? TaxRatePercentage { get; set; }
}