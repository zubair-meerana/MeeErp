using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class AccountService : IAccountService
{
	private readonly FinanceDbContext _context;

	public AccountService(FinanceDbContext context)
	{
		_context = context;
	}

	public async Task<Account> CreateAccountAsync(Account account)
	{
		// Validation: Unique Account Number per Company
		bool exists = await _context.Accounts.AnyAsync(a =>
			a.CompanyId == account.CompanyId && a.AccountNumber == account.AccountNumber);

		if (exists) throw new Exception($"Account number {account.AccountNumber} already exists.");

		_context.Accounts.Add(account);
		await _context.SaveChangesAsync();
		return account;
	}

	public async Task<Account> UpdateAccountAsync(Account account)
	{
		var existing = await _context.Accounts.FindAsync(account.Id);
		if (existing == null) throw new Exception("Account not found");

		existing.Name = account.Name;
		existing.IsActive = account.IsActive;
		existing.ParentAccountId = account.ParentAccountId;
		existing.AccountingMethod = account.AccountingMethod;
		// Note: We typically don't allow changing AccountType or Number once created to preserve audit history

		await _context.SaveChangesAsync();
		return existing;
	}

	public async Task<List<Account>> GetChartOfAccountsAsync(Guid companyId)
	{
		// Return flat list; Frontend usually handles the tree conversion using ParentAccountId
		return await _context.Accounts
			.Where(a => a.CompanyId == companyId)
			.OrderBy(a => a.AccountNumber)
			.ToListAsync();
	}

public async Task<Account> GetAccountByIdAsync(Guid id)
	{
		return await _context.Accounts.FindAsync(id)
			?? throw new Exception("Account not found");
	}

	public async Task<Account?> GetAccountByNumberAsync(string accountNumber, Guid companyId)
	{
		return await _context.Accounts
			.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber && a.CompanyId == companyId);
}
}