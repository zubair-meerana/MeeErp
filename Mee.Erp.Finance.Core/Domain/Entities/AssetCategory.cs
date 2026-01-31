using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class AssetCategory : BaseEntity
{
    public required string Name { get; set; }
    public Guid CompanyId { get; set; }

    // --- GL Account Links ---
    /// <summary>
    /// The Asset account where the cost of assets in this category is recorded (e.g., "1700 - Office Equipment").
    /// </summary>
    public Guid AssetAccountId { get; set; }

    /// <summary>
    /// The contra-asset account for accumulated depreciation (e.g., "1790 - Acc. Dep. - Office Equipment").
    /// </summary>
    public Guid AccumulatedDepreciationAccountId { get; set; }

    /// <summary>
    /// The Expense account for the periodic depreciation charge (e.g., "6500 - Depreciation Expense").
    /// </summary>
    public Guid DepreciationExpenseAccountId { get; set; }
}