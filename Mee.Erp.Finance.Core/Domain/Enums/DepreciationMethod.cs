namespace Mee.Erp.Finance.Core.Domain.Enums;

/// <summary>
/// Defines the method used to calculate asset depreciation.
/// </summary>
public enum DepreciationMethod
{
    /// <summary>
    /// (Cost - Salvage Value) / Useful Life. Spreads the cost evenly.
    /// </summary>
    StraightLine = 1,

    /// <summary>
    /// Applies a constant depreciation rate to the asset's book value each year.
    /// </summary>
    DecliningBalance = 2
}