using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for advanced accounting functions
/// </summary>
public interface IAdvancedAccountingService
{
    /// <summary>
    /// Creates accrual accounting adjustments
    /// </summary>
    Task<Journal> CreateAccrualAdjustmentAsync(Guid accountId, decimal amount, string description, DateTime asOfDate, Guid companyId);

    /// <summary>
    /// Creates prepayment deferral entries
    /// </summary>
    Task<Journal> CreatePrepaymentDeferralAsync(Guid prepaidAccountId, Guid expenseAccountId, decimal amount, DateTime startDate, DateTime endDate, Guid companyId);

    /// <summary>
    /// Creates multi-period allocation entries
    /// </summary>
    Task<Journal> CreateMultiPeriodAllocationAsync(Guid sourceAccountId, Guid[] targetAccountIds, decimal[] allocationPercentages, DateTime effectiveDate, Guid companyId);

    /// <summary>
    /// Creates intercompany accounting entries
    /// </summary>
    Task<Journal> CreateIntercompanyEntryAsync(Guid sourceCompanyId, Guid targetCompanyId, Guid accountId, decimal amount, string description, DateTime effectiveDate);

    /// <summary>
    /// Processes period-end closing entries
    /// </summary>
    Task<bool> ProcessPeriodEndClosingAsync(Guid companyId, DateTime periodEndDate);
}
