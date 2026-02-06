using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

/// <summary>
/// Implements error handling and transaction corrections
/// </summary>
public class ErrorCorrectionService : IErrorCorrectionService
{
    private readonly FinanceDbContext _context;

    public ErrorCorrectionService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<Journal> GenerateReversalJournalAsync(Guid journalId, string reason, Guid correctedByUserId)
    {
        // Retrieve the original journal to reverse
        var originalJournal = await _context.Journals
            .Include(j => j.Entries)
            .FirstOrDefaultAsync(j => j.Id == journalId);

        if (originalJournal == null)
        {
            throw new ArgumentException($"Journal with ID {journalId} not found");
        }

        if (originalJournal.Status != JournalStatus.Posted)
        {
            throw new InvalidOperationException("Cannot reverse a journal that is not posted");
        }

        // Create a reversal journal with opposite entries
        var reversalJournal = new Journal
        {
            JournalDate = DateTime.Today, // Today's date for the reversal
            Description = $"REVERSAL: {originalJournal.Description} - {reason}",
            ReferenceNumber = $"REV-{originalJournal.ReferenceNumber}", // Mark as reversal
            CompanyId = originalJournal.CompanyId,
            Status = JournalStatus.Draft // Draft initially, needs approval
        };

        // Create reversed entries (swap debits and credits)
        foreach (var originalEntry in originalJournal.Entries)
        {
            var reversalEntry = new JournalEntry
            {
                AccountId = originalEntry.AccountId,
                Debit = originalEntry.Credit, // Swap debit/credit
                Credit = originalEntry.Debit, // Swap debit/credit
                Description = $"REVERSAL: {originalEntry.Description}",
                BusinessUnitId = originalEntry.BusinessUnitId,
                TaxCodeId = originalEntry.TaxCodeId,
                TaxRatePercentage = originalEntry.TaxRatePercentage
            };

            reversalJournal.Entries.Add(reversalEntry);
        }

        _context.Journals.Add(reversalJournal);
        await _context.SaveChangesAsync();

        return reversalJournal;
    }

    public async Task<bool> ProcessErrorCorrectionAsync(Guid journalId, string correctionDetails, Guid correctedByUserId)
    {
        // Retrieve the journal to correct
        var journalToCorrect = await _context.Journals
            .Include(j => j.Entries)
            .FirstOrDefaultAsync(j => j.Id == journalId);

        if (journalToCorrect == null)
        {
            return false;
        }

        // Check if the user has permission to correct this journal
        // In a real implementation, this would check user roles and permissions
        var hasPermission = await CheckCorrectionPermissionAsync(journalToCorrect, correctedByUserId);
        if (!hasPermission)
        {
            return false;
        }

        // Log the correction action
        await LogCorrectionActionAsync(journalToCorrect, correctionDetails, correctedByUserId);

        // Mark the original journal as requiring review or correction
        journalToCorrect.UpdatedBy = correctedByUserId;
        journalToCorrect.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Journal> ProcessJournalReclassificationAsync(Guid journalId, Guid[] newAccountIds, decimal[] newAmounts, string reason, Guid reclassifiedByUserId)
    {
        if (newAccountIds.Length != newAmounts.Length)
        {
            throw new ArgumentException("Account IDs and amounts arrays must have the same length");
        }

        // Retrieve the original journal
        var originalJournal = await _context.Journals
            .Include(j => j.Entries)
            .FirstOrDefaultAsync(j => j.Id == journalId);

        if (originalJournal == null)
        {
            throw new ArgumentException($"Journal with ID {journalId} not found");
        }

        if (originalJournal.Status != JournalStatus.Posted)
        {
            throw new InvalidOperationException("Cannot reclassify a journal that is not posted");
        }

        // Validate that the new amounts balance correctly
        var totalDebits = newAmounts.Where((x, i) => i % 2 == 0).Sum(); // Even indices as debits
        var totalCredits = newAmounts.Where((x, i) => i % 2 == 1).Sum(); // Odd indices as credits
        if (Math.Abs(totalDebits - totalCredits) > 0.01m)
        {
            throw new InvalidOperationException("New amounts must form a balanced journal entry");
        }

        // Create a reversal of the original journal
        var reversalJournal = await GenerateReversalJournalAsync(journalId, $"Reclassification: {reason}", reclassifiedByUserId);

        // Create a new journal with reclassified entries
        var reclassificationJournal = new Journal
        {
            JournalDate = DateTime.Today,
            Description = $"RECLASSIFICATION: {originalJournal.Description} - {reason}",
            ReferenceNumber = $"REC-{originalJournal.ReferenceNumber}",
            CompanyId = originalJournal.CompanyId,
            Status = JournalStatus.Draft
        };

        // Add the reclassified entries
        for (int i = 0; i < newAccountIds.Length; i++)
        {
            var isDebit = i % 2 == 0; // Alternate between debit and credit

            var reclassEntry = new JournalEntry
            {
                AccountId = newAccountIds[i],
                Debit = isDebit ? newAmounts[i] : 0,
                Credit = isDebit ? 0 : newAmounts[i],
                Description = $"Reclassification to account {newAccountIds[i]}: {reason}"
            };

            reclassificationJournal.Entries.Add(reclassEntry);
        }

        _context.Journals.Add(reclassificationJournal);
        await _context.SaveChangesAsync();

        return reclassificationJournal;
    }

    public async Task<Journal> ProcessPeriodAdjustmentAsync(Guid journalId, DateTime newEffectiveDate, string reason, Guid adjustedByUserId)
    {
        // Retrieve the journal to adjust
        var journalToAdjust = await _context.Journals
            .Include(j => j.Entries)
            .FirstOrDefaultAsync(j => j.Id == journalId);

        if (journalToAdjust == null)
        {
            throw new ArgumentException($"Journal with ID {journalId} not found");
        }

        // Check if the new date is in an open period
        var financialPeriod = await _context.FinancialPeriods
            .FirstOrDefaultAsync(fp => fp.CompanyId == journalToAdjust.CompanyId &&
                                     fp.StartDate <= newEffectiveDate &&
                                     fp.EndDate >= newEffectiveDate);

        if (financialPeriod == null)
        {
            throw new InvalidOperationException($"No financial period defined for date {newEffectiveDate:yyyy-MM-dd}");
        }

        if (financialPeriod.IsClosed)
        {
            throw new InvalidOperationException($"Financial period '{financialPeriod.Name}' is closed. Cannot adjust transaction date.");
        }

        // Create a reversal of the original journal
        var reversalJournal = await GenerateReversalJournalAsync(journalId, $"Period adjustment: {reason}", adjustedByUserId);

        // Create a new journal with the adjusted date
        var adjustmentJournal = new Journal
        {
            JournalDate = newEffectiveDate,
            Description = $"PERIOD ADJUSTMENT: {journalToAdjust.Description} - {reason}",
            ReferenceNumber = $"ADJ-{journalToAdjust.ReferenceNumber}",
            CompanyId = journalToAdjust.CompanyId,
            Status = JournalStatus.Draft
        };

        // Copy the original entries to the new journal
        foreach (var originalEntry in journalToAdjust.Entries)
        {
            var adjustmentEntry = new JournalEntry
            {
                AccountId = originalEntry.AccountId,
                Debit = originalEntry.Debit,
                Credit = originalEntry.Credit,
                Description = originalEntry.Description,
                BusinessUnitId = originalEntry.BusinessUnitId,
                TaxCodeId = originalEntry.TaxCodeId,
                TaxRatePercentage = originalEntry.TaxRatePercentage
            };

            adjustmentJournal.Entries.Add(adjustmentEntry);
        }

        _context.Journals.Add(adjustmentJournal);
        await _context.SaveChangesAsync();

        return adjustmentJournal;
    }

    public async Task<bool> ValidateCorrectionEntryAsync(Journal correctionJournal, Guid validatorUserId)
    {
        // Perform validation checks on the correction journal
        var validationIssues = new System.Collections.Generic.List<string>();

        // Check 1: Journal must be balanced
        var totalDebits = correctionJournal.Entries.Sum(e => e.Debit);
        var totalCredits = correctionJournal.Entries.Sum(e => e.Credit);
        if (Math.Abs(totalDebits - totalCredits) > 0.01m)
        {
            validationIssues.Add($"Journal not balanced: Debits {totalDebits:C} vs Credits {totalCredits:C}");
        }

        // Check 2: Required fields populated
        if (string.IsNullOrWhiteSpace(correctionJournal.Description))
        {
            validationIssues.Add("Correction journal must have a description");
        }

        // Check 3: Date validation
        if (correctionJournal.JournalDate > DateTime.Now.AddDays(1))
        {
            validationIssues.Add("Future-dated correction detected");
        }

        // Check 4: Account validation
        var accountIds = correctionJournal.Entries.Select(e => e.AccountId).Distinct();
        var accounts = await _context.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToListAsync();

        var invalidAccounts = accountIds.Where(id => !accounts.Any(a => a.Id == id));
        if (invalidAccounts.Any())
        {
            validationIssues.Add($"Invalid account IDs in correction: {string.Join(", ", invalidAccounts)}");
        }

        // Check 5: Ensure this is actually a correction (contains correction keywords)
        if (!correctionJournal.Description.ToLower().Contains("reversal") &&
            !correctionJournal.Description.ToLower().Contains("correction") &&
            !correctionJournal.Description.ToLower().Contains("adjustment") &&
            !correctionJournal.Description.ToLower().Contains("error"))
        {
            validationIssues.Add("Journal description does not indicate this is a correction");
        }

        // Log validation results
        if (validationIssues.Any())
        {
            await LogValidationIssuesAsync(correctionJournal.Id, validationIssues, validatorUserId);
            return false;
        }

        return true;
    }

    #region Helper Methods

    private async Task<bool> CheckCorrectionPermissionAsync(Journal journal, Guid userId)
    {
        // In a real implementation, this would check user roles and permissions
        // For now, we'll return true as a placeholder
        return true;
    }

    private async Task LogCorrectionActionAsync(Journal journal, string correctionDetails, Guid userId)
    {
        // In a real implementation, this would log the correction action to an audit trail
        // For now, we'll just record it as an update to the journal
        journal.UpdatedBy = userId;
        journal.UpdatedDate = DateTime.UtcNow;
    }

    private async Task LogValidationIssuesAsync(Guid journalId, System.Collections.Generic.List<string> issues, Guid userId)
    {
        // In a real implementation, this would log validation issues to a compliance system
        // For now, we'll just record the validation attempt
    }

    #endregion
}
