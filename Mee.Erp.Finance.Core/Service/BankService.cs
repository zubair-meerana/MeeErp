using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class BankService : IBankService
{
	private readonly FinanceDbContext _context;

	public BankService(FinanceDbContext context)
	{
		_context = context;
	}

	public async Task<BankReconciliation> CreateReconciliationAsync(Guid bankAccountId, DateTime statementDate, decimal endingBalance)
	{
		// Logic: Calculate ERP Balance at that date
		var transactions = await _context.BankTransactions
			.Where(t => t.BankAccountId == bankAccountId && t.TransactionDate <= statementDate)
			.ToListAsync();

		decimal erpBalance = transactions.Sum(t => t.Deposit - t.Withdrawal);

		var rec = new BankReconciliation
		{
			BankAccountId = bankAccountId,
			StatementDate = statementDate,
			StatementEndingBalance = endingBalance,
			ErpEndingBalance = erpBalance
		};

		_context.BankReconciliations.Add(rec);
		await _context.SaveChangesAsync();
		return rec;
	}

	public async Task MatchTransactionAsync(Guid reconciliationId, Guid bankTransactionId)
	{
		var txn = await _context.BankTransactions.FindAsync(bankTransactionId);
		txn.BankReconciliationId = reconciliationId; // Mark as cleared
		await _context.SaveChangesAsync();
	}

	public async Task CompleteReconciliationAsync(Guid reconciliationId)
	{
		var rec = await _context.BankReconciliations.FindAsync(reconciliationId);

		// Re-calculate cleared balance
		var clearedTxns = await _context.BankTransactions
			.Where(t => t.BankReconciliationId == reconciliationId)
			.ToListAsync();

		decimal clearedBalance = clearedTxns.Sum(t => t.Deposit - t.Withdrawal);

		if (clearedBalance != rec.StatementEndingBalance)
		{
			throw new Exception("Reconciliation does not match statement balance.");
		}

		// Mark as finalized (would add a Status field to BankReconciliation entity)
		await _context.SaveChangesAsync();
	}
}