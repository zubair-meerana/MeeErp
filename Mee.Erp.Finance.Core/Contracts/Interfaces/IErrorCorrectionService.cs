using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for error handling and transaction corrections
/// </summary>
public interface IErrorCorrectionService
{
    /// <summary>
    /// Generates automated reversal journals for correcting errors
    /// </summary>
    Task<Journal> GenerateReversalJournalAsync(Guid journalId, string reason, Guid correctedByUserId);

    /// <summary>
    /// Manages error correction workflows
    /// </summary>
    Task<bool> ProcessErrorCorrectionAsync(Guid journalId, string correctionDetails, Guid correctedByUserId);

    /// <summary>
    /// Handles journal reclassification procedures
    /// </summary>
    Task<Journal> ProcessJournalReclassificationAsync(Guid journalId, Guid[] newAccountIds, decimal[] newAmounts, string reason, Guid reclassifiedByUserId);

    /// <summary>
    /// Manages period adjustment protocols
    /// </summary>
    Task<Journal> ProcessPeriodAdjustmentAsync(Guid journalId, DateTime newEffectiveDate, string reason, Guid adjustedByUserId);

    /// <summary>
    /// Validates correction entries before posting
    /// </summary>
    Task<bool> ValidateCorrectionEntryAsync(Journal correctionJournal, Guid validatorUserId);
}
