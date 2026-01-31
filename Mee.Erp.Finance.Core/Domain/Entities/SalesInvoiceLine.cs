using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class SalesInvoiceLine : BaseEntity
{
    public Guid SalesInvoiceId { get; set; }
    public required string Description { get; set; }

    /// <summary>
    /// The Revenue or Income account this line item should be posted to.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// The pre-tax amount for this line.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The Tax Code from the localization module (e.g., UAE VAT).
    /// </summary>
    public Guid? TaxCodeId { get; set; }
}