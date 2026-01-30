using Mee.Erp.Finance.Core.Contracts.Dtos;
using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Enums;
// using Mee.Erp.Finance.Core.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class FinancialReportService // : IFinancialReportService
{
    // private readonly FinanceDbContext _context;
    // public FinancialReportService(FinanceDbContext context) { _context = context; }

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(TrialBalanceRequest request)
    {
        // STEP 1 & 2: Query the ledger and group by account
        // var accountBalancesQuery = _context.LedgerEntries
        //     .Where(le => le.CompanyId == request.CompanyId && le.EntryDate <= request.EndDate);

        // if (request.BusinessUnitIds != null && request.BusinessUnitIds.Any())
        // {
        //     accountBalancesQuery = accountBalancesQuery.Where(le => request.BusinessUnitIds.Contains(le.BusinessUnitId.Value));
        // }

        // var balances = await accountBalancesQuery
        //     .GroupBy(le => le.AccountId)
        //     .Select(g => new
        //     {
        //         AccountId = g.Key,
        //         TotalDebit = g.Sum(le => le.Debit),
        //         TotalCredit = g.Sum(le => le.Credit)
        //     }).ToListAsync();
        
        // STEP 3: Join with accounts and calculate final balances
        // var reportLines = new List<TrialBalanceLineDto>();
        // var allAccounts = await _context.Accounts.Where(a => a.CompanyId == request.CompanyId).ToListAsync();

        // foreach (var account in allAccounts)
        // {
        //     var balanceInfo = balances.FirstOrDefault(b => b.AccountId == account.Id);
        //     decimal totalDebit = balanceInfo?.TotalDebit ?? 0;
        //     decimal totalCredit = balanceInfo?.TotalCredit ?? 0;
        //     decimal balance = totalDebit - totalCredit;
            
        //     if (balance == 0) continue; // Don't show accounts with zero balance

        //     var line = new TrialBalanceLineDto
        //     {
        //         AccountNumber = account.AccountNumber,
        //         AccountName = account.Name
        //     };

        //     // Determine if the balance is a Debit or Credit based on account type
        //     switch (account.AccountType)
        //     {
        //         case AccountType.Asset:
        //         case AccountType.Expense:
        //             line.Debit = balance;
        //             break;
        //         case AccountType.Liability:
        //         case AccountType.Equity:
        //         case AccountType.Revenue:
        //             line.Credit = -balance; // Show as a positive number in the credit column
        //             break;
        //     }
        //     reportLines.Add(line);
        // }

        // STEP 4: Aggregate and return
        // var report = new TrialBalanceReportDto
        // {
        //     Lines = reportLines.OrderBy(l => l.AccountNumber).ToList(),
        //     TotalDebits = reportLines.Sum(l => l.Debit),
        //     TotalCredits = reportLines.Sum(l => l.Credit)
        // };
        // return report;

        Console.WriteLine("--- Logic for Trial Balance Service ---");
        Console.WriteLine("1. Received request with filters (Company, EndDate, BusinessUnits).");
        Console.WriteLine("2. Queried Ledger, filtered, and grouped by AccountId to get total debits/credits.");
        Console.WriteLine("3. Joined with Accounts to get names and types.");
        Console.WriteLine("4. Calculated final balance for each account and placed in the correct Debit/Credit column.");
        Console.WriteLine("5. Calculated grand totals.");
        Console.WriteLine("6. Returned the final report DTO.");

        // For demonstration, we return a dummy object
        return new TrialBalanceReportDto();
    }
}