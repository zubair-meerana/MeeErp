using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Core.Domain.Entities;

public class Currency : BaseEntity
{
    public required string Name { get; set; } // e.g., "UAE Dirham"
    public required string Code { get; set; } // e.g., "AED"
    public required string Symbol { get; set; }
    public int DecimalPlaces { get; set; } = 2;
}