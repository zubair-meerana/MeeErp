using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface ICurrencyService
{
    Task<ExchangeRate> AddExchangeRateAsync(ExchangeRate exchangeRate);
    Task<ExchangeRate?> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime date, Guid companyId);
    Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency, DateTime date, Guid companyId);
    Task<List<ExchangeRate>> GetExchangeRatesAsync(Guid companyId, string? fromCurrency = null, string? toCurrency = null);
    Task<ExchangeRate> UpdateExchangeRateAsync(Guid rateId, decimal newRate);
    Task DeleteExchangeRateAsync(Guid rateId);
    Task<decimal> GetRealizedGainLossAsync(Guid companyId, string currency, DateTime startDate, DateTime endDate);
}