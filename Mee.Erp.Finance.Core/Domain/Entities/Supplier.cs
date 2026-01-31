using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class Supplier : BaseEntity
{
    public required string Name { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public Guid CompanyId { get; set; }
    
    // Default account for posting this supplier's liabilities.
    // This will typically be your main "Accounts Payable" account.
    public Guid DefaultAccountsPayableAccountId { get; set; }
}