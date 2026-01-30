using Mee.Erp.Shared.Kernel.Domain.Entities;
using System.Collections.Generic;

namespace Mee.Erp.Finance.Localization.UAE.Domain.Entities;

public class TaxCode : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Can this tax code be used on sales transactions?
    /// </summary>
    public bool IsSalesTax { get; set; } = true;

    /// <summary>
    /// Can this tax code be used on purchase transactions?
    /// </summary>
    public bool IsPurchaseTax { get; set; } = true;
    
    /// <summary>
    /// For a given price, does the system need to add tax (exclusive) or extract it (inclusive)?
    /// </summary>
    public bool IsPriceInclusive { get; set; } = false;

    public ICollection<TaxCodeRate> Rates { get; set; } = new List<TaxCodeRate>();
}