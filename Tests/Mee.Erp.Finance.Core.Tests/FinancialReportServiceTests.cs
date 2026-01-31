using FluentAssertions;
using Mee.Erp.Finance.Core.Contracts.Dtos;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests;

public class FinancialReportServiceTests
{
	private FinanceDbContext GetInMemoryDbContext()
	{
		var options = new DbContextOptionsBuilder<FinanceDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		return new FinanceDbContext(options);
	}

	[Fact]
	public async Task GetTrialBalanceAsync_Should_Calculate_Balances_Correctly()
	{
		// 1. ARRANGE
		using var context = GetInMemoryDbContext();
		var service = new FinancialReportService(context);

		var companyId = Guid.NewGuid();

		// Create Accounts
		var cashAccount = new Account { Id = Guid.NewGuid(), Name = "Cash", AccountNumber = "100", AccountType = AccountType.Asset, CompanyId = companyId };
		var revenueAccount = new Account { Id = Guid.NewGuid(), Name = "Sales", AccountNumber = "400", AccountType = AccountType.Revenue, CompanyId = companyId };
		var expenseAccount = new Account { Id = Guid.NewGuid(), Name = "Rent", AccountNumber = "500", AccountType = AccountType.Expense, CompanyId = companyId };

		context.Accounts.AddRange(cashAccount, revenueAccount, expenseAccount);

		// Create Ledger Entries (History)
		// Transaction 1: Sold items for 1000 Cash
		// Dr Cash 1000, Cr Sales 1000
		context.LedgerEntries.Add(new LedgerEntry { AccountId = cashAccount.Id, Debit = 1000, Credit = 0, EntryDate = DateTime.Today, JournalId = Guid.NewGuid() });
		context.LedgerEntries.Add(new LedgerEntry { AccountId = revenueAccount.Id, Debit = 0, Credit = 1000, EntryDate = DateTime.Today, JournalId = Guid.NewGuid() });

		// Transaction 2: Paid Rent 200 Cash
		// Dr Rent 200, Cr Cash 200
		context.LedgerEntries.Add(new LedgerEntry { AccountId = expenseAccount.Id, Debit = 200, Credit = 0, EntryDate = DateTime.Today, JournalId = Guid.NewGuid() });
		context.LedgerEntries.Add(new LedgerEntry { AccountId = cashAccount.Id, Debit = 0, Credit = 200, EntryDate = DateTime.Today, JournalId = Guid.NewGuid() });

		await context.SaveChangesAsync();

		// Expected Results:
		// Cash: 1000 Debit - 200 Credit = 800 Debit Balance
		// Sales: 1000 Credit Balance
		// Rent: 200 Debit Balance

		// 2. ACT
		var request = new TrialBalanceRequest
		{
			CompanyId = companyId,
			EndDate = DateTime.Today.AddDays(1)
		};
		var report = await service.GetTrialBalanceAsync(request);

		// 3. ASSERT
		// Check Grand Totals
		report.TotalDebits.Should().Be(1000); // 800 (Cash) + 200 (Rent)
		report.TotalCredits.Should().Be(1000); // 1000 (Sales)
		report.TotalDebits.Should().Be(report.TotalCredits); // It must balance!

		// Check Individual Lines
		report.Lines.Should().HaveCount(3);

		var cashLine = report.Lines.First(l => l.AccountNumber == "100");
		cashLine.Debit.Should().Be(800);
		cashLine.Credit.Should().Be(0);

		var salesLine = report.Lines.First(l => l.AccountNumber == "400");
		salesLine.Debit.Should().Be(0);
		salesLine.Credit.Should().Be(1000);
	}
}