using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Shared.Kernel.Domain.Entities;
using System;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class FixedAsset : BaseEntity
{
    public required string Name { get; set; }
    
    /// <summary>
    /// A unique identifier tag or serial number for the physical asset.
    /// </summary>
    public string? AssetTag { get; set; }
    public Guid AssetCategoryId { get; set; }
    public Guid CompanyId { get; set; }

    public DateTime PurchaseDate { get; set; }
    public decimal PurchasePrice { get; set; }
    
    /// <summary>
    /// The date depreciation begins. Often the purchase date or start of the next month.
    /// </summary>
    public DateTime DepreciationStartDate { get; set; }
    
    /// <summary>
    /// The total number of months the asset is expected to be in service.
    /// </summary>
    public int UsefulLifeInMonths { get; set; }
    
    /// <summary>
    // The estimated residual value of the asset at the end of its useful life.
    /// </summary>
    public decimal SalvageValue { get; set; }
    
    public DepreciationMethod DepreciationMethod { get; set; }

    public bool IsDisposed { get; set; } = false;
    public DateTime? DisposalDate { get; set; }

	public virtual AssetCategory AssetCategory { get; set; }
}