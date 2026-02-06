using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

/// <summary>
/// Implements financial validation and business rule enforcement
/// </summary>
public class FinancialValidationService : IFinancialValidationService
{
    private readonly FinanceDbContext _context;

    public FinancialValidationService(FinanceDbContext context)
    {
        _context = context;
    }

    #region Transaction Integrity & Validation

    public async Task<bool> ValidateInterModuleDependenciesAsync(Journal journal)
    {
        // Check if journal involves inventory-related accounts and validate inventory levels
        var inventoryRelatedEntries = journal.Entries
            .Where(e => e.Description?.ToLower().Contains("inventory") == true ||
                       e.Description?.ToLower().Contains("cogs") == true);

        foreach (var entry in inventoryRelatedEntries)
        {
            // Example validation: Check if inventory account is properly linked to actual inventory
            var account = await _context.Accounts.FindAsync(entry.AccountId);
            if (account != null && account.Name.ToLower().Contains("inventory"))
            {
                // Additional validation logic would go here
                // For example, checking if corresponding inventory transactions exist
            }
        }

        return true; // Placeholder - actual validation logic would be more complex
    }

    public async Task<bool> ValidateARAPCrossValidationAsync(Journal journal)
    {
        // Validate that AR/AP entries have corresponding customer/supplier references
        var arapEntries = journal.Entries
            .Where(e => e.Description?.ToLower().Contains("ar") == true ||
                       e.Description?.ToLower().Contains("ap") == true ||
                       e.Description?.ToLower().Contains("receivable") == true ||
                       e.Description?.ToLower().Contains("payable") == true);

        foreach (var entry in arapEntries)
        {
            // Check if customer/supplier exists for AR entries
            if (entry.Description?.ToLower().Contains("receivable") == true ||
                entry.Description?.ToLower().Contains("ar") == true)
            {
                // Validate customer exists
                var customerExists = await _context.Customers.AnyAsync(c => c.CompanyId == journal.CompanyId);
                if (!customerExists)
                    return false;
            }

            if (entry.Description?.ToLower().Contains("payable") == true ||
                entry.Description?.ToLower().Contains("ap") == true)
            {
                // Validate supplier exists
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.CompanyId == journal.CompanyId);
                if (!supplierExists)
                    return false;
            }
        }

        return true;
    }

    public async Task<(bool isValid, decimal gainLossAmount, string errorMessage)> ValidateCurrencyExchangeAsync(Journal journal)
    {
        decimal gainLossAmount = 0;

        // Check for multi-currency transactions
        var multiCurrencyEntries = journal.Entries
            .Where(e => !string.IsNullOrEmpty(e.Description) &&
                        (e.Description.Contains("FX") || e.Description.Contains("currency") ||
                         e.Description.Contains("exchange")));

        if (!multiCurrencyEntries.Any())
        {
            return (true, 0, null);
        }

        // Calculate potential FX gains/losses
        foreach (var entry in multiCurrencyEntries)
        {
            // In a real implementation, we would:
            // 1. Look up the exchange rates at the transaction date
            // 2. Compare with current rates
            // 3. Calculate the gain/loss amount
            // 4. Validate that appropriate FX gain/loss accounts are used

            // Placeholder calculation
            gainLossAmount += entry.Debit > entry.Credit ? entry.Debit * 0.01m : entry.Credit * 0.01m;
        }

        // Validate that FX gain/loss accounts are properly used
        var fxGainLossAccounts = await _context.Accounts
            .Where(a => a.CompanyId == journal.CompanyId &&
                       (a.Name.ToLower().Contains("fx gain") ||
                        a.Name.ToLower().Contains("fx loss") ||
                        a.Name.ToLower().Contains("foreign exchange")))
            .ToListAsync();

        if (gainLossAmount != 0 && !fxGainLossAccounts.Any())
        {
            return (false, gainLossAmount, "No FX gain/loss accounts configured for currency exchange validation");
        }

        return (true, gainLossAmount, null);
    }

    #endregion

    #region Financial Controls

    public async Task<bool> ValidateSpendAuthorizationAsync(Journal journal, Guid userId)
    {
        // Get user's spending limit
        // In a real implementation, this would come from a user profile or role-based config
        var userSpendingLimit = await GetUserSpendingLimitAsync(userId);

        var totalAmount = journal.Entries.Sum(e => e.Debit);

        return totalAmount <= userSpendingLimit;
    }

    public async Task<bool> ValidateSegregationOfDutiesAsync(Journal journal, Guid userId)
    {
        // Check if the same user is trying to perform conflicting operations
        // For example, creating and approving the same transaction

        // This would typically check against a workflow or approval system
        // Placeholder implementation
        var existingApprovals = await _context.Journals
            .Where(j => j.Id == journal.Id && j.CreatedBy == userId && j.UpdatedBy == userId)
            .AnyAsync();

        return !existingApprovals; // If same user created and updated, segregation is violated
    }

    public async Task<bool> ValidateDualControlRequirementsAsync(Journal journal, Guid userId)
    {
        // Check if critical operations require dual control
        var criticalAccounts = await _context.Accounts
            .Where(a => a.CompanyId == journal.CompanyId &&
                       (a.Name.ToLower().Contains("cash") ||
                        a.Name.ToLower().Contains("bank") ||
                        a.Name.ToLower().Contains("loan") ||
                        a.AccountNumber.StartsWith("1") || // Asset accounts
                        a.AccountNumber.StartsWith("2"))) // Liability accounts
            .ToListAsync();

        var hasCriticalAccount = journal.Entries.Any(e =>
            criticalAccounts.Any(ca => ca.Id == e.AccountId));

        // If journal involves critical accounts, check if dual control is applied
        if (hasCriticalAccount)
        {
            // In a real implementation, this would check for secondary approval
            // Placeholder: assume dual control means another user has approved
            return journal.UpdatedBy.HasValue && journal.UpdatedBy != journal.CreatedBy;
        }

        return true;
    }

    public async Task<IEnumerable<string>> DetectTransactionExceptionsAsync(Journal journal)
    {
        var exceptions = new List<string>();

        // Check for unusually large amounts
        var avgTransactionAmount = await _context.Journals
            .Where(j => j.CompanyId == journal.CompanyId)
            .AverageAsync(j => j.Entries.Sum(e => e.Debit)) ?? 0;

        var currentAmount = journal.Entries.Sum(e => e.Debit);
        if (currentAmount > avgTransactionAmount * 10) // 10x average threshold
        {
            exceptions.Add($"Transaction amount ({currentAmount}) significantly exceeds average ({avgTransactionAmount})");
        }

        // Check for round numbers that might indicate estimates
        if (journal.Entries.All(e => e.Debit % 1 == 0 && e.Credit % 1 == 0))
        {
            exceptions.Add("Transaction contains only round numbers - may be estimated");
        }

        // Check for unusual timing (outside business hours)
        if (journal.JournalDate.Hour < 8 || journal.JournalDate.Hour > 18)
        {
            exceptions.Add($"Transaction recorded outside normal business hours ({journal.JournalDate:HH:mm})");
        }

        // Check for duplicate entries
        var similarRecentJournals = await _context.Journals
            .Where(j => j.CompanyId == journal.CompanyId &&
                       j.JournalDate.Date == journal.JournalDate.Date &&
                       j.Description == journal.Description &&
                       j.Entries.Sum(e => e.Debit) == journal.Entries.Sum(e => e.Debit))
            .ToListAsync();

        if (similarRecentJournals.Count > 1)
        {
            exceptions.Add("Potential duplicate transaction detected");
        }

        return exceptions;
    }

    #endregion

    #region Advanced Accounting Functions

    public async Task<bool> ValidateAccrualAdjustmentsAsync(Journal journal)
    {
        // Check if this is an accrual adjustment
        var isAccrual = journal.Description.ToLower().Contains("accrual") ||
                       journal.Description.ToLower().Contains("adjustment");

        if (!isAccrual) return true;

        // Validate accrual follows proper accounting principles
        // Accruals should typically involve expense/revenue accounts and liability/asset accounts
        var expenseRevenueAccounts = await _context.Accounts
            .Where(a => a.CompanyId == journal.CompanyId &&
                       (a.AccountType == Domain.Enums.AccountType.Expense ||
                        a.AccountType == Domain.Enums.AccountType.Revenue))
            .ToListAsync();

        var liabilityAssetAccounts = await _context.Accounts
            .Where(a => a.CompanyId == journal.CompanyId &&
                       (a.AccountType == Domain.Enums.AccountType.Liability ||
                        a.AccountType == Domain.Enums.AccountType.Asset))
            .ToListAsync();

        var hasExpenseRevenue = journal.Entries.Any(e =>
            expenseRevenueAccounts.Any(era => era.Id == e.AccountId));
        var hasLiabilityAsset = journal.Entries.Any(e =>
            liabilityAssetAccounts.Any(laa => laa.Id == e.AccountId));

        return hasExpenseRevenue && hasLiabilityAsset;
    }

    public async Task<bool> ValidatePrepaymentDeferralsAsync(Journal journal)
    {
        // Check if this is a prepayment deferral
        var isDeferral = journal.Description.ToLower().Contains("deferral") ||
                        journal.Description.ToLower().Contains("prepayment");

        if (!isDeferral) return true;

        // Validate prepayment deferral structure
        // Should involve prepaid expense (asset) and expense accounts
        var prepaidExpenseAccounts = await _context.Accounts
            .Where(a => a.CompanyId == journal.CompanyId &&
                       (a.Name.ToLower().Contains("prepaid") ||
                        a.Name.ToLower().Contains("deferred")))
            .ToListAsync();

        var expenseAccounts = await _context.Accounts
            .Where(a => a.CompanyId == journal.CompanyId &&
                       a.AccountType == Domain.Enums.AccountType.Expense)
            .ToListAsync();

        var hasPrepaid = journal.Entries.Any(e =>
            prepaidExpenseAccounts.Any(pea => pea.Id == e.AccountId));
        var hasExpense = journal.Entries.Any(e =>
            expenseAccounts.Any(ea => ea.Id == e.AccountId));

        return hasPrepaid && hasExpense;
    }

    public async Task<bool> ValidateMultiPeriodAllocationsAsync(Journal journal)
    {
        // Check if this is a multi-period allocation
        var isAllocation = journal.Description.ToLower().Contains("allocation") ||
                          journal.Description.ToLower().Contains("distribution");

        if (!isAllocation) return true;

        // Validate allocation entries are properly structured
        // Typically involves cost centers or departments
        var hasBusinessUnit = journal.Entries.Any(e => e.BusinessUnitId.HasValue);

        return hasBusinessUnit;
    }

    public async Task<bool> ValidateIntercompanyAccountingAsync(Journal journal)
    {
        // Check if this is an intercompany transaction
        var isIntercompany = journal.Description.ToLower().Contains("intercompany") ||
                            journal.ReferenceNumber?.ToLower().Contains("ic") == true;

        if (!isIntercompany) return true;

        // Validate intercompany structure
        // Should involve intercompany payable/receivable accounts
        var intercompanyAccounts = await _context.Accounts
            .Where(a => a.CompanyId == journal.CompanyId &&
                       (a.Name.ToLower().Contains("intercompany") ||
                        a.Name.ToLower().Contains("due to") ||
                        a.Name.ToLower().Contains("due from") ||
                        a.Name.ToLower().Contains("ic payable") ||
                        a.Name.ToLower().Contains("ic receivable")))
            .ToListAsync();

        var hasIntercompanyAccount = journal.Entries.Any(e =>
            intercompanyAccounts.Any(ica => ica.Id == e.AccountId));

        return hasIntercompanyAccount;
    }

    #endregion

    #region Compliance Validation

    public async Task<bool> ValidateComplianceAsync(Journal journal)
    {
        // Basic compliance checks
        // Ensure all required fields are populated
        if (string.IsNullOrWhiteSpace(journal.Description))
        {
            return false;
        }

        // Check if transaction date is within acceptable range
        var financialPeriod = await _context.FinancialPeriods
            .FirstOrDefaultAsync(fp => fp.CompanyId == journal.CompanyId &&
                                     fp.StartDate <= journal.JournalDate &&
                                     fp.EndDate >= journal.JournalDate);

        if (financialPeriod == null)
        {
            return false; // Transaction date is outside defined financial periods
        }

        return true;
    }

    public async Task<bool> ValidateAuditTrailCompletenessAsync(Journal journal)
    {
        // Ensure audit fields are properly populated
        return journal.CreatedDate != default(DateTime) &&
               journal.CreatedBy.HasValue;
    }

    public async Task<bool> ValidatePeriodClosingRequirementsAsync(Journal journal)
    {
        // Check if we're trying to post to a closed period
        var period = await _context.FinancialPeriods
            .FirstOrDefaultAsync(p => p.CompanyId == journal.CompanyId &&
                                     p.StartDate <= journal.JournalDate &&
                                     p.EndDate >= journal.JournalDate);

        if (period != null && period.IsClosed)
        {
            return false; // Cannot post to closed period
        }

        return true;
    }

    #endregion

    #region Helper Methods

    private async Task<decimal> GetUserSpendingLimitAsync(Guid userId)
    {
        // In a real implementation, this would fetch from user profile or role configuration
        // For now, returning a default value
        return 100000; // $100,000 default limit
    }

    #endregion
}
