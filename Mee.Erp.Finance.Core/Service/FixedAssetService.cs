using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class FixedAssetService : IFixedAssetService
{
	private readonly FinanceDbContext _context;
	private readonly IJournalPostingService _postingService;

	public FixedAssetService(FinanceDbContext context, IJournalPostingService postingService)
	{
		_context = context;
		_postingService = postingService;
	}

	public async Task RunDepreciationForMonthAsync(Guid companyId, DateTime periodEnd, Guid userId)
	{
		// 1. Get all active assets that haven't been disposed
		var assets = await _context.FixedAssets
			.Include(a => a.AssetCategory) // Need category for GL accounts
			.Where(a => a.CompanyId == companyId && !a.IsDisposed && a.DepreciationStartDate <= periodEnd)
			.ToListAsync();

		if (!assets.Any()) return;

		// 2. Create one big Depreciation Journal for the month
		var journal = new Journal
		{
			JournalDate = periodEnd,
			Description = $"Depreciation Run - {periodEnd:MM/yyyy}",
			CompanyId = companyId,
			Status = JournalStatus.Draft,
			CreatedBy = userId
		};

		foreach (var asset in assets)
		{
			// Check if already depreciated for this month
			bool alreadyRun = await _context.FixedAssetDepreciations
				.AnyAsync(d => d.FixedAssetId == asset.Id && d.DepreciationDate.Month == periodEnd.Month && d.DepreciationDate.Year == periodEnd.Year);

			if (alreadyRun) continue;

			// CALCULATE DEPRECIATION (Straight Line Example)
			// Monthly Charge = (Cost - Salvage) / LifeMonths
			decimal depreciableAmount = asset.PurchasePrice - asset.SalvageValue;
			decimal monthlyCharge = depreciableAmount / asset.UsefulLifeInMonths;

			// TODO: Ensure we don't depreciate past 0 or Salvage Value

			// Debit Expense
			journal.Entries.Add(new JournalEntry
			{
				AccountId = asset.AssetCategory.DepreciationExpenseAccountId,
				Debit = monthlyCharge,
				Credit = 0,
				Description = $"Depr: {asset.Name}"
			});

			// Credit Accumulated Depreciation (Contra-Asset)
			journal.Entries.Add(new JournalEntry
			{
				AccountId = asset.AssetCategory.AccumulatedDepreciationAccountId,
				Debit = 0,
				Credit = monthlyCharge,
				Description = $"Depr: {asset.Name}"
			});

			// Create History Record
			var history = new FixedAssetDepreciation
			{
				FixedAssetId = asset.Id,
				DepreciationDate = periodEnd,
				Amount = monthlyCharge
			};
			_context.FixedAssetDepreciations.Add(history);
		}

		if (journal.Entries.Any())
		{
			_context.Journals.Add(journal);
			await _context.SaveChangesAsync();

			// Link history to journal
			foreach (var entry in _context.FixedAssetDepreciations.Local)
			{
				if (entry.JournalId == Guid.Empty) entry.JournalId = journal.Id;
			}

			await _postingService.PostJournalAsync(journal.Id, userId);
		}
	}
}