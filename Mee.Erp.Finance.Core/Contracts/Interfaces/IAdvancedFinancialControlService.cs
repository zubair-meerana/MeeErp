using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for advanced financial controls and governance
/// </summary>
public interface IAdvancedFinancialControlService
{
    /// <summary>
    /// Sets spending authorization limits for users/roles with approval hierarchies
    /// </summary>
    Task<bool> SetSpendingAuthorizationLimitAsync(Guid userId, decimal limit, Guid companyId, Guid roleId = default);

    /// <summary>
    /// Gets spending authorization limit for a user
    /// </summary>
    Task<decimal> GetSpendingAuthorizationLimitAsync(Guid userId, Guid companyId);

    /// <summary>
    /// Enforces segregation of duties for financial operations
    /// </summary>
    Task<bool> EnforceSegregationOfDutiesAsync(Journal journal, Guid userId);

    /// <summary>
    /// Implements dual control for critical operations (four-eye principle)
    /// </summary>
    Task<bool> ApplyDualControlForOperationAsync(Journal journal, Guid initiatingUserId, Guid approvingUserId);

    /// <summary>
    /// Monitors and detects unusual transaction patterns
    /// </summary>
    Task<IEnumerable<string>> MonitorUnusualTransactionPatternsAsync(Journal journal);

    /// <summary>
    /// Generates exception reports for financial controls
    /// </summary>
    Task<byte[]> GenerateExceptionReportAsync(Guid companyId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Monitors financial risks and generates alerts
    /// </summary>
    Task<IEnumerable<FinancialRiskAlert>> MonitorFinancialRisksAsync(Guid companyId, DateTime asOfDate);
}

public class FinancialRiskAlert
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string AlertType { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime AlertDate { get; set; }
    public string Severity { get; set; } // Low, Medium, High, Critical
    public bool IsResolved { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public Guid? ResolvedByUserId { get; set; }
}
