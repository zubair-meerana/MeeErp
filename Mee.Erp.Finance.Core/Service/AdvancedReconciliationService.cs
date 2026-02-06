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
/// Implements advanced reconciliation management
/// </summary>
public class AdvancedReconciliationService : IAdvancedReconciliationService
{
    private readonly FinanceDbContext _context;

    public AdvancedReconciliationService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<BankReconciliationResult> PerformAutomatedBankReconciliationAsync(BankReconciliationRequest request)
    {
        var result = new BankReconciliationResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            BankAccountId = request.BankAccountId,
            StatementBalance = request.StatementBalance,
            Matches = new List<ReconciliationMatch>(),
            Exceptions = new List<ReconciliationException>(),
            AccountingEntries = new List<Journal>(),
            OutstandingChecks = 0,
            DepositsInTransit = 0
        };

        // Get system balance for the account
        var systemBalance = await GetAccountBalanceAsync(request.BankAccountId);
        result.SystemBalance = systemBalance;

        // Calculate difference
        result.Difference = result.StatementBalance - result.SystemBalance;

        // Attempt to match transactions
        var matchedSystemTransactions = new HashSet<Guid>();
        var matchedBankTransactions = new HashSet<Guid>();

        foreach (var bankTx in request.BankStatementTransactions)
        {
            // Look for matching system transaction
            var matchingSystemTx = request.SystemTransactions
                .FirstOrDefault(st => st.Amount == bankTx.Amount &&
                                    Math.Abs((st.TransactionDate - bankTx.TransactionDate).TotalDays) <= 5 &&
                                    !matchedSystemTransactions.Contains(st.Id));

            if (matchingSystemTx != null)
            {
                // Create match
                var match = new ReconciliationMatch
                {
                    SystemTransactionId = matchingSystemTx.Id,
                    BankStatementTransactionId = bankTx.Id,
                    Amount = bankTx.Amount,
                    TransactionDate = bankTx.TransactionDate,
                    Description = bankTx.Description,
                    MatchDate = DateTime.UtcNow,
                    MatchMethod = "AmountAndDate"
                };

                result.Matches.Add(match);
                matchedSystemTransactions.Add(matchingSystemTx.Id);
                matchedBankTransactions.Add(bankTx.Id);
            }
            else
            {
                // Add as exception
                result.Exceptions.Add(new ReconciliationException
                {
                    TransactionId = bankTx.Id,
                    TransactionType = "BankOnly",
                    Amount = bankTx.Amount,
                    TransactionDate = bankTx.TransactionDate,
                    Description = bankTx.Description,
                    ExceptionType = "MissingInSystem",
                    ResolutionStatus = "Open"
                });
            }
        }

        // Add unmatched system transactions as exceptions
        foreach (var systemTx in request.SystemTransactions.Where(st => !matchedSystemTransactions.Contains(st.Id)))
        {
            result.Exceptions.Add(new ReconciliationException
            {
                TransactionId = systemTx.Id,
                TransactionType = "SystemOnly",
                Amount = systemTx.Amount,
                TransactionDate = systemTx.TransactionDate,
                Description = systemTx.Description,
                ExceptionType = "MissingInBank",
                ResolutionStatus = "Open"
            });
        }

        // Calculate outstanding checks and deposits in transit
        var outstandingChecks = request.SystemTransactions
            .Where(st => st.TransactionType == "Check" && !matchedSystemTransactions.Contains(st.Id))
            .Sum(st => st.Amount);

        var depositsInTransit = request.BankStatementTransactions
            .Where(bt => bt.TransactionType == "Deposit" && !matchedBankTransactions.Contains(bt.Id))
            .Sum(bt => bt.Amount);

        result.OutstandingChecks = outstandingChecks;
        result.DepositsInTransit = depositsInTransit;

        // Determine status
        if (Math.Abs(result.Difference) <= request.ToleranceAmount)
        {
            result.Status = "Reconciled";
        }
        else if (result.Matches.Count > 0)
        {
            result.Status = "Partial";
        }
        else
        {
            result.Status = "NotReconciled";
        }

        return result;
    }

    public async Task<IntercompanyReconciliationResult> PerformIntercompanyReconciliationAsync(IntercompanyReconciliationRequest request)
    {
        var result = new IntercompanyReconciliationResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ReconciliationLines = new List<IntercompanyReconciliationLine>(),
            Discrepancies = new List<IntercompanyDiscrepancy>(),
            Disputes = new List<IntercompanyDispute>(),
            AccountingEntries = new List<Journal>()
        };

        foreach (var accountId in request.IntercompanyAccountIds)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account != null)
            {
                // Get our balance for this intercompany account
                var ourBalance = await GetAccountBalanceAsync(accountId);

                // For each related company, get their balance
                foreach (var relatedCompanyId in request.RelatedCompanyIds)
                {
                    // In a real implementation, this would connect to the related company's system
                    // For this example, we'll use a placeholder value
                    var theirBalance = ourBalance * -1; // Opposite balance for intercompany

                    var reconciliationLine = new IntercompanyReconciliationLine
                    {
                        AccountId = accountId,
                        AccountName = account.Name,
                        RelatedCompanyId = relatedCompanyId,
                        RelatedCompanyName = $"Company {relatedCompanyId}", // Placeholder
                        OurBalance = ourBalance,
                        TheirBalance = theirBalance,
                        Difference = ourBalance + theirBalance, // Should be zero for balanced intercompany
                        LastReconciledDate = DateTime.Today.AddDays(-7), // Placeholder
                        Status = Math.Abs(ourBalance + theirBalance) < 0.01m ? "Matched" : "Unmatched"
                    };

                    result.ReconciliationLines.Add(reconciliationLine);

                    // Check for discrepancies
                    if (Math.Abs(reconciliationLine.Difference) > 0.01m)
                    {
                        result.Discrepancies.Add(new IntercompanyDiscrepancy
                        {
                            AccountId = accountId,
                            RelatedCompanyId = relatedCompanyId,
                            OurBalance = ourBalance,
                            TheirBalance = theirBalance,
                            Difference = reconciliationLine.Difference,
                            DiscrepancyType = "Amount",
                            ResolutionStatus = "Open"
                        });
                    }
                }
            }
        }

        // Calculate totals
        result.TotalIntercompanyBalance = result.ReconciliationLines.Sum(r => r.OurBalance);
        result.TotalDiscrepancy = result.Discrepancies.Sum(d => Math.Abs(d.Difference));

        // Determine status
        result.Status = result.TotalDiscrepancy < 0.01m ? "Balanced" :
                       result.ReconciliationLines.Any(r => r.Status == "Matched") ? "Partial" : "Unbalanced";

        return result;
    }

    public async Task<BalanceSheetReconciliationResult> PerformBalanceSheetReconciliationAsync(BalanceSheetReconciliationRequest request)
    {
        var result = new BalanceSheetReconciliationResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ReconciliationLines = new List<BalanceSheetReconciliationLine>(),
            Exceptions = new List<ReconciliationException>(),
            AccountingEntries = new List<Journal>(),
            SupportingSchedules = request.SupportingSchedules
        };

        foreach (var accountId in request.BalanceSheetAccountIds)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account != null)
            {
                // Get GL balance
                var glBalance = await GetAccountBalanceAsync(accountId);

                // Get schedule balance (sum of supporting schedules)
                var scheduleBalance = request.SupportingSchedules
                    .Where(ss => ss.Details.Any(sd => sd.Category == account.AccountNumber || sd.Description.Contains(account.Name)))
                    .Sum(ss => ss.ScheduleTotal);

                var reconciliationLine = new BalanceSheetReconciliationLine
                {
                    AccountId = accountId,
                    AccountNumber = account.AccountNumber,
                    AccountName = account.Name,
                    GLBalance = glBalance,
                    ScheduleBalance = scheduleBalance,
                    Difference = glBalance - scheduleBalance,
                    SupportingSchedules = request.SupportingSchedules
                        .Where(ss => ss.Details.Any(sd => sd.Category == account.AccountNumber || sd.Description.Contains(account.Name)))
                        .ToList(),
                    Status = Math.Abs(glBalance - scheduleBalance) < 0.01m ? "Reconciled" : "Unreconciled",
                    LastReconciledDate = DateTime.Today
                };

                result.ReconciliationLines.Add(reconciliationLine);

                // Add exception if not reconciled
                if (Math.Abs(reconciliationLine.Difference) > 0.01m)
                {
                    result.Exceptions.Add(new ReconciliationException
                    {
                        TransactionId = accountId,
                        TransactionType = "BalanceSheetAccount",
                        Amount = reconciliationLine.Difference,
                        TransactionDate = DateTime.Today,
                        Description = $"Balance sheet account {account.Name} difference: {reconciliationLine.Difference}",
                        ExceptionType = "BalanceMismatch",
                        ResolutionStatus = "Open"
                    });
                }
            }
        }

        // Calculate totals
        result.TotalGLBalance = result.ReconciliationLines.Sum(r => r.GLBalance);
        result.TotalScheduleBalance = result.ReconciliationLines.Sum(r => r.ScheduleBalance);
        result.TotalDifference = result.ReconciliationLines.Sum(r => r.Difference);

        // Determine status
        result.Status = Math.Abs(result.TotalDifference) < 0.01m ? "Reconciled" :
                       result.ReconciliationLines.Any(r => r.Status == "Reconciled") ? "Partial" : "NotReconciled";

        return result;
    }

    public async Task<InterimReconciliationResult> PerformInterimReconciliationAsync(InterimReconciliationRequest request)
    {
        var result = new InterimReconciliationResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ReconciliationLines = new List<InterimReconciliationLine>(),
            Exceptions = new List<ReconciliationException>(),
            AccountingEntries = new List<Journal>()
        };

        foreach (var accountId in request.AccrualAccountIds)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account != null)
            {
                // Get accrued amount (estimated based on historical patterns or other methods)
                var accruedAmount = await EstimateAccruedAmountAsync(accountId, request.PeriodEndDate);

                // Get actual amount (transactions up to current date)
                var actualAmount = await GetAccountBalanceUpToDateAsync(accountId, request.AsOfDate);

                var variance = actualAmount - accruedAmount;
                var variancePercentage = accruedAmount != 0 ? (variance / accruedAmount) * 100 : 0;

                var reconciliationLine = new InterimReconciliationLine
                {
                    AccountId = accountId,
                    AccountName = account.Name,
                    AccruedAmount = accruedAmount,
                    ActualAmount = actualAmount,
                    Variance = variance,
                    VariancePercentage = variancePercentage,
                    VarianceType = variance >= 0 ? "Favorable" : "Unfavorable",
                    Status = Math.Abs(variancePercentage) < 5 ? "Reconciled" : "Unreconciled" // 5% tolerance
                };

                result.ReconciliationLines.Add(reconciliationLine);

                // Add exception if variance is significant
                if (Math.Abs(variancePercentage) > 5) // More than 5% variance
                {
                    result.Exceptions.Add(new ReconciliationException
                    {
                        TransactionId = accountId,
                        TransactionType = "AccrualAccount",
                        Amount = variance,
                        TransactionDate = request.AsOfDate,
                        Description = $"Accrual account {account.Name} variance: {variance:F2} ({variancePercentage:F2}%)",
                        ExceptionType = "VarianceExceedsThreshold",
                        ResolutionStatus = "Open"
                    });
                }
            }
        }

        // Calculate totals
        result.TotalAccruedAmount = result.ReconciliationLines.Sum(r => r.AccruedAmount);
        result.TotalActualAmount = result.ReconciliationLines.Sum(r => r.ActualAmount);
        result.TotalVariance = result.ReconciliationLines.Sum(r => r.Variance);

        // Determine status
        result.Status = result.Exceptions.Count == 0 ? "Reconciled" :
                       result.ReconciliationLines.Any(r => r.Status == "Reconciled") ? "Partial" : "NotReconciled";

        return result;
    }

    public async Task<VarianceAnalysisResult> PerformVarianceAnalysisAsync(VarianceAnalysisRequest request)
    {
        var result = new VarianceAnalysisResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            VarianceLines = new List<VarianceAnalysisLine>(),
            Investigations = new List<VarianceInvestigation>()
        };

        foreach (var accountId in request.AccountIds)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account != null)
            {
                // Get budgeted amount (from budget records)
                var budgetedAmount = await GetBudgetedAmountAsync(accountId, request.StartDate, request.EndDate);

                // Get actual amount (from ledger entries)
                var actualAmount = await GetActualAmountAsync(accountId, request.StartDate, request.EndDate);

                var variance = actualAmount - budgetedAmount;
                var variancePercentage = budgetedAmount != 0 ? (variance / budgetedAmount) * 100 : 0;

                // Only include if variance exceeds threshold
                if (Math.Abs(variancePercentage) >= request.VarianceThreshold)
                {
                    var varianceLine = new VarianceAnalysisLine
                    {
                        AccountId = accountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.Name,
                        BudgetedAmount = budgetedAmount,
                        ActualAmount = actualAmount,
                        Variance = variance,
                        VariancePercentage = variancePercentage,
                        VarianceType = variance >= 0 ? "Favorable" : "Unfavorable",
                        VarianceCategory = CategorizeVariance(Math.Abs(variancePercentage)),
                        InvestigationStatus = "NotStarted"
                    };

                    result.VarianceLines.Add(varianceLine);

                    // Automatically create investigation for significant variances
                    if (Math.Abs(variancePercentage) > request.VarianceThreshold * 2) // Double the threshold
                    {
                        result.Investigations.Add(new VarianceInvestigation
                        {
                            InvestigationId = Guid.NewGuid(),
                            AccountId = accountId,
                            VarianceAmount = variance,
                            VariancePercentage = variancePercentage,
                            InvestigationStatus = "Open",
                            InvestigatorNotes = $"Auto-generated investigation for variance of {variancePercentage:F2}%",
                            RootCause = "Pending investigation",
                            CorrectiveAction = "Pending investigation",
                            InvestigationDate = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        // Calculate totals
        result.TotalVariance = result.VarianceLines.Sum(vl => vl.Variance);
        result.TotalVariancePercentage = result.VarianceLines.Any() ?
            result.VarianceLines.Average(vl => vl.VariancePercentage) : 0;

        // Determine status
        result.Status = result.Investigations.Count > 0 ? "RequiresAttention" :
                       result.VarianceLines.Count > 0 ? "Partial" : "Complete";

        return result;
    }

    public async Task<DocumentManagementResult> ManageReconciliationDocumentsAsync(DocumentManagementRequest request)
    {
        var result = new DocumentManagementResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ProcessedAttachments = new List<DocumentAttachment>(),
            Messages = new List<string>()
        };

        foreach (var attachment in request.Attachments)
        {
            // In a real implementation, this would save the document to a file system or cloud storage
            // For this example, we'll just validate and add to the result
            var processedAttachment = new DocumentAttachment
            {
                DocumentId = attachment.DocumentId,
                FileName = attachment.FileName,
                FileType = attachment.FileType,
                FileSize = attachment.FileSize,
                FileUrl = attachment.FileUrl,
                Description = attachment.Description,
                UploadDate = attachment.UploadDate,
                UploadedByUserId = attachment.UploadedByUserId,
                IsVerified = false, // Will be verified later
                VerificationDate = null,
                VerifiedByUserId = null
            };

            result.ProcessedAttachments.Add(processedAttachment);
        }

        // Add success message
        result.Messages.Add($"Successfully processed {request.Attachments.Count} document(s) for reconciliation {request.ReconciliationId}");

        // Determine status
        result.Status = "Success";

        return result;
    }

    #region Helper Methods

    private async Task<decimal> GetAccountBalanceAsync(Guid accountId)
    {
        var ledgerEntries = await _context.LedgerEntries
            .Where(le => le.AccountId == accountId)
            .ToListAsync();

        decimal totalDebits = ledgerEntries.Sum(le => le.Debit);
        decimal totalCredits = ledgerEntries.Sum(le => le.Credit);

        // For asset accounts, the balance is debits minus credits
        // For liability/equity/revenue accounts, it's credits minus debits
        var account = await _context.Accounts.FindAsync(accountId);
        if (account != null)
        {
            return account.AccountType switch
            {
                Domain.Enums.AccountType.Asset => totalDebits - totalCredits,
                Domain.Enums.AccountType.Expense => totalDebits - totalCredits,
                Domain.Enums.AccountType.Liability => totalCredits - totalDebits,
                Domain.Enums.AccountType.Equity => totalCredits - totalDebits,
                Domain.Enums.AccountType.Revenue => totalCredits - totalDebits,
                _ => totalDebits - totalCredits
            };
        }

        return totalDebits - totalCredits;
    }

    private async Task<decimal> GetAccountBalanceUpToDateAsync(Guid accountId, DateTime asOfDate)
    {
        var ledgerEntries = await _context.LedgerEntries
            .Where(le => le.AccountId == accountId && le.EntryDate <= asOfDate)
            .ToListAsync();

        decimal totalDebits = ledgerEntries.Sum(le => le.Debit);
        decimal totalCredits = ledgerEntries.Sum(le => le.Credit);

        var account = await _context.Accounts.FindAsync(accountId);
        if (account != null)
        {
            return account.AccountType switch
            {
                Domain.Enums.AccountType.Asset => totalDebits - totalCredits,
                Domain.Enums.AccountType.Expense => totalDebits - totalCredits,
                Domain.Enums.AccountType.Liability => totalCredits - totalDebits,
                Domain.Enums.AccountType.Equity => totalCredits - totalDebits,
                Domain.Enums.AccountType.Revenue => totalCredits - totalDebits,
                _ => totalDebits - totalCredits
            };
        }

        return totalDebits - totalCredits;
    }

    private async Task<decimal> EstimateAccruedAmountAsync(Guid accountId, DateTime periodEndDate)
    {
        // In a real implementation, this would estimate accrued amounts based on:
        // - Historical patterns
        // - Known commitments
        // - Usage patterns
        // For this example, returning a placeholder value based on historical average

        // Get historical average for this account
        var historicalAverage = await _context.LedgerEntries
            .Where(le => le.AccountId == accountId)
            .GroupBy(le => le.EntryDate.Month)
            .Select(g => g.Sum(le => le.Debit - le.Credit))
            .DefaultIfEmpty(0)
            .AverageAsync();

        return historicalAverage;
    }

    private async Task<decimal> GetBudgetedAmountAsync(Guid accountId, DateTime startDate, DateTime endDate)
    {
        // Get budgeted amount for the account in the specified period
        var budgetedAmount = await _context.BudgetLines
            .Where(bl => bl.AccountId == accountId)
            .SumAsync(bl => bl.BudgetAmount);

        return budgetedAmount;
    }

    private async Task<decimal> GetActualAmountAsync(Guid accountId, DateTime startDate, DateTime endDate)
    {
        // Get actual amount from ledger entries in the specified period
        var actualAmount = await _context.LedgerEntries
            .Where(le => le.AccountId == accountId &&
                        le.EntryDate >= startDate &&
                        le.EntryDate <= endDate)
            .SumAsync(le => le.Debit - le.Credit);

        return actualAmount;
    }

    private string CategorizeVariance(decimal variancePercentage)
    {
        if (variancePercentage < 5) return "OneTime";
        if (variancePercentage < 10) return "Operational";
        if (variancePercentage < 20) return "Timing";
        return "Structural";
    }

    #endregion
}
