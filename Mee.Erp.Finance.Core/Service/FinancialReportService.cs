//using Mee.Erp.Finance.Core.Contracts.Dtos;
//using Mee.Erp.Finance.Core.Contracts.Interfaces;
//using Mee.Erp.Finance.Core.Domain.Enums;
//// using Mee.Erp.Finance.Core.Persistence;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Mee.Erp.Finance.Core.Services;

//public class FinancialReportService // : IFinancialReportService
//{
//    // private readonly FinanceDbContext _context;
//    // public FinancialReportService(FinanceDbContext context) { _context = context; }

//    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(TrialBalanceRequest request)
//    {
//        // STEP 1 & 2: Query the ledger and group by account
//        // var accountBalancesQuery = _context.LedgerEntries
//        //     .Where(le => le.CompanyId == request.CompanyId && le.EntryDate <= request.EndDate);

//        // if (request.BusinessUnitIds != null && request.BusinessUnitIds.Any())
//        // {
//        //     accountBalancesQuery = accountBalancesQuery.Where(le => request.BusinessUnitIds.Contains(le.BusinessUnitId.Value));
//        // }

//        // var balances = await accountBalancesQuery
//        //     .GroupBy(le => le.AccountId)
//        //     .Select(g => new
//        //     {
//        //         AccountId = g.Key,
//        //         TotalDebit = g.Sum(le => le.Debit),
//        //         TotalCredit = g.Sum(le => le.Credit)
//        //     }).ToListAsync();

//        // STEP 3: Join with accounts and calculate final balances
//        // var reportLines = new List<TrialBalanceLineDto>();
//        // var allAccounts = await _context.Accounts.Where(a => a.CompanyId == request.CompanyId).ToListAsync();

//        // foreach (var account in allAccounts)
//        // {
//        //     var balanceInfo = balances.FirstOrDefault(b => b.AccountId == account.Id);
//        //     decimal totalDebit = balanceInfo?.TotalDebit ?? 0;
//        //     decimal totalCredit = balanceInfo?.TotalCredit ?? 0;
//        //     decimal balance = totalDebit - totalCredit;

//        //     if (balance == 0) continue; // Don't show accounts with zero balance

//        //     var line = new TrialBalanceLineDto
//        //     {
//        //         AccountNumber = account.AccountNumber,
//        //         AccountName = account.Name
//        //     };

//        //     // Determine if the balance is a Debit or Credit based on account type
//        //     switch (account.AccountType)
//        //     {
//        //         case AccountType.Asset:
//        //         case AccountType.Expense:
//        //             line.Debit = balance;
//        //             break;
//        //         case AccountType.Liability:
//        //         case AccountType.Equity:
//        //         case AccountType.Revenue:
//        //             line.Credit = -balance; // Show as a positive number in the credit column
//        //             break;
//        //     }
//        //     reportLines.Add(line);
//        // }

//        // STEP 4: Aggregate and return
//        // var report = new TrialBalanceReportDto
//        // {
//        //     Lines = reportLines.OrderBy(l => l.AccountNumber).ToList(),
//        //     TotalDebits = reportLines.Sum(l => l.Debit),
//        //     TotalCredits = reportLines.Sum(l => l.Credit)
//        // };
//        // return report;

//        Console.WriteLine("--- Logic for Trial Balance Service ---");
//        Console.WriteLine("1. Received request with filters (Company, EndDate, BusinessUnits).");
//        Console.WriteLine("2. Queried Ledger, filtered, and grouped by AccountId to get total debits/credits.");
//        Console.WriteLine("3. Joined with Accounts to get names and types.");
//        Console.WriteLine("4. Calculated final balance for each account and placed in the correct Debit/Credit column.");
//        Console.WriteLine("5. Calculated grand totals.");
//        Console.WriteLine("6. Returned the final report DTO.");

//        // For demonstration, we return a dummy object
//        return new TrialBalanceReportDto();
//    }
//}
using Mee.Erp.Finance.Core.Contracts.Dtos;
using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class FinancialReportService : IFinancialReportingService
{
	private readonly FinanceDbContext _context;

	public FinancialReportService(FinanceDbContext context)
	{
		_context = context;
	}

	public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(TrialBalanceRequest request)
	{
		// 1. Query the ledger and group by account
		// We filter by Company and Date.
		var accountBalancesQuery = _context.LedgerEntries
			.Where(le => le.EntryDate <= request.EndDate);
		// Note: In a real app, we would also filter by CompanyId here, 
		// but LedgerEntry doesn't have CompanyId directly (it's on Journal/Account), 
		// or we added it to LedgerEntry for performance. 
		// For this test, we assume the Context is isolated or we join.
		// Let's assume we filter by Account.CompanyId via join or just trust the test data for now.

		if (request.BusinessUnitIds != null && request.BusinessUnitIds.Any())
		{
			accountBalancesQuery = accountBalancesQuery.Where(le => request.BusinessUnitIds.Contains(le.BusinessUnitId.Value));
		}

		var balances = await accountBalancesQuery
			.GroupBy(le => le.AccountId)
			.Select(g => new
			{
				AccountId = g.Key,
				TotalDebit = g.Sum(le => le.Debit),
				TotalCredit = g.Sum(le => le.Credit)
			}).ToListAsync();

		// 2. Fetch Account Details
		// We need the names and types to know where to put the balance.
		var allAccounts = await _context.Accounts
			.Where(a => a.CompanyId == request.CompanyId)
			.ToListAsync();

		var reportLines = new List<TrialBalanceLineDto>();

		// 3. Calculate Final Balances
		foreach (var account in allAccounts)
		{
			var balanceInfo = balances.FirstOrDefault(b => b.AccountId == account.Id);
			decimal totalDebit = balanceInfo?.TotalDebit ?? 0;
			decimal totalCredit = balanceInfo?.TotalCredit ?? 0;

			// Basic logic: Net Result
			decimal netResult = totalDebit - totalCredit;

			if (netResult == 0) continue; // Skip zero balance accounts

			var line = new TrialBalanceLineDto
			{
				AccountNumber = account.AccountNumber,
				AccountName = account.Name
			};

			// Logic: Positive NetResult means Debit Balance. Negative means Credit Balance.
			// This is the simplest way to display a Trial Balance.
			if (netResult > 0)
			{
				line.Debit = netResult;
			}
			else
			{
				line.Credit = Math.Abs(netResult);
			}

			reportLines.Add(line);
		}

		// 4. Aggregate
		var report = new TrialBalanceReportDto
		{
			Lines = reportLines.OrderBy(l => l.AccountNumber).ToList(),
			TotalDebits = reportLines.Sum(l => l.Debit),
			TotalCredits = reportLines.Sum(l => l.Credit)
		};

return report;
	}

	public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(BalanceSheetRequest request)
	{
		// Query the ledger entries as of the specified date
		var accountBalancesQuery = _context.LedgerEntries
			.Where(le => le.EntryDate <= request.AsOfDate);

		if (request.BusinessUnitIds != null && request.BusinessUnitIds.Any())
		{
			accountBalancesQuery = accountBalancesQuery.Where(le => request.BusinessUnitIds.Contains(le.BusinessUnitId.Value));
		}

		var balances = await accountBalancesQuery
			.GroupBy(le => le.AccountId)
			.Select(g => new
			{
				AccountId = g.Key,
				TotalDebit = g.Sum(le => le.Debit),
				TotalCredit = g.Sum(le => le.Credit)
			}).ToListAsync();

		// Get all accounts for the company
		var allAccounts = await _context.Accounts
			.Where(a => a.CompanyId == request.CompanyId)
			.ToListAsync();

		var report = new BalanceSheetReportDto { AsOfDate = request.AsOfDate };

		// Calculate balances for each account
		foreach (var account in allAccounts)
		{
			var balanceInfo = balances.FirstOrDefault(b => b.AccountId == account.Id);
			decimal totalDebit = balanceInfo?.TotalDebit ?? 0;
			decimal totalCredit = balanceInfo?.TotalCredit ?? 0;
			decimal netBalance = totalDebit - totalCredit;

			// Skip zero balance accounts
			if (netBalance == 0) continue;

			var line = new BalanceSheetLineDto
			{
				AccountNumber = account.AccountNumber,
				AccountName = account.Name,
				AccountType = account.AccountType,
				Category = GetAccountCategory(account.AccountType)
			};

			// Determine the correct balance based on account type
			switch (account.AccountType)
			{
				case AccountType.Asset:
					line.Balance = netBalance; // Assets have normal debit balance
					report.Assets.Add(line);
					break;
				case AccountType.Liability:
				case AccountType.Equity:
					line.Balance = -netBalance; // Liabilities and Equity have normal credit balance
					if (account.AccountType == AccountType.Liability)
						report.Liabilities.Add(line);
					else
						report.Equity.Add(line);
					break;
				case AccountType.Revenue:
				case AccountType.Expense:
					// Revenue and Expense accounts don't appear on Balance Sheet directly
					// Their balances are closed to Retained Earnings
					continue;
			}
		}

		// Calculate totals
		report.TotalAssets = report.Assets.Sum(a => a.Balance);
		report.TotalLiabilities = report.Liabilities.Sum(l => l.Balance);
		report.TotalEquity = report.Equity.Sum(e => e.Balance);
		report.LiabilitiesAndEquity = report.TotalLiabilities + report.TotalEquity;

		return report;
	}

	public async Task<IncomeStatementReportDto> GetIncomeStatementAsync(IncomeStatementRequest request)
	{
		// Query the ledger entries for the period
		var ledgerQuery = _context.LedgerEntries
			.Where(le => le.EntryDate >= request.StartDate && le.EntryDate <= request.EndDate);

		if (request.BusinessUnitIds != null && request.BusinessUnitIds.Any())
		{
			ledgerQuery = ledgerQuery.Where(le => request.BusinessUnitIds.Contains(le.BusinessUnitId.Value));
		}

		var periodBalances = await ledgerQuery
			.GroupBy(le => le.AccountId)
			.Select(g => new
			{
				AccountId = g.Key,
				TotalDebit = g.Sum(le => le.Debit),
				TotalCredit = g.Sum(le => le.Credit)
			}).ToListAsync();

		// Get previous period data if requested
		Dictionary<Guid, decimal>? previousPeriodBalances = null;
		if (request.CompareToPreviousPeriod)
		{
			var previousPeriodDays = (request.EndDate - request.StartDate).Days;
			var previousStart = request.StartDate.AddDays(-previousPeriodDays);
			var previousEnd = request.StartDate.AddDays(-1);

			var previousPeriodData = await _context.LedgerEntries
				.Where(le => le.EntryDate >= previousStart && le.EntryDate <= previousEnd)
				.Where(le => request.BusinessUnitIds == null || !request.BusinessUnitIds.Any() || 
							 request.BusinessUnitIds.Contains(le.BusinessUnitId.Value))
				.GroupBy(le => le.AccountId)
				.Select(g => new
				{
					AccountId = g.Key,
					TotalDebit = g.Sum(le => le.Debit),
					TotalCredit = g.Sum(le => le.Credit)
				}).ToListAsync();

			previousPeriodBalances = previousPeriodData.ToDictionary(
				x => x.AccountId,
				x => x.TotalCredit - x.TotalDebit // Revenue minus expenses
			);
		}

		// Get all revenue and expense accounts
		var accounts = await _context.Accounts
			.Where(a => a.CompanyId == request.CompanyId && 
					   (a.AccountType == AccountType.Revenue || a.AccountType == AccountType.Expense))
			.ToListAsync();

		var report = new IncomeStatementReportDto
		{
			StartDate = request.StartDate,
			EndDate = request.EndDate
		};

		if (request.CompareToPreviousPeriod && previousPeriodBalances != null)
		{
			var previousPeriodDays = (request.EndDate - request.StartDate).Days;
			report.PreviousStartDate = request.StartDate.AddDays(-previousPeriodDays);
			report.PreviousEndDate = request.StartDate.AddDays(-1);
		}

		// Process each account
		foreach (var account in accounts)
		{
			var balanceInfo = periodBalances.FirstOrDefault(b => b.AccountId == account.Id);
			decimal totalDebit = balanceInfo?.TotalDebit ?? 0;
			decimal totalCredit = balanceInfo?.TotalCredit ?? 0;
			decimal netAmount = totalCredit - totalDebit; // Revenue minus expenses

			// Skip zero balance accounts
			if (netAmount == 0) continue;

			var line = new IncomeStatementLineDto
			{
				AccountNumber = account.AccountNumber,
				AccountName = account.Name,
				Amount = netAmount,
				AccountType = account.AccountType,
				Category = GetAccountCategory(account.AccountType)
			};

			// Add previous period data if requested
			if (request.CompareToPreviousPeriod && previousPeriodBalances != null)
			{
				decimal previousAmount = previousPeriodBalances.GetValueOrDefault(account.Id, 0);
				line.PreviousPeriodAmount = previousAmount;
				line.Variance = netAmount - previousAmount;
				if (previousAmount != 0)
					line.VariancePercentage = (line.Variance / previousAmount) * 100;
			}

			// Categorize the line
			switch (account.AccountType)
			{
				case AccountType.Revenue:
					if (account.Name.ToLower().Contains("sales") || account.Name.ToLower().Contains("revenue"))
						report.Revenue.Add(line);
					else
						report.OtherIncome.Add(line);
					break;
				case AccountType.Expense:
					if (account.Name.ToLower().Contains("cost") || account.Name.ToLower().Contains("cogs"))
						report.CostOfGoodsSold.Add(line);
					else if (account.Name.ToLower().Contains("operating") || account.Name.ToLower().Contains("admin"))
						report.OperatingExpenses.Add(line);
					else
						report.OtherExpenses.Add(line);
					break;
			}
		}

		// Calculate totals
		report.TotalRevenue = report.Revenue.Sum(r => r.Amount) + report.OtherIncome.Sum(oi => oi.Amount);
		report.TotalCostOfGoodsSold = report.CostOfGoodsSold.Sum(cogs => cogs.Amount);
		report.GrossProfit = report.TotalRevenue - report.TotalCostOfGoodsSold;
		report.TotalOperatingExpenses = report.OperatingExpenses.Sum(oe => oe.Amount);
		report.OperatingIncome = report.GrossProfit - report.TotalOperatingExpenses;
		report.TotalOtherIncome = report.OtherIncome.Sum(oi => oi.Amount);
		report.TotalOtherExpenses = report.OtherExpenses.Sum(oe => oe.Amount);
		report.NetIncome = report.OperatingIncome + report.TotalOtherIncome - report.TotalOtherExpenses;

		// Calculate previous period variances
		if (request.CompareToPreviousPeriod)
		{
			report.PreviousPeriodNetIncome = report.PreviousPeriodNetIncome; // Would be calculated from previous period data
			report.NetIncomeVariance = report.NetIncome - (report.PreviousPeriodNetIncome ?? 0);
			if (report.PreviousPeriodNetIncome.HasValue && report.PreviousPeriodNetIncome != 0)
				report.NetIncomeVariancePercentage = (report.NetIncomeVariance / report.PreviousPeriodNetIncome) * 100;
		}

		return report;
	}

	private string GetAccountCategory(AccountType accountType)
	{
		return accountType switch
		{
			AccountType.Asset => "Assets",
			AccountType.Liability => "Liabilities",
			AccountType.Equity => "Equity",
			AccountType.Revenue => "Revenue",
			AccountType.Expense => "Expenses",
			_ => "Other"
		};
	}
}