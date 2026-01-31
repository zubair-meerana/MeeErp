using Mee.Erp.Shared.Kernel.Domain.Entities;
using System;

namespace Mee.Erp.Finance.Core.Domain.Entities;

/// <summary>
/// Records a single instance of a depreciation calculation being posted.
/// Provides a clear audit trail for an asset's depreciation history.
/// </summary>
public class FixedAssetDepreciation : BaseEntity
{
    public Guid FixedAssetId { get; set; }
    
    /// <summary>
    /// The date for which the depreciation is being recognized (e.g., end of the month).
    /// </summary>
    public DateTime DepreciationDate { get; set; }
    
    public decimal Amount { get; set; }
    
    /// <summary>
    /// The Journal ID of the depreciation transaction posted to the GL.
    /// </summary>
    public Guid JournalId { get; set; }
}