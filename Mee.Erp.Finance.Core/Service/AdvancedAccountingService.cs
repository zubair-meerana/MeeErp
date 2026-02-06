using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

/// <summary>
/// Implements advanced accounting functions
/// </summary>
public class AdvancedAccountingService : IAdvancedAccountingService
{
    private readonly FinanceDbContext _context;

    public AdvancedAccountingService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<Journal> CreateAccrualAdjustmentAsync(Guid accountId, decimal amount, string description, DateTime asOfDate, Guid companyId)
    {
        // Find the account to determine if it's an expense or revenue
        var account = await _context.Accounts.FindAsync(accountId);
        if (account == null)
            throw new ArgumentException($"Account {accountId} not found");

        // Determine the corresponding liability or asset account for the accrual
        Guid contraAccountId;
        if (account.AccountType == AccountType.Expense)
        {
            // For expenses, we need to credit an accrued liability
            var accruedLiabilityAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId &&
                                         a.AccountType == AccountType.Liability &&
                                         a.Name.ToLower().Contains("accrued"));
            contraAccountId = accruedLiabilityAccount?.Id ?? throw new InvalidOperationException("No accrued liability account found");
        }
        else if (account.AccountType == AccountType.Revenue)
        {
            // For revenues, we need to credit an accrued receivable
            var accruedAssetAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId &&
                                         a.AccountType == AccountType.Asset &&
                                         a.Name.ToLower().Contains("accrued"));
            contraAccountId = accruedAssetAccount?.Id ?? throw new InvalidOperationException("No accrued asset account found");
        }
        else
        {
            throw new InvalidOperationException("Accrual adjustments can only be created for expense or revenue accounts");
        }

        // Create the journal entry
        var journal = new Journal
        {
            JournalDate = asOfDate,
            Description = $"Accrual Adjustment: {description}",
            CompanyId = companyId,
            Status = JournalStatus.Draft
        };

        // Add the entries
        if (account.AccountType == AccountType.Expense)
        {
            // Debit the expense account, credit the accrued liability
            journal.Entries.Add(new JournalEntry
            {
                AccountId = accountId,
                Debit = amount,
                Credit = 0,
                Description = $"Accrued expense: {description}"
            });
            journal.Entries.Add(new JournalEntry
            {
                AccountId = contraAccountId,
                Debit = 0,
                Credit = amount,
                Description = $"Accrued liability: {description}"
            });
        }
        else // Revenue
        {
            // Debit the accrued asset, credit the revenue account
            journal.Entries.Add(new JournalEntry
            {
                AccountId = contraAccountId,
                Debit = amount,
                Credit = 0,
                Description = $"Accrued receivable: {description}"
            });
            journal.Entries.Add(new JournalEntry
            {
                AccountId = accountId,
                Debit = 0,
                Credit = amount,
                Description = $"Accrued revenue: {description}"
            });
        }

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        return journal;
    }

    public async Task<Journal> CreatePrepaymentDeferralAsync(Guid prepaidAccountId, Guid expenseAccountId, decimal amount, DateTime startDate, DateTime endDate, Guid companyId)
    {
        // Validate accounts exist
        var prepaidAccount = await _context.Accounts.FindAsync(prepaidAccountId);
        var expenseAccount = await _context.Accounts.FindAsync(expenseAccountId);

        if (prepaidAccount == null || expenseAccount == null)
            throw new ArgumentException("Prepaid or expense account not found");

        // Ensure the prepaid account is an asset and expense account is an expense
        if (prepaidAccount.AccountType != AccountType.Asset || expenseAccount.AccountType != AccountType.Expense)
            throw new InvalidOperationException("Prepaid must be an asset account and expense must be an expense account");

        // Create the deferral journal
        var journal = new Journal
        {
            JournalDate = startDate,
            Description = $"Prepayment Deferral: {expenseAccount.Name}",
            CompanyId = companyId,
            Status = JournalStatus.Draft
        };

        // Initially record the prepayment (this would typically be from a separate payment transaction)
        // But for the deferral, we'll create the amortization entry
        var totalMonths = (endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month) + 1;
        var monthlyAmount = Math.Round(amount / totalMonths, 2);

        // For simplicity, we'll create one entry representing the deferral
        // In practice, you might create monthly entries over the period
        journal.Entries.Add(new JournalEntry
        {
            AccountId = expenseAccountId,
            Debit = monthlyAmount,
            Credit = 0,
            Description = $"Monthly amortization of prepayment"
        });
        journal.Entries.Add(new JournalEntry
        {
            AccountId = prepaidAccountId,
            Debit = 0,
            Credit = monthlyAmount,
            Description = $"Reduction in prepaid asset"
        });

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        return journal;
    }

    public async Task<Journal> CreateMultiPeriodAllocationAsync(Guid sourceAccountId, Guid[] targetAccountIds, decimal[] allocationPercentages, DateTime effectiveDate, Guid companyId)
    {
        if (targetAccountIds.Length != allocationPercentages.Length)
            throw new ArgumentException("Target accounts and allocation percentages arrays must have the same length");

        if (Math.Abs(allocationPercentages.Sum() - 100.0m) > 0.01m)
            throw new ArgumentException("Allocation percentages must sum to 100%");

        // Get the source account balance or amount to allocate
        // For this example, we'll assume we're allocating a specific amount
        // In practice, this might be based on actual balances or other criteria
        var sourceAccount = await _context.Accounts.FindAsync(sourceAccountId);
        if (sourceAccount == null)
            throw new ArgumentException($"Source account {sourceAccount} not found");

        // For this example, we'll use a fixed amount to allocate
        // In practice, this would come from the calling function
        decimal totalAmountToAllocate = 10000; // Placeholder - this should come from parameters

        var journal = new Journal
        {
            JournalDate = effectiveDate,
            Description = $"Multi-period allocation from {sourceAccount.Name}",
            CompanyId = companyId,
            Status = JournalStatus.Draft
        };

        // Create entries for each allocation
        for (int i = 0; i < targetAccountIds.Length; i++)
        {
            var targetAccount = await _context.Accounts.FindAsync(targetAccountIds[i]);
            if (targetAccount == null)
                throw new ArgumentException($"Target account {targetAccountIds[i]} not found");

            var allocatedAmount = Math.Round(totalAmountToAllocate * allocationPercentages[i] / 100, 2);

            // Debit the target account
            journal.Entries.Add(new JournalEntry
            {
                AccountId = targetAccountIds[i],
                Debit = allocatedAmount,
                Credit = 0,
                Description = $"Allocation {allocationPercentages[i]}% to {targetAccount.Name}"
            });
        }

        // Credit the source account
        journal.Entries.Add(new JournalEntry
        {
            AccountId = sourceAccountId,
            Debit = 0,
            Credit = totalAmountToAllocate,
            Description = $"Source for allocation"
        });

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        return journal;
    }

    public async Task<Journal> CreateIntercompanyEntryAsync(Guid sourceCompanyId, Guid targetCompanyId, Guid accountId, decimal amount, string description, DateTime effectiveDate)
    {
        // For intercompany transactions, we typically need to create entries in both companies
        // This is a simplified version that creates a single journal
        // In practice, you'd need to coordinate between two company databases

        // Find the intercompany clearing account for the source company
        var sourceClearingAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == sourceCompanyId &&
                                    (a.Name.ToLower().Contains("intercompany") ||
                                     a.Name.ToLower().Contains("due to") ||
                                     a.Name.ToLower().Contains("due from")));

        // Find the intercompany clearing account for the target company
        var targetClearingAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == targetCompanyId &&
                                    (a.Name.ToLower().Contains("intercompany") ||
                                     a.Name.ToLower().Contains("due to") ||
                                     a.Name.ToLower().Contains("due from")));

        if (sourceClearingAccount == null || targetClearingAccount == null)
            throw new InvalidOperationException("Intercompany clearing accounts not found");

        // Create the journal for the source company
        var journal = new Journal
        {
            JournalDate = effectiveDate,
            Description = $"Intercompany transaction: {description}",
            CompanyId = sourceCompanyId,
            Status = JournalStatus.Draft
        };

        // Debit or credit the specified account depending on transaction type
        // For this example, we'll assume it's a transfer out from source
        journal.Entries.Add(new JournalEntry
        {
            AccountId = accountId,
            Debit = 0,
            Credit = amount,
            Description = $"Intercompany transfer: {description}"
        });

        // Credit the intercompany clearing account
        journal.Entries.Add(new JournalEntry
        {
            AccountId = sourceClearingAccount.Id,
            Debit = amount,
            Credit = 0,
            Description = $"Intercompany clearing for transfer to company {targetCompanyId}"
        });

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        return journal;
    }

    public async Task<bool> ProcessPeriodEndClosingAsync(Guid companyId, DateTime periodEndDate)
    {
        // Perform period-end closing procedures
        // 1. Close revenue accounts to retained earnings
        // 2. Close expense accounts to retained earnings
        // 3. Close dividend accounts to retained earnings
        // 4. Update financial period status

        var revenueAccounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId && a.AccountType == AccountType.Revenue)
            .ToListAsync();

        var expenseAccounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId && a.AccountType == AccountType.Expense)
            .ToListAsync();

        // Calculate net income/loss
        decimal totalRevenue = 0;
        decimal totalExpenses = 0;

        foreach (var revAccount in revenueAccounts)
        {
            // Get account balance
            var balance = await GetAccountBalanceAsync(revAccount.Id, periodEndDate);
            totalRevenue += balance;
        }

        foreach (var expAccount in expenseAccounts)
        {
            // Get account balance
            var balance = await GetAccountBalanceAsync(expAccount.Id, periodEndDate);
            totalExpenses += balance;
        }

        decimal netIncome = totalRevenue - totalExpenses;

        // Create closing entries
        var closingJournal = new Journal
        {
            JournalDate = periodEndDate,
            Description = "Period-end closing entries",
            CompanyId = companyId,
            Status = JournalStatus.Draft
        };

        // Close revenue accounts (debit revenues, credit income summary)
        var incomeSummaryAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId &&
                                    a.Name.ToLower().Contains("income summary"));

        if (incomeSummaryAccount == null)
        {
            // Create an income summary account if it doesn't exist
            incomeSummaryAccount = new Account
            {
                AccountNumber = "99999",
                Name = "Income Summary",
                AccountType = AccountType.Equity,
                CompanyId = companyId,
                IsActive = true
            };
            _context.Accounts.Add(incomeSummaryAccount);
            await _context.SaveChangesAsync();
        }

        foreach (var revAccount in revenueAccounts)
        {
            var balance = await GetAccountBalanceAsync(revAccount.Id, periodEndDate);
            if (balance != 0)
            {
                closingJournal.Entries.Add(new JournalEntry
                {
                    AccountId = revAccount.Id,
                    Debit = balance,  // Debit revenue to close it
                    Credit = 0,
                    Description = "Closing entry - zero out revenue"
                });

                closingJournal.Entries.Add(new JournalEntry
                {
                    AccountId = incomeSummaryAccount.Id,
                    Debit = 0,
                    Credit = balance,
                    Description = "Close revenue to income summary"
                });
            }
        }

        // Close expense accounts (credit expenses, debit income summary)
        foreach (var expAccount in expenseAccounts)
        {
            var balance = await GetAccountBalanceAsync(expAccount.Id, periodEndDate);
            if (balance != 0)
            {
                closingJournal.Entries.Add(new JournalEntry
                {
                    AccountId = expAccount.Id,
                    Debit = 0,  // Credit expense to close it
                    Credit = balance,
                    Description = "Closing entry - zero out expense"
                });

                closingJournal.Entries.Add(new JournalEntry
                {
                    AccountId = incomeSummaryAccount.Id,
                    Debit = balance,
                    Credit = 0,
                    Description = "Close expense to income summary"
                });
            }
        }

        // Close income summary to retained earnings
        var retainedEarningsAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId &&
                                    a.AccountType == AccountType.Equity &&
                                    a.Name.ToLower().Contains("retained earnings"));

        if (retainedEarningsAccount != null)
        {
            closingJournal.Entries.Add(new JournalEntry
            {
                AccountId = incomeSummaryAccount.Id,
                Debit = netIncome > 0 ? netIncome : 0,  // If net income, debit income summary
                Credit = netIncome < 0 ? Math.Abs(netIncome) : 0,  // If net loss, credit income summary
                Description = "Close income summary to retained earnings"
            });

            closingJournal.Entries.Add(new JournalEntry
            {
                AccountId = retainedEarningsAccount.Id,
                Debit = netIncome < 0 ? Math.Abs(netIncome) : 0,  // If net loss, debit retained earnings
                Credit = netIncome > 0 ? netIncome : 0,  // If net income, credit retained earnings
                Description = "Close income summary to retained earnings"
            });
        }

        // Add the closing journal to the database
        _context.Journals.Add(closingJournal);
        await _context.SaveChangesAsync();

        // Mark the financial period as closed
        var financialPeriod = await _context.FinancialPeriods
            .FirstOrDefaultAsync(fp => fp.CompanyId == companyId &&
                                     fp.StartDate <= periodEndDate &&
                                     fp.EndDate >= periodEndDate);

        if (financialPeriod != null)
        {
            financialPeriod.IsClosed = true;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    private async Task<decimal> GetAccountBalanceAsync(Guid accountId, DateTime asOfDate)
    {
        // Calculate the account balance up to the specified date
        var ledgerEntries = await _context.LedgerEntries
            .Where(le => le.AccountId == accountId && le.EntryDate <= asOfDate)
            .ToListAsync();

        decimal totalDebits = ledgerEntries.Sum(le => le.Debit);
        decimal totalCredits = ledgerEntries.Sum(le => le.Credit);

        // The balance depends on the account type
        var account = await _context.Accounts.FindAsync(accountId);

        if (account.AccountType == AccountType.Asset ||
            account.AccountType == AccountType.Expense)
        {
            return totalDebits - totalCredits; // Normal balance for assets/expenses is debit
        }
        else
        {
            return totalCredits - totalDebits; // Normal balance for liabilities/equity/revenue is credit
        }
    }
}
