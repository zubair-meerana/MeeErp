namespace Mee.Erp.Core.Domain.Enums;

/// <summary>
/// Defines the operating mode of a company, which changes system behavior.
/// </summary>
public enum OperatingMode
{
    /// <summary>
    /// For businesses with complex structures, departments, cost centers, and approval workflows.
    /// </summary>
    Enterprise = 1,

    /// <summary>
    /// For businesses with high-volume transactions and simpler operational needs, like a retail store.
    /// </summary>
    Retail = 2
}