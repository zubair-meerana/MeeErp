using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for financial controls and governance
/// </summary>
public interface IFinancialControlService
{
    /// <summary>
    /// Sets spending authorization limits for users/roles
    /// </summary>
    Task<bool> SetSpendingAuthorizationLimitAsync(Guid userId, decimal limit, Guid companyId);

    /// <summary>
    /// Enforces segregation of duties for financial operations
    /// </summary>
    Task<bool> EnforceSegregationOfDutiesAsync(Journal journal, Guid userId);

    /// <summary>
    /// Implements dual control for critical operations
    /// </summary>
    Task<bool> ApplyDualControlForOperationAsync(Journal journal, Guid initiatingUserId, Guid approvingUserId);

    /// <summary>
    /// Monitors and detects unusual transaction patterns
    /// </summary>
    Task<bool> MonitorUnusualTransactionPatternsAsync(Journal journal);

    /// <summary>
    /// Generates exception reports for financial controls
    /// </summary>
    Task<byte[]> GenerateExceptionReportAsync(Guid companyId, DateTime fromDate, DateTime toDate);
}
