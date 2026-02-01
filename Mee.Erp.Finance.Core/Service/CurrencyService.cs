using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class CurrencyService : ICurrencyService
{
    private readonly FinanceDbContext _context;

    public CurrencyService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<ExchangeRate> AddExchangeRateAsync(ExchangeRate exchangeRate)
    {
        // Check if an active rate already exists for the same period
        var existingRate = await _context.ExchangeRates
            .Where(r => r.FromCurrencyCode == exchangeRate.FromCurrencyCode)
            .Where(r => r.ToCurrencyCode == exchangeRate.ToCurrencyCode)
            .Where(r => r.CompanyId == exchangeRate.CompanyId)
            .Where(r => r.IsActive)
            .Where(r => r.EffectiveDate <= exchangeRate.EffectiveDate)
            .Where(r => r.ExpiryDate == null || r.ExpiryDate >= exchangeRate.EffectiveDate)
            .FirstOrDefaultAsync();

        if (existingRate != null)
        {
            // Deactivate the old rate
            existingRate.ExpiryDate = exchangeRate.EffectiveDate.AddDays(-1);
            existingRate.IsActive = false;
        }

        exchangeRate.IsActive = true;
        _context.ExchangeRates.Add(exchangeRate);
        await _context.SaveChangesAsync();

        return exchangeRate;
    }

    public async Task<ExchangeRate?> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime date, Guid companyId)
    {
        // First try direct rate
        var directRate = await _context.ExchangeRates
            .Where(r => r.FromCurrencyCode == fromCurrency)
            .Where(r => r.ToCurrencyCode == toCurrency)
            .Where(r => r.CompanyId == companyId)
            .Where(r => r.IsActive)
            .Where(r => r.EffectiveDate <= date)
            .Where(r => r.ExpiryDate == null || r.ExpiryDate >= date)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        if (directRate != null)
        {
            return directRate;
        }

        // Try reverse rate if direct rate not found
        var reverseRate = await _context.ExchangeRates
            .Where(r => r.FromCurrencyCode == toCurrency)
            .Where(r => r.ToCurrencyCode == fromCurrency)
            .Where(r => r.CompanyId == companyId)
            .Where(r => r.IsActive)
            .Where(r => r.EffectiveDate <= date)
            .Where(r => r.ExpiryDate == null || r.ExpiryDate >= date)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        if (reverseRate != null)
        {
            return new ExchangeRate
            {
                Id = reverseRate.Id,
                FromCurrencyCode = fromCurrency,
                ToCurrencyCode = toCurrency,
                Rate = reverseRate.Rate > 0 ? 1 / reverseRate.Rate : 0,
                EffectiveDate = reverseRate.EffectiveDate,
                ExpiryDate = reverseRate.ExpiryDate,
                CompanyId = reverseRate.CompanyId,
                IsActive = reverseRate.IsActive,
                Source = reverseRate.Source,
                SourceReference = reverseRate.SourceReference
            };
        }

        return null;
    }

    public async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency, DateTime date, Guid companyId)
    {
        if (fromCurrency == toCurrency)
        {
            return amount;
        }

        var exchangeRate = await GetExchangeRateAsync(fromCurrency, toCurrency, date, companyId);
        if (exchangeRate == null)
        {
            throw new InvalidOperationException($"No exchange rate found from {fromCurrency} to {toCurrency} for {date:yyyy-MM-dd}");
        }

        return Math.Round(amount * exchangeRate.Rate, 2);
    }

    public async Task<List<ExchangeRate>> GetExchangeRatesAsync(Guid companyId, string? fromCurrency = null, string? toCurrency = null)
    {
        var query = _context.ExchangeRates
            .Where(r => r.CompanyId == companyId)
            .Where(r => r.IsActive);

        if (!string.IsNullOrEmpty(fromCurrency))
        {
            query = query.Where(r => r.FromCurrencyCode == fromCurrency);
        }

        if (!string.IsNullOrEmpty(toCurrency))
        {
            query = query.Where(r => r.ToCurrencyCode == toCurrency);
        }

        return await query
            .OrderBy(r => r.FromCurrencyCode)
            .ThenBy(r => r.ToCurrencyCode)
            .ThenByDescending(r => r.EffectiveDate)
            .ToListAsync();
    }

    public async Task<ExchangeRate> UpdateExchangeRateAsync(Guid rateId, decimal newRate)
    {
        var rate = await _context.ExchangeRates
            .FirstOrDefaultAsync(r => r.Id == rateId)
            ?? throw new ArgumentException("Exchange rate not found");

        rate.Rate = newRate;
        rate.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return rate;
    }

    public async Task DeleteExchangeRateAsync(Guid rateId)
    {
        var rate = await _context.ExchangeRates
            .FirstOrDefaultAsync(r => r.Id == rateId)
            ?? throw new ArgumentException("Exchange rate not found");

        // Check if rate is being used in any transactions
        var isUsed = await _context.JournalEntries
            .AnyAsync(je => je.Description.Contains($"Rate: {rate.Rate}"));

        if (isUsed)
        {
            rate.IsActive = false;
            rate.ExpiryDate = DateTime.UtcNow;
        }
        else
        {
            _context.ExchangeRates.Remove(rate);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<decimal> GetRealizedGainLossAsync(Guid companyId, string currency, DateTime startDate, DateTime endDate)
    {
        // This calculates realized gains/losses from currency conversions that have been settled
        
        // Find all transactions that involved the foreign currency and were settled
        var foreignCurrencyTransactions = await _context.JournalEntries
            .Join(_context.Journals, je => je.JournalId, j => j.Id, (je, j) => new { je, j })
            .Where(x => x.j.JournalDate >= startDate && x.j.JournalDate <= endDate)
            .Where(x => x.j.CompanyId == companyId)
            .Where(x => x.je.Description.Contains(currency)) // Simplified - in real app, would track currency properly
            .ToListAsync();

        // This is a simplified calculation
        // In a real implementation, you'd need to track the original and settlement rates
        decimal realizedGainLoss = 0;
        
        foreach (var transaction in foreignCurrencyTransactions)
        {
            // Simplified logic - in reality, you'd compare the rate at transaction time vs settlement time
            // For now, we'll assume a basic calculation
            var netAmount = transaction.je.Debit - transaction.je.Credit;
            if (netAmount != 0)
            {
                // This would need to be calculated based on actual gain/loss accounts
                // For demo purposes, returning 0
            }
        }

        return realizedGainLoss;
    }
}