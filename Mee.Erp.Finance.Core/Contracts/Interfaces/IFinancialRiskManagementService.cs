using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for financial risk management
/// </summary>
public interface IFinancialRiskManagementService
{
    /// <summary>
    /// Measures and analyzes interest rate risk
    /// </summary>
    Task<InterestRateRiskResult> MeasureInterestRateRiskAsync(InterestRateRiskRequest request);

    /// <summary>
    /// Assesses and monitors credit risk for customers and suppliers
    /// </summary>
    Task<CreditRiskResult> AssessCreditRiskAsync(CreditRiskRequest request);

    /// <summary>
    /// Manages counterparty risk
    /// </summary>
    Task<CounterpartyRiskResult> ManageCounterpartyRiskAsync(CounterpartyRiskRequest request);

    /// <summary>
    /// Performs market risk analytics and VaR calculations
    /// </summary>
    Task<MarketRiskResult> PerformMarketRiskAnalyticsAsync(MarketRiskRequest request);

    /// <summary>
    /// Handles derivatives accounting and reporting
    /// </summary>
    Task<DerivativesResult> HandleDerivativesAccountingAsync(DerivativesRequest request);

    /// <summary>
    /// Manages credit loss provisioning (CECL, IFRS 9)
    /// </summary>
    Task<CreditLossProvisionResult> ManageCreditLossProvisioningAsync(CreditLossProvisionRequest request);

    /// <summary>
    /// Generates concentration risk reports
    /// </summary>
    Task<ConcentrationRiskReport> GenerateConcentrationRiskReportAsync(ConcentrationRiskRequest request);

    /// <summary>
    /// Manages liquidity risk
    /// </summary>
    Task<LiquidityRiskResult> ManageLiquidityRiskAsync(LiquidityRiskRequest request);
}

public class InterestRateRiskRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<Guid> PortfolioAssetIds { get; set; } = new List<Guid>();
    public decimal InterestRateChangeScenario { get; set; } // Basis points change
    public string RiskMetric { get; set; } // "DV01", "Duration", "Convexity"
}

public class InterestRateRiskResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal PortfolioValueAfterShock { get; set; }
    public decimal ValueAtRisk { get; set; }
    public decimal Duration { get; set; }
    public decimal Convexity { get; set; }
    public decimal DV01 { get; set; } // Dollar value of a basis point
    public List<InterestRateSensitivity> Sensitivities { get; set; } = new List<InterestRateSensitivity>();
}

public class InterestRateSensitivity
{
    public Guid AssetId { get; set; }
    public string AssetName { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ValueAfterShock { get; set; }
    public decimal Sensitivity { get; set; }
}

public class CreditRiskRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> CustomerIds { get; set; } = new List<Guid>();
    public List<Guid> SupplierIds { get; set; } = new List<Guid>();
    public DateTime AsOfDate { get; set; }
    public string RiskModel { get; set; } // "ProbabilityOfDefault", "ExpectedLoss", "CreditMigration"
}

public class CreditRiskResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<CreditRiskEntity> CustomerRisks { get; set; } = new List<CreditRiskEntity>();
    public List<CreditRiskEntity> SupplierRisks { get; set; } = new List<CreditRiskEntity>();
    public decimal TotalExpectedCreditLoss { get; set; }
    public decimal PortfolioCreditVaR { get; set; }
}

public class CreditRiskEntity
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; }
    public string EntityType { get; set; } // "Customer", "Supplier"
    public decimal ExposureAtDefault { get; set; }
    public decimal ProbabilityOfDefault { get; set; }
    public decimal LossGivenDefault { get; set; }
    public decimal ExpectedLoss { get; set; }
    public string CreditRating { get; set; }
    public string RiskLevel { get; set; } // "Low", "Medium", "High", "Critical"
    public DateTime LastReviewDate { get; set; }
}

public class CounterpartyRiskRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> CounterpartyIds { get; set; } = new List<Guid>();
    public DateTime AsOfDate { get; set; }
    public decimal ThresholdAmount { get; set; }
}

public class CounterpartyRiskResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<CounterpartyRiskProfile> CounterpartyProfiles { get; set; } = new List<CounterpartyRiskProfile>();
    public decimal TotalCounterpartyExposure { get; set; }
    public decimal TotalPotentialFutureExposure { get; set; }
    public List<CounterpartyRiskAlert> Alerts { get; set; } = new List<CounterpartyRiskAlert>();
}

public class CounterpartyRiskProfile
{
    public Guid CounterpartyId { get; set; }
    public string CounterpartyName { get; set; }
    public decimal CurrentExposure { get; set; }
    public decimal PotentialFutureExposure { get; set; }
    public decimal CreditValuationAdjustment { get; set; }
    public string CreditRating { get; set; }
    public string RiskLevel { get; set; }
    public DateTime LastReviewDate { get; set; }
}

public class CounterpartyRiskAlert
{
    public Guid CounterpartyId { get; set; }
    public string AlertType { get; set; } // "ExposureThreshold", "RatingDowngrade", "PaymentDelay"
    public string Description { get; set; }
    public DateTime AlertDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
}

public class MarketRiskRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<Guid> PortfolioAssetIds { get; set; } = new List<Guid>();
    public string VaRCalculationMethod { get; set; } // "Historical", "Parametric", "MonteCarlo"
    public decimal ConfidenceLevel { get; set; } // 0.95, 0.99, etc.
    public int HoldingPeriodDays { get; set; } // 1, 10, etc.
}

public class MarketRiskResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal ValueAtRisk { get; set; }
    public decimal ExpectedShortfall { get; set; }
    public decimal Volatility { get; set; }
    public decimal Beta { get; set; }
    public List<MarketRiskFactor> RiskFactors { get; set; } = new List<MarketRiskFactor>();
    public List<StressTestResult> StressTestResults { get; set; } = new List<StressTestResult>();
}

public class MarketRiskFactor
{
    public string FactorName { get; set; }
    public string FactorType { get; set; } // "InterestRate", "Equity", "Commodity", "FX"
    public decimal Sensitivity { get; set; }
    public decimal ContributionToVaR { get; set; }
}

public class StressTestResult
{
    public string ScenarioName { get; set; }
    public decimal PortfolioValueBefore { get; set; }
    public decimal PortfolioValueAfter { get; set; }
    public decimal LossAmount { get; set; }
    public decimal LossPercentage { get; set; }
}

public class DerivativesRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> DerivativeInstrumentIds { get; set; } = new List<Guid>();
    public DateTime AsOfDate { get; set; }
    public string AccountingStandard { get; set; } // "IFRS", "USGAAP"
    public string HedgeType { get; set; } // "FairValue", "CashFlow", "NetInvestment"
}

public class DerivativesResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<DerivativeInstrument> Instruments { get; set; } = new List<DerivativeInstrument>();
    public decimal TotalFairValue { get; set; }
    public decimal TotalEffectivePortion { get; set; }
    public decimal TotalIneffectivePortion { get; set; }
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
}

public class DerivativeInstrument
{
    public Guid InstrumentId { get; set; }
    public string InstrumentType { get; set; } // "Forward", "Option", "Swap", "Future"
    public string UnderlyingAsset { get; set; }
    public decimal NotionalAmount { get; set; }
    public decimal FairValue { get; set; }
    public decimal EffectivePortion { get; set; }
    public decimal IneffectivePortion { get; set; }
    public string HedgeAccountingStatus { get; set; } // "Qualifying", "Ineffective", "De-designated"
}

public class CreditLossProvisionRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public string AccountingStandard { get; set; } // "CECL", "IFRS9"
    public List<Guid> FinancialAssetIds { get; set; } = new List<Guid>();
}

public class CreditLossProvisionResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<CreditLossProvision> Provisions { get; set; } = new List<CreditLossProvision>();
    public decimal TotalExpectedCreditLoss { get; set; }
    public decimal LifetimeExpectedCreditLoss { get; set; }
    public decimal TwelveMonthExpectedCreditLoss { get; set; }
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
}

public class CreditLossProvision
{
    public Guid FinancialAssetId { get; set; }
    public string AssetType { get; set; } // "Receivable", "Loan", "Security"
    public decimal OutstandingAmount { get; set; }
    public decimal ProbabilityOfDefault { get; set; }
    public decimal LossGivenDefault { get; set; }
    public decimal ExposureAtDefault { get; set; }
    public decimal ExpectedCreditLoss { get; set; }
    public decimal Stage { get; set; } // 1, 2, or 3 for IFRS 9
    public DateTime LastSignificantIncreaseDate { get; set; }
}

public class ConcentrationRiskRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public string RiskType { get; set; } // "Geographic", "Industry", "Customer", "Supplier", "Product"
}

public class ConcentrationRiskReport
{
    public Guid RequestId { get; set; }
    public DateTime ReportDate { get; set; }
    public string RiskType { get; set; }
    public List<ConcentrationRiskBucket> Buckets { get; set; } = new List<ConcentrationRiskBucket>();
    public decimal TotalExposure { get; set; }
    public decimal LargestExposure { get; set; }
    public decimal TopFiveExposure { get; set; }
    public decimal ConcentrationRatio { get; set; }
    public List<ConcentrationRiskAlert> Alerts { get; set; } = new List<ConcentrationRiskAlert>();
}

public class ConcentrationRiskBucket
{
    public string BucketName { get; set; }
    public decimal ExposureAmount { get; set; }
    public decimal ExposurePercentage { get; set; }
    public string RiskLevel { get; set; } // "Low", "Medium", "High", "Critical"
}

public class ConcentrationRiskAlert
{
    public string AlertType { get; set; } // "SingleCounterparty", "Industry", "Geographic"
    public string Description { get; set; }
    public decimal ThresholdExceeded { get; set; }
    public decimal ActualPercentage { get; set; }
    public DateTime AlertDate { get; set; }
}

public class LiquidityRiskRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public int StressPeriodDays { get; set; } // 30, 90, 180 days
}

public class LiquidityRiskResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public decimal CurrentCashBalance { get; set; }
    public decimal AvailableCreditLines { get; set; }
    public decimal CommittedCashOutflows { get; set; }
    public decimal CommittedCashInflows { get; set; }
    public decimal NetCashFlow30Days { get; set; }
    public decimal NetCashFlow90Days { get; set; }
    public decimal NetCashFlow180Days { get; set; }
    public decimal LiquidityCoverageRatio { get; set; }
    public decimal NetStableFundingRatio { get; set; }
    public List<LiquidityRiskAlert> Alerts { get; set; } = new List<LiquidityRiskAlert>();
}

public class LiquidityRiskAlert
{
    public string AlertType { get; set; } // "LowLiquidity", "NegativeCashFlow", "CreditLineUtilization"
    public string Description { get; set; }
    public DateTime AlertDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
}
