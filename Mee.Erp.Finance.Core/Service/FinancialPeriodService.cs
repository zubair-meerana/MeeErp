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

public class FinancialPeriodService : IFinancialPeriodService
{
    private readonly FinanceDbContext _context;

    public FinancialPeriodService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialPeriod> CreatePeriodAsync(FinancialPeriod period)
    {
        // Validate period doesn't overlap with existing periods
        var existingPeriods = await _context.FinancialPeriods
            .Where(p => p.CompanyId == period.CompanyId)
            .Where(p => p.StartDate <= period.EndDate && p.EndDate >= period.StartDate)
            .ToListAsync();

        if (existingPeriods.Any())
        {
            throw new InvalidOperationException("Period dates overlap with existing periods");
        }

        period.Status = FinancialPeriodStatus.Open;
        _context.FinancialPeriods.Add(period);
        await _context.SaveChangesAsync();

        // Update linked periods
        await UpdatePeriodLinksAsync(period);

        return period;
    }

    public async Task<FinancialPeriod> ClosePeriodAsync(Guid periodId, Guid userId, string? notes = null)
    {
        var period = await _context.FinancialPeriods
            .FirstOrDefaultAsync(p => p.Id == periodId)
            ?? throw new ArgumentException("Period not found");

        if (period.Status == FinancialPeriodStatus.Closed || period.Status == FinancialPeriodStatus.PermanentlyClosed)
        {
            throw new InvalidOperationException("Period is already closed");
        }

        // Check if there are any unclosed periods before this one
        var unclosedPriorPeriods = await _context.FinancialPeriods
            .Where(p => p.CompanyId == period.CompanyId)
            .Where(p => p.EndDate < period.StartDate)
            .Where(p => p.Status != FinancialPeriodStatus.Closed && p.Status != FinancialPeriodStatus.PermanentlyClosed)
            .ToListAsync();

        if (unclosedPriorPeriods.Any())
        {
            throw new InvalidOperationException("Cannot close period. Previous periods must be closed first.");
        }

        // Create closing journal entries for temporary accounts (Revenue and Expenses)
        await CreateClosingEntriesAsync(period);

        period.Status = FinancialPeriodStatus.Closed;
        period.ClosedDate = DateTime.UtcNow;
        period.ClosedByUserId = userId;
        period.ClosingNotes = notes;

        await _context.SaveChangesAsync();
        return period;
    }

    public async Task<FinancialPeriod> ReopenPeriodAsync(Guid periodId, Guid userId)
    {
        var period = await _context.FinancialPeriods
            .FirstOrDefaultAsync(p => p.Id == periodId)
            ?? throw new ArgumentException("Period not found");

        if (period.Status == FinancialPeriodStatus.PermanentlyClosed)
        {
            throw new InvalidOperationException("Cannot reopen a permanently closed period");
        }

        // Check if subsequent periods are closed
        var subsequentClosedPeriods = await _context.FinancialPeriods
            .Where(p => p.CompanyId == period.CompanyId)
            .Where(p => p.StartDate > period.EndDate)
            .Where(p => p.Status == FinancialPeriodStatus.PermanentlyClosed)
            .AnyAsync();

        if (subsequentClosedPeriods)
        {
            throw new InvalidOperationException("Cannot reopen period. Subsequent periods are permanently closed.");
        }

        // Reverse closing entries if they exist
        await ReverseClosingEntriesAsync(period);

        period.Status = FinancialPeriodStatus.Open;
        period.ClosedDate = null;
        period.ClosedByUserId = null;
        period.ClosingNotes = null;

        await _context.SaveChangesAsync();
        return period;
    }

    public async Task<List<FinancialPeriod>> GetPeriodsAsync(Guid companyId)
    {
        return await _context.FinancialPeriods
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.StartDate)
            .ToListAsync();
    }

    public async Task<FinancialPeriod?> GetCurrentPeriodAsync(Guid companyId)
    {
        var now = DateTime.UtcNow;
        return await _context.FinancialPeriods
            .Where(p => p.CompanyId == companyId)
            .Where(p => p.StartDate <= now && p.EndDate >= now)
            .Where(p => p.Status == FinancialPeriodStatus.Open)
            .FirstOrDefaultAsync();
    }

    public async Task<FinancialPeriod?> GetPeriodByDateAsync(Guid companyId, DateTime date)
    {
        return await _context.FinancialPeriods
            .Where(p => p.CompanyId == companyId)
            .Where(p => p.StartDate <= date && p.EndDate >= date)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CanPostToPeriodAsync(Guid periodId)
    {
        var period = await _context.FinancialPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == periodId);

        return period?.Status == FinancialPeriodStatus.Open;
    }

    public async Task<List<FinancialPeriod>> GetOpenPeriodsAsync(Guid companyId)
    {
        return await _context.FinancialPeriods
            .Where(p => p.CompanyId == companyId)
            .Where(p => p.Status == FinancialPeriodStatus.Open)
            .OrderBy(p => p.StartDate)
            .ToListAsync();
    }

    public async Task<FinancialPeriod> CreateFiscalYearAsync(Guid companyId, int fiscalYear, DateTime startDate, DateTime endDate, int numberOfPeriods = 12)
    {
        // Check if fiscal year already exists
        var existingYear = await _context.FinancialPeriods
            .AnyAsync(p => p.CompanyId == companyId && p.FiscalYear == fiscalYear && p.IsFiscalYear);

        if (existingYear)
        {
            throw new InvalidOperationException($"Fiscal year {fiscalYear} already exists");
        }

        var totalDays = (endDate - startDate).Days;
        var daysPerPeriod = totalDays / numberOfPeriods;

        var fiscalYearPeriod = new FinancialPeriod
        {
            Id = Guid.NewGuid(),
            Name = $"FY {fiscalYear}",
            Code = $"FY{fiscalYear}",
            StartDate = startDate,
            EndDate = endDate,
            CompanyId = companyId,
            FiscalYear = fiscalYear,
            IsFiscalYear = true,
            Status = FinancialPeriodStatus.Open
        };

        _context.FinancialPeriods.Add(fiscalYearPeriod);

        // Create individual periods
        var periods = new List<FinancialPeriod>();
        for (int i = 0; i < numberOfPeriods; i++)
        {
            var periodStartDate = startDate.AddDays(daysPerPeriod * i);
            var periodEndDate = i == numberOfPeriods - 1 
                ? endDate 
                : startDate.AddDays(daysPerPeriod * (i + 1)) - TimeSpan.FromDays(1);

            var period = new FinancialPeriod
            {
                Id = Guid.NewGuid(),
                Name = $"Period {i + 1} - {fiscalYear}",
                Code = $"P{i + 1:D2}FY{fiscalYear}",
                StartDate = periodStartDate,
                EndDate = periodEndDate,
                CompanyId = companyId,
                FiscalYear = fiscalYear,
                PeriodNumber = i + 1,
                IsFiscalYear = false,
                Status = FinancialPeriodStatus.Open
            };

            periods.Add(period);
        }

        _context.FinancialPeriods.AddRange(periods);
        await _context.SaveChangesAsync();

        // Update period links
        foreach (var period in periods)
        {
            await UpdatePeriodLinksAsync(period);
        }

        return fiscalYearPeriod;
    }

    private async Task UpdatePeriodLinksAsync(FinancialPeriod period)
    {
        // Find previous period
        var previousPeriod = await _context.FinancialPeriods
            .Where(p => p.CompanyId == period.CompanyId)
            .Where(p => p.EndDate < period.StartDate)
            .OrderByDescending(p => p.EndDate)
            .FirstOrDefaultAsync();

        if (previousPeriod != null)
        {
            period.PreviousPeriodId = previousPeriod.Id;
            previousPeriod.NextPeriodId = period.Id;
        }

        // Find next period
        var nextPeriod = await _context.FinancialPeriods
            .Where(p => p.CompanyId == period.CompanyId)
            .Where(p => p.StartDate > period.EndDate)
            .OrderBy(p => p.StartDate)
            .FirstOrDefaultAsync();

        if (nextPeriod != null)
        {
            period.NextPeriodId = nextPeriod.Id;
            nextPeriod.PreviousPeriodId = period.Id;
        }

        await _context.SaveChangesAsync();
    }

    private async Task CreateClosingEntriesAsync(FinancialPeriod period)
    {
        // Get revenue and expense accounts
        var revenueAndExpenseAccounts = await _context.Accounts
            .Where(a => a.CompanyId == period.CompanyId)
            .Where(a => a.AccountType == AccountType.Revenue || a.AccountType == AccountType.Expense)
            .ToListAsync();

        if (!revenueAndExpenseAccounts.Any()) return;

        // Get balances for these accounts
        var accountBalances = await _context.LedgerEntries
            .Where(le => le.EntryDate >= period.StartDate && le.EntryDate <= period.EndDate)
            .Join(revenueAndExpenseAccounts, le => le.AccountId, a => a.Id, (le, a) => new { le, a })
            .GroupBy(x => x.a.Id)
            .Select(g => new
            {
                AccountId = g.Key,
                AccountType = g.First().a.AccountType,
                Balance = g.Sum(x => x.le.Credit) - g.Sum(x => x.le.Debit)
            })
            .Where(x => x.Balance != 0)
            .ToListAsync();

        if (!accountBalances.Any()) return;

        // Get retained earnings account
        var retainedEarningsAccount = await _context.Accounts
            .Where(a => a.CompanyId == period.CompanyId)
            .Where(a => a.AccountType == AccountType.Equity && a.Name.ToLower().Contains("retained"))
            .FirstOrDefaultAsync();

        if (retainedEarningsAccount == null)
        {
            throw new InvalidOperationException("Retained Earnings account not found. Please create an equity account named 'Retained Earnings'.");
        }

        // Create closing journal
        var closingJournal = new Journal
        {
            Id = Guid.NewGuid(),
            Description = $"Closing entries for {period.Name}",
            JournalDate = period.EndDate,
            Status = JournalStatus.Draft,
            CompanyId = period.CompanyId
        };

        _context.Journals.Add(closingJournal);

        // Create closing entries
        foreach (var balance in accountBalances)
        {
            if (balance.AccountType == AccountType.Revenue)
            {
                // Revenue accounts have credit balances, so we debit to close them
                _context.JournalEntries.Add(new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    JournalId = closingJournal.Id,
                    AccountId = balance.AccountId,
                    Description = $"Close revenue account",
                    Debit = balance.Balance,
                    Credit = 0
                });

                _context.JournalEntries.Add(new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    JournalId = closingJournal.Id,
                    AccountId = retainedEarningsAccount.Id,
                    Description = $"Close revenue to retained earnings",
                    Debit = 0,
                    Credit = balance.Balance
                });
            }
            else if (balance.AccountType == AccountType.Expense)
            {
                // Expense accounts have debit balances, so we credit to close them
                _context.JournalEntries.Add(new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    JournalId = closingJournal.Id,
                    AccountId = balance.AccountId,
                    Description = $"Close expense account",
                    Debit = 0,
                    Credit = balance.Balance
                });

                _context.JournalEntries.Add(new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    JournalId = closingJournal.Id,
                    AccountId = retainedEarningsAccount.Id,
                    Description = $"Close expenses to retained earnings",
                    Debit = balance.Balance,
                    Credit = 0
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ReverseClosingEntriesAsync(FinancialPeriod period)
    {
        // Find and reverse the closing journal for this period
        var closingJournal = await _context.Journals
            .Where(j => j.CompanyId == period.CompanyId)
            .Where(j => j.Description.Contains("Closing entries") && j.Description.Contains(period.Name))
            .FirstOrDefaultAsync();

        if (closingJournal != null)
        {
            // Create reversing journal
            var reversingJournal = new Journal
            {
                Id = Guid.NewGuid(),
                Description = $"Reversing closing entries for {period.Name}",
                JournalDate = DateTime.UtcNow,
                Status = JournalStatus.Draft,
                CompanyId = period.CompanyId
            };

            _context.Journals.Add(reversingJournal);

            // Get original entries and create reversals
            var originalEntries = await _context.JournalEntries
                .Where(je => je.JournalId == closingJournal.Id)
                .ToListAsync();

            foreach (var entry in originalEntries)
            {
                _context.JournalEntries.Add(new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    JournalId = reversingJournal.Id,
                    AccountId = entry.AccountId,
                    Description = $"Reverse: {entry.Description}",
                    Debit = entry.Credit,
                    Credit = entry.Debit
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}