using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

/// <summary>
/// Represents a single account in a company's Chart of Accounts.
/// </summary>
public class Account : BaseEntity
{
    /// <summary>
    /// The unique number or code for the account (e.g., "10100").
    /// </summary>
    public required string AccountNumber { get; set; }

    /// <summary>
    /// The name of the account (e.g., "Cash in Bank").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// An optional description for more details about the account's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The fundamental type of the account (Asset, Liability, etc.).
    /// </summary>
    public AccountType AccountType { get; set; }

    /// <summary>
    /// Indicates whether transactions can be posted to this account.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// The ID of the company this account belongs to. A CoA is company-specific.
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// The parent account's ID, used to create a hierarchical chart of accounts.
    /// Null for top-level (control) accounts.
    /// </summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>
    /// An optional override for the accounting method at the account level.
    /// If null, the system will use the Business Unit's or Company's default method.
    /// </summary>
    public AccountingMethod? AccountingMethod { get; set; }
}