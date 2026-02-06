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
/// Implements cash management and liquidity functions
/// </summary>
public class CashManagementService : ICashManagementService
{
    private readonly FinanceDbContext _context;

    public CashManagementService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CashFlowForecastLine>> GenerateCashFlowForecastAsync(Guid companyId, DateTime startDate, DateTime endDate)
    {
        var forecastLines = new List<CashFlowForecastLine>();
        var currentDate = startDate;

        // Get historical cash flow data to establish patterns
        var historicalCashFlows = await GetHistoricalCashFlowsAsync(companyId, startDate.AddMonths(-6), startDate.AddDays(-1));

        // Generate forecast for each day in the period
        while (currentDate <= endDate)
        {
            var dailyForecast = new CashFlowForecastLine
            {
                Date = currentDate,
                ExpectedInflow = 0,
                ExpectedOutflow = 0,
                Description = "Daily Cash Flow Forecast"
            };

            // Calculate expected inflows (collections from receivables, investments, etc.)
            var expectedInflows = await CalculateExpectedInflowsAsync(companyId, currentDate);
            dailyForecast.ExpectedInflow = expectedInflows;

            // Calculate expected outflows (payments to suppliers, expenses, etc.)
            var expectedOutflows = await CalculateExpectedOutflowsAsync(companyId, currentDate);
            dailyForecast.ExpectedOutflow = expectedOutflows;

            dailyForecast.NetCashFlow = dailyForecast.ExpectedInflow - dailyForecast.ExpectedOutflow;

            // Calculate cumulative balance (assuming we have opening balance)
            var openingBalance = await GetOpeningCashBalanceAsync(companyId, startDate);
            var daysFromStart = (currentDate - startDate).Days;
            dailyForecast.CumulativeBalance = openingBalance;

            // Calculate cumulative balance based on previous days
            for (int i = 0; i <= daysFromStart; i++)
            {
                var prevDate = startDate.AddDays(i);
                if (prevDate < currentDate)
                {
                    var prevForecast = forecastLines.FirstOrDefault(f => f.Date == prevDate);
                    if (prevForecast != null)
                    {
                        dailyForecast.CumulativeBalance = prevForecast.CumulativeBalance + dailyForecast.NetCashFlow;
                    }
                }
            }

            forecastLines.Add(dailyForecast);
            currentDate = currentDate.AddDays(1);
        }

        return forecastLines;
    }

    public async Task<LiquidityPosition> GetLiquidityPositionAsync(Guid companyId, DateTime asOfDate)
    {
        var liquidityPosition = new LiquidityPosition();

        // Calculate total cash and cash equivalents
        var cashAccounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId &&
                       (a.Name.ToLower().Contains("cash") ||
                        a.Name.ToLower().Contains("checking") ||
                        a.Name.ToLower().Contains("savings") ||
                        a.AccountNumber.StartsWith("10") || // Common cash account prefix
                        a.AccountType == Domain.Enums.AccountType.Asset))
            .ToListAsync();

        var cashAccountIds = cashAccounts.Select(a => a.Id).ToList();
        liquidityPosition.TotalCashAndCashEquivalents = await CalculateAccountBalancesAsync(cashAccountIds, asOfDate);

        // Calculate available credit lines (would come from a credit facility table in real implementation)
        liquidityPosition.AvailableCreditLines = 500000; // Placeholder

        // Calculate short term investments
        var investmentAccounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId &&
                       (a.Name.ToLower().Contains("investment") ||
                        a.Name.ToLower().Contains("marketable securities") ||
                        a.AccountNumber.StartsWith("11"))) // Common investment account prefix
            .ToListAsync();

        var investmentAccountIds = investmentAccounts.Select(a => a.Id).ToList();
        liquidityPosition.ShortTermInvestments = await CalculateAccountBalancesAsync(investmentAccountIds, asOfDate);

        // Calculate current liabilities
        var currentLiabilityAccounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId &&
                       (a.AccountType == Domain.Enums.AccountType.Liability &&
                        (a.Name.ToLower().Contains("current") ||
                         a.AccountNumber.StartsWith("20")))) // Common current liability prefix
            .ToListAsync();

        var currentLiabilityAccountIds = currentLiabilityAccounts.Select(a => a.Id).ToList();
        liquidityPosition.CurrentLiabilities = await CalculateAccountBalancesAsync(currentLiabilityAccountIds, asOfDate);

        // Calculate net liquidity
        liquidityPosition.NetLiquidity = liquidityPosition.TotalCashAndCashEquivalents +
                                       liquidityPosition.ShortTermInvestments -
                                       liquidityPosition.CurrentLiabilities;

        // Calculate liquidity ratio (current assets / current liabilities)
        var currentAssets = liquidityPosition.TotalCashAndCashEquivalents + liquidityPosition.ShortTermInvestments;
        liquidityPosition.LiquidityRatio = liquidityPosition.CurrentLiabilities != 0 ?
                                         currentAssets / liquidityPosition.CurrentLiabilities : 0;

        return liquidityPosition;
    }

    public async Task<bool> ProcessCashPoolingAsync(Guid companyId, IEnumerable<Guid> bankAccountIds)
    {
        // Cash pooling involves consolidating balances across multiple accounts
        // This is typically done for optimizing interest income and reducing borrowing costs

        var accounts = await _context.BankAccounts
            .Where(ba => bankAccountIds.Contains(ba.Id) && ba.CompanyId == companyId)
            .ToListAsync();

        if (!accounts.Any())
        {
            return false;
        }

        // Calculate total pool balance
        var totalPoolBalance = accounts.Sum(a => a.CurrentBalance);

        // Determine target allocations based on predefined rules
        // This is a simplified approach - real pooling would be more complex
        var primaryAccount = accounts.OrderByDescending(a => a.CurrentBalance).FirstOrDefault();
        if (primaryAccount == null)
        {
            return false;
        }

        // Move excess funds to primary account (simplified approach)
        // In reality, this would involve actual fund transfers between banks
        foreach (var account in accounts.Where(a => a.Id != primaryAccount.Id))
        {
            if (account.CurrentBalance > 10000) // Threshold for pooling
            {
                // Create a journal entry to represent the theoretical movement
                var journal = new Journal
                {
                    JournalDate = DateTime.Today,
                    Description = $"Cash pooling transfer from {account.AccountName} to {primaryAccount.AccountName}",
                    CompanyId = companyId,
                    Status = Domain.Enums.JournalStatus.Draft
                };

                journal.Entries.Add(new JournalEntry
                {
                    AccountId = GetAccountIdByName(companyId, account.AccountName).Result,
                    Debit = 0,
                    Credit = account.CurrentBalance - 1000, // Leave minimal balance
                    Description = "Cash pooling outflow"
                });

                journal.Entries.Add(new JournalEntry
                {
                    AccountId = GetAccountIdByName(companyId, primaryAccount.AccountName).Result,
                    Debit = account.CurrentBalance - 1000, // Amount transferred
                    Credit = 0,
                    Description = "Cash pooling inflow"
                });

                _context.Journals.Add(journal);
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<InvestmentIncome>> GetInvestmentIncomeAsync(Guid companyId, DateTime startDate, DateTime endDate)
    {
        var investmentIncome = new List<InvestmentIncome>();

        // Get investment-related journals within the date range
        var investmentJournals = await _context.Journals
            .Include(j => j.Entries)
            .ThenInclude(e => e.Account)
            .Where(j => j.CompanyId == companyId &&
                       j.JournalDate >= startDate &&
                       j.JournalDate <= endDate &&
                       (j.Description.ToLower().Contains("investment") ||
                        j.Description.ToLower().Contains("dividend") ||
                        j.Description.ToLower().Contains("interest income")))
            .ToListAsync();

        foreach (var journal in investmentJournals)
        {
            foreach (var entry in journal.Entries)
            {
                var account = await _context.Accounts.FindAsync(entry.AccountId);
                if (account != null &&
                    (account.Name.ToLower().Contains("investment") ||
                     account.Name.ToLower().Contains("dividend") ||
                     account.Name.ToLower().Contains("interest income")))
                {
                    investmentIncome.Add(new InvestmentIncome
                    {
                        InvestmentId = journal.Id, // Using journal ID as investment ID for simplicity
                        InvestmentDescription = journal.Description,
                        IncomeAmount = entry.Credit, // Income is typically credited
                        IncomeDate = journal.JournalDate,
                        IncomeType = account.Name
                    });
                }
            }
        }

        return investmentIncome;
    }

    public async Task<CashPositionReport> GenerateCashPositionReportAsync(Guid companyId, DateTime asOfDate)
    {
        var report = new CashPositionReport
        {
            ReportDate = asOfDate,
            CashPositionsByAccount = new Dictionary<string, decimal>(),
            CashFlowProjection = new List<CashFlowForecastLine>()
        };

        // Get all bank accounts for the company
        var bankAccounts = await _context.BankAccounts
            .Where(ba => ba.CompanyId == companyId)
            .ToListAsync();

        foreach (var account in bankAccounts)
        {
            report.CashPositionsByAccount[account.AccountName] = account.CurrentBalance;
        }

        report.TotalCashPosition = report.CashPositionsByAccount.Values.Sum();

        // Generate short-term cash flow projection (next 30 days)
        var projectionStartDate = asOfDate;
        var projectionEndDate = asOfDate.AddDays(30);

        var forecast = await GenerateCashFlowForecastAsync(companyId, projectionStartDate, projectionEndDate);
        report.CashFlowProjection = forecast.Take(30).ToList(); // Take first 30 days

        if (report.CashFlowProjection.Any())
        {
            report.DailyCashFlowProjection = report.CashFlowProjection.Average(f => f.NetCashFlow);
        }

        return report;
    }

    #region Helper Methods

    private async Task<decimal> GetOpeningCashBalanceAsync(Guid companyId, DateTime asOfDate)
    {
        // Get the opening balance for cash accounts as of the given date
        var cashAccounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId &&
                       (a.Name.ToLower().Contains("cash") ||
                        a.Name.ToLower().Contains("checking") ||
                        a.Name.ToLower().Contains("savings")))
            .ToListAsync();

        var cashAccountIds = cashAccounts.Select(a => a.Id).ToList();
        return await CalculateAccountBalancesAsync(cashAccountIds, asOfDate);
    }

    private async Task<decimal> CalculateAccountBalancesAsync(IEnumerable<Guid> accountIds, DateTime asOfDate)
    {
        var ledgerEntries = await _context.LedgerEntries
            .Where(le => accountIds.Contains(le.AccountId) && le.EntryDate <= asOfDate)
            .ToListAsync();

        decimal totalDebits = ledgerEntries.Sum(le => le.Debit);
        decimal totalCredits = ledgerEntries.Sum(le => le.Credit);

        // For asset accounts (cash), the normal balance is debit
        return totalDebits - totalCredits;
    }

    private async Task<decimal> CalculateExpectedInflowsAsync(Guid companyId, DateTime date)
    {
        // Calculate expected cash inflows based on:
        // - Outstanding receivables with due dates
        // - Investment income
        // - Loan proceeds
        // - Other expected receipts

        // This is a simplified calculation
        var expectedInflows = 0m;

        // Get receivables that are due around this date
        var salesInvoices = await _context.SalesInvoices
            .Where(si => si.CompanyId == companyId &&
                        si.InvoiceDate <= date &&
                        si.DueDate >= date &&
                        si.Status != Domain.Enums.SalesInvoiceStatus.Paid)
            .ToListAsync();

        expectedInflows += salesInvoices.Sum(si => si.TotalAmount - si.AmountPaid);

        // Add any scheduled investment income
        var investmentIncome = await _context.Journals
            .Where(j => j.CompanyId == companyId &&
                       j.JournalDate == date &&
                       j.Description.ToLower().Contains("investment income"))
            .SumAsync(j => j.Entries.Sum(e => e.Credit));

        expectedInflows += investmentIncome;

        return expectedInflows;
    }

    private async Task<decimal> CalculateExpectedOutflowsAsync(Guid companyId, DateTime date)
    {
        // Calculate expected cash outflows based on:
        // - Outstanding payables with due dates
        // - Scheduled loan payments
        // - Operating expenses
        // - Other expected payments

        // This is a simplified calculation
        var expectedOutflows = 0m;

        // Get payables that are due around this date
        var purchaseInvoices = await _context.PurchaseInvoices
            .Where(pi => pi.CompanyId == companyId &&
                        pi.InvoiceDate <= date &&
                        pi.DueDate >= date &&
                        pi.Status != Domain.Enums.PurchaseInvoiceStatus.Paid)
            .ToListAsync();

        expectedOutflows += purchaseInvoices.Sum(pi => pi.TotalAmount - pi.AmountPaid);

        // Add any scheduled operating expenses
        var operatingExpenses = await _context.Journals
            .Where(j => j.CompanyId == companyId &&
                       j.JournalDate == date &&
                       j.Description.ToLower().Contains("operating expense"))
            .SumAsync(j => j.Entries.Sum(e => e.Debit));

        expectedOutflows += operatingExpenses;

        return expectedOutflows;
    }

    private async Task<IEnumerable<dynamic>> GetHistoricalCashFlowsAsync(Guid companyId, DateTime startDate, DateTime endDate)
    {
        // Get historical cash flow data for forecasting
        // This would typically come from actual cash transactions
        return new List<dynamic>(); // Placeholder
    }

    private async Task<Guid> GetAccountIdByName(Guid companyId, string accountName)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Name == accountName);
        return account?.Id ?? Guid.Empty;
    }

    #endregion
}
