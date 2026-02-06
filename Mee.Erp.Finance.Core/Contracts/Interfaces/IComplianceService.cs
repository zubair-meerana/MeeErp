using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for regulatory compliance and audit management
/// </summary>
public interface IComplianceService
{
    /// <summary>
    /// Performs automated compliance checking for transactions
    /// </summary>
    Task<bool> PerformAutomatedComplianceCheckAsync(Journal journal);

    /// <summary>
    /// Ensures audit-ready transaction trails
    /// </summary>
    Task<bool> EnsureAuditTrailCompletenessAsync(Journal journal);

    /// <summary>
    /// Manages period closing checklist automation
    /// </summary>
    Task<bool> ManagePeriodClosingChecklistAsync(Guid companyId, DateTime periodEndDate);

    /// <summary>
    /// Generates financial statement footnotes
    /// </summary>
    Task<string> GenerateFinancialStatementFootnotesAsync(Guid companyId, DateTime periodEndDate);

    /// <summary>
    /// Validates adherence to local regulatory requirements
    /// </summary>
    Task<bool> ValidateLocalRegulatoryComplianceAsync(Journal journal, string countryCode);
}
