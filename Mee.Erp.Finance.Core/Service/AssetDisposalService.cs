using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class AssetDisposalService : IAssetDisposalService
{
    private readonly FinanceDbContext _context;
    private readonly IJournalPostingService _journalPostingService;
    private readonly IFixedAssetService _fixedAssetService;

    public AssetDisposalService(FinanceDbContext context, IJournalPostingService journalPostingService, IFixedAssetService fixedAssetService)
    {
        _context = context;
        _journalPostingService = journalPostingService;
        _fixedAssetService = fixedAssetService;
    }

    public async Task<FixedAsset> DisposeAssetAsync(Guid assetId, DateTime disposalDate, decimal disposalValue, string reason)
    {
        var asset = await _context.FixedAssets
            .Include(fa => fa.AssetCategory)
            .FirstOrDefaultAsync(fa => fa.Id == assetId)
            ?? throw new ArgumentException("Asset not found");

        if (asset.IsDisposed)
        {
            throw new InvalidOperationException("Asset is already disposed");
        }

        // Calculate accumulated depreciation
        var accumulatedDepreciation = await _context.FixedAssetDepreciations
            .Where(fd => fd.FixedAssetId == assetId)
            .SumAsync(fd => fd.Amount);

        // Calculate book value
        var bookValue = asset.PurchasePrice - accumulatedDepreciation;

        // Calculate gain or loss on disposal
        var gainLoss = disposalValue - bookValue;

        // Create disposal journal entry
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            Description = $"Disposal of asset {asset.Name} - {reason}",
            JournalDate = disposalDate,
            Status = JournalStatus.Draft,
            CompanyId = asset.CompanyId
        };

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        // Get relevant accounts
        var assetAccount = await GetAssetAccountAsync(asset.AssetCategoryId, asset.CompanyId);
        var accumulatedDepreciationAccount = await GetAccumulatedDepreciationAccountAsync(asset.AssetCategoryId, asset.CompanyId);
        var disposalAccount = await GetDisposalAccountAsync(asset.CompanyId);
        var gainLossAccount = await GetGainLossAccountAsync(asset.CompanyId, gainLoss >= 0);

        // Create journal entries for disposal
        // 1. Remove asset from books (credit asset account)
        _context.JournalEntries.Add(new JournalEntry
        {
            Id = Guid.NewGuid(),
            JournalId = journal.Id,
            AccountId = assetAccount.Id,
            Description = $"Remove {asset.Name} from asset register",
            Debit = 0,
            Credit = asset.PurchasePrice
        });

        // 2. Remove accumulated depreciation (debit accumulated depreciation account)
        if (accumulatedDepreciation > 0)
        {
            _context.JournalEntries.Add(new JournalEntry
            {
                Id = Guid.NewGuid(),
                JournalId = journal.Id,
                AccountId = accumulatedDepreciationAccount.Id,
                Description = $"Remove accumulated depreciation for {asset.Name}",
                Debit = accumulatedDepreciation,
                Credit = 0
            });
        }

        // 3. Record disposal proceeds (debit cash/bank account)
        if (disposalValue > 0)
        {
            _context.JournalEntries.Add(new JournalEntry
            {
                Id = Guid.NewGuid(),
                JournalId = journal.Id,
                AccountId = disposalAccount.Id,
                Description = $"Proceeds from disposal of {asset.Name}",
                Debit = disposalValue,
                Credit = 0
            });
        }

        // 4. Record gain or loss on disposal
        if (Math.Abs(gainLoss) > 0.01m) // Only record if material
        {
            _context.JournalEntries.Add(new JournalEntry
            {
                Id = Guid.NewGuid(),
                JournalId = journal.Id,
                AccountId = gainLossAccount.Id,
                Description = $"{(gainLoss >= 0 ? "Gain" : "Loss")} on disposal of {asset.Name}",
                Debit = gainLoss > 0 ? 0 : Math.Abs(gainLoss),
                Credit = gainLoss > 0 ? gainLoss : 0
            });
        }

        // Post the journal
        await _journalPostingService.PostJournalAsync(journal.Id, Guid.NewGuid()); // TODO: Get actual user ID

        // Update asset status
        asset.IsDisposed = true;
        asset.DisposalDate = disposalDate;

        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task<FixedAsset> TransferAssetAsync(Guid assetId, Guid newBusinessUnitId, DateTime transferDate, string reason)
    {
        var asset = await _context.FixedAssets
            .FirstOrDefaultAsync(fa => fa.Id == assetId)
            ?? throw new ArgumentException("Asset not found");

        if (asset.IsDisposed)
        {
            throw new InvalidOperationException("Cannot transfer disposed asset");
        }

        // Create transfer journal entry
        var journal = new Journal
        {
            Id = Guid.NewGuid(),
            Description = $"Transfer of asset {asset.Name} to business unit - {reason}",
            JournalDate = transferDate,
            Status = JournalStatus.Draft,
            CompanyId = asset.CompanyId
        };

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        // Get source and destination asset accounts
        var sourceAccount = await GetAssetAccountAsync(asset.AssetCategoryId, asset.CompanyId);
        var destinationAccount = await GetAssetAccountAsync(asset.AssetCategoryId, newBusinessUnitId);

        // Create transfer entries (simplified - in reality you'd need more complex tracking)
        _context.JournalEntries.Add(new JournalEntry
        {
            Id = Guid.NewGuid(),
            JournalId = journal.Id,
            AccountId = sourceAccount.Id,
            Description = $"Transfer out: {asset.Name}",
            Debit = 0,
            Credit = asset.PurchasePrice
        });

        _context.JournalEntries.Add(new JournalEntry
        {
            Id = Guid.NewGuid(),
            JournalId = journal.Id,
            AccountId = destinationAccount.Id,
            Description = $"Transfer in: {asset.Name}",
            Debit = asset.PurchasePrice,
            Credit = 0
        });

        // Post the journal
        await _journalPostingService.PostJournalAsync(journal.Id, Guid.NewGuid()); // TODO: Get actual user ID

        await _context.SaveChangesAsync();
        return asset;
    }

    private async Task<Account> GetAssetAccountAsync(Guid assetCategoryId, Guid companyId)
    {
        // Get the default asset account for this category
        var category = await _context.AssetCategories
            .FirstOrDefaultAsync(ac => ac.Id == assetCategoryId)
            ?? throw new ArgumentException("Asset category not found");

        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == category.AssetAccountId && a.CompanyId == companyId)
            ?? throw new InvalidOperationException($"Asset account not found for category {category.Name}");
    }

    private async Task<Account> GetAccumulatedDepreciationAccountAsync(Guid assetCategoryId, Guid companyId)
    {
        // Get the accumulated depreciation account for this category
        var category = await _context.AssetCategories
            .FirstOrDefaultAsync(ac => ac.Id == assetCategoryId)
            ?? throw new ArgumentException("Asset category not found");

        // Look for accumulated depreciation account (would need to be stored in AssetCategory)
        var accumulatedDepAccount = await _context.Accounts
            .Where(a => a.CompanyId == companyId)
            .Where(a => a.AccountType == AccountType.Asset && a.Name.ToLower().Contains("accumulated depreciation"))
            .FirstOrDefaultAsync();

        return accumulatedDepAccount ?? throw new InvalidOperationException("Accumulated depreciation account not found");
    }

    private async Task<Account> GetDisposalAccountAsync(Guid companyId)
    {
        return await _context.Accounts
            .Where(a => a.CompanyId == companyId)
            .Where(a => (a.AccountType == AccountType.Asset) && 
                        (a.Name.ToLower().Contains("cash") || a.Name.ToLower().Contains("bank")))
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Disposal account (cash/bank) not found");
    }

    private async Task<Account> GetGainLossAccountAsync(Guid companyId, bool isGain)
    {
        var accountType = isGain ? "gain" : "loss";
        return await _context.Accounts
            .Where(a => a.CompanyId == companyId)
            .Where(a => (a.AccountType == AccountType.Revenue && isGain) || 
                        (a.AccountType == AccountType.Expense && !isGain))
            .Where(a => a.Name.ToLower().Contains(accountType))
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"{(isGain ? "Gain" : "Loss")} on disposal account not found");
    }
}