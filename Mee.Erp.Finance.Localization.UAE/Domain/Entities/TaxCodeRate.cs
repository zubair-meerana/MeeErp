using Mee.Erp.Shared.Kernel.Domain.Entities;
using System;

namespace Mee.Erp.Finance.Localization.UAE.Domain.Entities;

public class TaxCodeRate : BaseEntity
{
    public Guid TaxCodeId { get; set; }
    public decimal RatePercentage { get; set; }

    /// <summary>
    /// The date from which this tax rate is effective.
    /// </summary>
    public DateTime EffectiveDate { get; set; }
}