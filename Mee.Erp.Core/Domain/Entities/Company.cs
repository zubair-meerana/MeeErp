using Mee.Erp.Core.Domain.Enums;
using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Core.Domain.Entities;

public class Company : BaseEntity
{
    public required string Name { get; set; }
    public string? LegalName { get; set; }
    public string? TaxRegistrationNumber { get; set; } // e.g., UAE TRN
    public Guid BaseCurrencyId { get; set; }
    public OperatingMode OperatingMode { get; set; }
}