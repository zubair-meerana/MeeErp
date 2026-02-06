using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for multi-currency and foreign exchange management
/// </summary>
public interface IMultiCurrencyFxService
{
    /// <summary>
    /// Gets exchange rate with historical rates support
    /// </summary>
    Task<ExchangeRate> GetExchangeRateWithHistoryAsync(string fromCurrency, string toCurrency, DateTime asOfDate);

    /// <summary>
    /// Converts amount between currencies using historical rates
    /// </summary>
    Task<decimal> ConvertCurrencyWithHistoryAsync(decimal amount, string fromCurrency, string toCurrency, DateTime transactionDate);

    /// <summary>
    /// Calculates foreign exchange gains/losses for transactions
    /// </summary>
    Task<FxGainLossResult> CalculateFxGainLossAsync(Guid transactionId, string fromCurrency, string toCurrency, decimal amount, DateTime transactionDate, DateTime settlementDate);

    /// <summary>
    /// Processes currency hedging instruments and accounting
    /// </summary>
    Task<HedgeInstrumentResult> ProcessHedgeInstrumentAsync(HedgeInstrument hedgeInstrument, Guid companyId);

    /// <summary>
    /// Manages multi-book accounting for different reporting requirements
    /// </summary>
    Task<bool> ProcessMultiBookAccountingAsync(Journal journal, MultiBookOptions options);

    /// <summary>
    /// Generates FX exposure reports and risk management data
    /// </summary>
    Task<FxExposureReport> GenerateFxExposureReportAsync(Guid companyId, DateTime asOfDate, string baseCurrency);
}

public class FxGainLossResult
{
    public decimal OriginalAmount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public decimal GainLossAmount { get; set; }
    public string GainLossType { get; set; } // "Gain" or "Loss"
    public decimal ExchangeRateAtTransaction { get; set; }
    public decimal ExchangeRateAtSettlement { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime SettlementDate { get; set; }
}

public class HedgeInstrument
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string InstrumentType { get; set; } // "Forward", "Option", "Swap"
    public string CurrencyPair { get; set; }
    public decimal NotionalAmount { get; set; }
    public decimal StrikeRate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public decimal Premium { get; set; }
    public string Purpose { get; set; } // "Fair Value Hedge", "Cash Flow Hedge", "Net Investment Hedge"
}

public class HedgeInstrumentResult
{
    public Guid InstrumentId { get; set; }
    public bool IsProcessed { get; set; }
    public string ProcessingStatus { get; set; }
    public decimal FairValue { get; set; }
    public decimal EffectivePortion { get; set; }
    public decimal IneffectivePortion { get; set; }
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
}

public class MultiBookOptions
{
    public string BookType { get; set; } // "Statutory", "Management", "Tax", "IFRS", "GAAP"
    public string ReportingCurrency { get; set; }
    public bool IncludeInConsolidation { get; set; }
    public List<string> AdditionalDimensions { get; set; } = new List<string>();
}

public class FxExposureReport
{
    public Guid CompanyId { get; set; }
    public DateTime ReportDate { get; set; }
    public string BaseCurrency { get; set; }
    public List<FxExposureLine> ExposureLines { get; set; } = new List<FxExposureLine>();
    public decimal TotalExposure { get; set; }
    public decimal NetExposure { get; set; }
}

public class FxExposureLine
{
    public string Currency { get; set; }
    public decimal ExposureAmount { get; set; }
    public decimal NetExposure { get; set; }
    public decimal WeightedAvgRate { get; set; }
    public string RiskLevel { get; set; } // "Low", "Medium", "High"
}
