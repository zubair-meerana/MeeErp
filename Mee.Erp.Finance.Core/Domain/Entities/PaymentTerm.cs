using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class PaymentTerm : BaseEntity
{
	public required string Name { get; set; } // e.g., "Net 30 Days"
	public required string Code { get; set; } // e.g., "N30"
	public int DueDays { get; set; } // e.g., 30
	public Guid CompanyId { get; set; }
}