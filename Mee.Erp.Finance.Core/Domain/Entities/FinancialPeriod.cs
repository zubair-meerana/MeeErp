using Mee.Erp.Shared.Kernel.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class FinancialPeriod : BaseEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid CompanyId { get; set; }
    public FinancialPeriodStatus Status { get; set; } = FinancialPeriodStatus.Open;
    public bool IsFiscalYear { get; set; } = false;
    public int FiscalYear { get; set; }
    public int? PeriodNumber { get; set; } // e.g., 1 for Jan, 2 for Feb, etc.
    public DateTime? ClosedDate { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public string? ClosingNotes { get; set; }
    public Guid? PreviousPeriodId { get; set; }
    public Guid? NextPeriodId { get; set; }
}

public enum FinancialPeriodStatus
{
    Open = 0,
    Closing = 1,
    Closed = 2,
    PermanentlyClosed = 3
}
