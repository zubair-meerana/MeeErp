using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for financial validation and business rule enforcement
/// </summary>
public interface IFinancialValidationService
{
    #region Transaction Integrity & Validation

    /// <summary>
    /// Validates inter-module dependencies when posting transactions
    /// </summary>
    Task<bool> ValidateInterModuleDependenciesAsync(Journal journal);

    /// <summary>
    /// Validates cross-references between AR/AP and GL
    /// </summary>
    Task<bool> ValidateARAPCrossValidationAsync(Journal journal);

    /// <summary>
    /// Calculates and validates currency exchange gains/losses
    /// </summary>
    Task<(bool isValid, decimal gainLossAmount, string errorMessage)> ValidateCurrencyExchangeAsync(Journal journal);

    #endregion

    #region Financial Controls

    /// <summary>
    /// Validates spend authorization limits for a user
    /// </summary>
    Task<bool> ValidateSpendAuthorizationAsync(Journal journal, Guid userId);

    /// <summary>
    /// Enforces segregation of duties for critical operations
    /// </summary>
    Task<bool> ValidateSegregationOfDutiesAsync(Journal journal, Guid userId);

    /// <summary>
    /// Validates dual control requirements for critical operations
    /// </summary>
    Task<bool> ValidateDualControlRequirementsAsync(Journal journal, Guid userId);

    /// <summary>
    /// Detects exceptions in transaction patterns
    /// </summary>
    Task<IEnumerable<string>> DetectTransactionExceptionsAsync(Journal journal);

    #endregion

    #region Advanced Accounting Functions

    /// <summary>
    /// Validates accrual accounting adjustments
    /// </summary>
    Task<bool> ValidateAccrualAdjustmentsAsync(Journal journal);

    /// <summary>
    /// Validates prepayment deferral entries
    /// </summary>
    Task<bool> ValidatePrepaymentDeferralsAsync(Journal journal);

    /// <summary>
    /// Validates multi-period allocation entries
    /// </summary>
    Task<bool> ValidateMultiPeriodAllocationsAsync(Journal journal);

    /// <summary>
    /// Validates intercompany accounting entries
    /// </summary>
    Task<bool> ValidateIntercompanyAccountingAsync(Journal journal);

    #endregion

    #region Compliance Validation

    /// <summary>
    /// Performs automated compliance checking
    /// </summary>
    Task<bool> ValidateComplianceAsync(Journal journal);

    /// <summary>
    /// Ensures audit-ready transaction trails
    /// </summary>
    Task<bool> ValidateAuditTrailCompletenessAsync(Journal journal);

    /// <summary>
    /// Validates period closing requirements
    /// </summary>
    Task<bool> ValidatePeriodClosingRequirementsAsync(Journal journal);

    #endregion
}
