using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class BankAccount : BaseEntity
{
    public required string AccountName { get; set; }
    public BankAccountType AccountType { get; set; }
    public string? BankName { get; set; } // Nullable for Petty Cash
    public string? AccountNumber { get; set; } // Nullable for Petty Cash
    public string? Iban { get; set; }
    public Guid CurrencyId { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>
    /// The crucial link to the General Ledger. This is the Asset account in the CoA.
    /// </summary>
    public Guid AccountId { get; set; }
}