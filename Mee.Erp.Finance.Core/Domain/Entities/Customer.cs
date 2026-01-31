using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class Customer : BaseEntity
{
    public required string Name { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public Guid CompanyId { get; set; }

    // Default account for posting this customer's receivables.
    // This will typically be your main "Accounts Receivable" account.
    public Guid DefaultAccountsReceivableAccountId { get; set; }
}