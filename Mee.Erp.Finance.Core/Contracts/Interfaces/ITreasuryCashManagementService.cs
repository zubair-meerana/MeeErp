using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for treasury and cash management
/// </summary>
public interface ITreasuryCashManagementService
{
    /// <summary>
    /// Creates advanced cash flow forecasting with predictive analytics
    /// </summary>
    Task<CashFlowForecastResult> CreateCashFlowForecastAsync(CashFlowForecastRequest request);

    /// <summary>
    /// Manages investment portfolio and accounting
    /// </summary>
    Task<InvestmentPortfolioResult> ManageInvestmentPortfolioAsync(InvestmentPortfolioRequest request);

    /// <summary>
    /// Manages banking relationships
    /// </summary>
    Task<BankingRelationshipResult> ManageBankingRelationshipsAsync(BankingRelationshipRequest request);

    /// <summary>
    /// Manages credit facilities and tracking
    /// </summary>
    Task<CreditFacilityResult> ManageCreditFacilitiesAsync(CreditFacilityRequest request);

    /// <summary>
    /// Optimizes liquidity strategies
    /// </summary>
    Task<LiquidityOptimizationResult> OptimizeLiquidityAsync(LiquidityOptimizationRequest request);

    /// <summary>
    /// Manages cash pooling and netting arrangements
    /// </summary>
    Task<CashPoolingResult> ManageCashPoolingAsync(CashPoolingRequest request);

    /// <summary>
    /// Manages sweep account arrangements
    /// </summary>
    Task<SweepAccountResult> ManageSweepAccountsAsync(SweepAccountRequest request);
}

public class CashFlowForecastRequest
{
    public Guid CompanyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ForecastHorizon { get; set; } // "Daily", "Weekly", "Monthly", "Quarterly"
    public string ForecastMethod { get; set; } // "Historical", "Predictive", "MachineLearning"
    public List<CashFlowDriver> Drivers { get; set; } = new List<CashFlowDriver>();
    public bool IncludePredictiveAnalytics { get; set; }
    public string ConfidenceLevel { get; set; } // "80%", "90%", "95%"
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class CashFlowDriver
{
    public string Name { get; set; }
    public string Type { get; set; } // "Revenue", "Expense", "Investment", "Financing"
    public decimal Weight { get; set; }
    public string Formula { get; set; }
    public List<HistoricalCashFlowData> HistoricalData { get; set; } = new List<HistoricalCashFlowData>();
}

public class HistoricalCashFlowData
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } // "Operating", "Investing", "Financing"
    public string DataSource { get; set; }
}

public class CashFlowForecastResult
{
    public Guid RequestId { get; set; }
    public DateTime GeneratedDate { get; set; }
    public List<CashFlowPeriod> ForecastPeriods { get; set; } = new List<CashFlowPeriod>();
    public decimal NetCashFlow { get; set; }
    public decimal EndingCashBalance { get; set; }
    public decimal BeginningCashBalance { get; set; }
    public string ModelUsed { get; set; }
    public decimal AccuracyRating { get; set; }
    public List<ConfidenceInterval> ConfidenceIntervals { get; set; } = new List<ConfidenceInterval>();
}

public class CashFlowPeriod
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal OperatingCashFlow { get; set; }
    public decimal InvestingCashFlow { get; set; }
    public decimal FinancingCashFlow { get; set; }
    public decimal NetCashFlow { get; set; }
    public decimal BeginningBalance { get; set; }
    public decimal EndingBalance { get; set; }
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public string VarianceExplanation { get; set; }
}

public class InvestmentPortfolioRequest
{
    public Guid CompanyId { get; set; }
    public List<InvestmentHolding> Holdings { get; set; } = new List<InvestmentHolding>();
    public DateTime AsOfDate { get; set; }
    public string PortfolioStrategy { get; set; } // "Conservative", "Balanced", "Aggressive"
    public decimal RiskTolerance { get; set; } // 0-1 scale
    public decimal TargetReturn { get; set; } // Expected return percentage
}

public class InvestmentHolding
{
    public Guid InvestmentId { get; set; }
    public string InvestmentType { get; set; } // "Bond", "Stock", "MutualFund", "CD", "MoneyMarket", "Derivative"
    public string Issuer { get; set; }
    public string Ticker { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal CurrentYield { get; set; }
    public DateTime? MaturityDate { get; set; }
    public decimal CouponRate { get; set; }
    public string RiskRating { get; set; } // "AAA", "AA", "A", "BBB", "BB", "B", "CCC", "Below"
}

public class InvestmentPortfolioResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public decimal TotalAnnualIncome { get; set; }
    public decimal PortfolioYield { get; set; }
    public decimal PortfolioRiskScore { get; set; }
    public List<InvestmentHolding> Holdings { get; set; } = new List<InvestmentHolding>();
    public List<PortfolioMetric> Metrics { get; set; } = new List<PortfolioMetric>();
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
}

public class PortfolioMetric
{
    public string MetricName { get; set; }
    public decimal Value { get; set; }
    public string Unit { get; set; }
    public DateTime AsOfDate { get; set; }
}

public class BankingRelationshipRequest
{
    public Guid CompanyId { get; set; }
    public List<BankingArrangement> Arrangements { get; set; } = new List<BankingArrangement>();
    public DateTime AsOfDate { get; set; }
    public string RelationshipManager { get; set; }
    public string PrimaryBank { get; set; }
}

public class BankingArrangement
{
    public Guid ArrangementId { get; set; }
    public string BankName { get; set; }
    public string AccountType { get; set; } // "Checking", "Savings", "Investment", "Credit", "LineOfCredit"
    public string AccountNumber { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal InterestRate { get; set; }
    public decimal Fees { get; set; }
    public DateTime LastStatementDate { get; set; }
    public string RelationshipManager { get; set; }
    public string ServiceLevel { get; set; } // "Premium", "Standard", "Basic"
    public List<BankingService> Services { get; set; } = new List<BankingService>();
}

public class BankingService
{
    public string ServiceName { get; set; }
    public string ServiceType { get; set; } // "WireTransfer", "ACH", "Lockbox", "CashManagement", "TradeFinance"
    public decimal Fee { get; set; }
    public DateTime LastUsed { get; set; }
}

public class BankingRelationshipResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<BankingArrangement> Arrangements { get; set; } = new List<BankingArrangement>();
    public decimal TotalBankBalances { get; set; }
    public decimal TotalAvailableCredit { get; set; }
    public decimal TotalAnnualFees { get; set; }
    public string PrimaryBank { get; set; }
    public string RelationshipManager { get; set; }
}

public class CreditFacilityRequest
{
    public Guid CompanyId { get; set; }
    public List<CreditFacility> Facilities { get; set; } = new List<CreditFacility>();
    public DateTime AsOfDate { get; set; }
    public string CreditRating { get; set; }
    public decimal TotalDebtCapacity { get; set; }
}

public class CreditFacility
{
    public Guid FacilityId { get; set; }
    public string FacilityType { get; set; } // "RevolvingCredit", "TermLoan", "Overdraft", "LetterOfCredit", "CommercialPaper"
    public string Lender { get; set; }
    public decimal FacilityAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal InterestRate { get; set; }
    public string InterestRateType { get; set; } // "Fixed", "Variable", "Hybrid"
    public DateTime MaturityDate { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public string Covenants { get; set; }
    public string Collateral { get; set; }
    public string CreditRating { get; set; }
    public List<CovenantCompliance> CovenantCompliance { get; set; } = new List<CovenantCompliance>();
}

public class CovenantCompliance
{
    public string CovenantName { get; set; }
    public string CovenantType { get; set; } // "Financial", "NonFinancial"
    public string Requirement { get; set; }
    public decimal ActualValue { get; set; }
    public decimal RequiredValue { get; set; }
    public bool IsCompliant { get; set; }
    public DateTime ComplianceDate { get; set; }
}

public class CreditFacilityResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<CreditFacility> Facilities { get; set; } = new List<CreditFacility>();
    public decimal TotalCreditFacilities { get; set; }
    public decimal TotalAvailableCredit { get; set; }
    public decimal TotalOutstandingCredit { get; set; }
    public string OverallComplianceStatus { get; set; }
    public List<ComplianceAlert> ComplianceAlerts { get; set; } = new List<ComplianceAlert>();
}

public class ComplianceAlert
{
    public Guid FacilityId { get; set; }
    public string CovenantName { get; set; }
    public string AlertType { get; set; } // "Warning", "Breach", "NearBreach"
    public string Description { get; set; }
    public DateTime AlertDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
}

public class LiquidityOptimizationRequest
{
    public Guid CompanyId { get; set; }
    public decimal MinimumCashBalance { get; set; }
    public decimal TargetCashBalance { get; set; }
    public decimal MaximumCashBalance { get; set; }
    public List<InvestmentOpportunity> InvestmentOpportunities { get; set; } = new List<InvestmentOpportunity>();
    public DateTime AsOfDate { get; set; }
    public string OptimizationStrategy { get; set; } // "MaximizeYield", "MinimizeRisk", "Balance"
}

public class InvestmentOpportunity
{
    public string OpportunityName { get; set; }
    public string Type { get; set; } // "CD", "MoneyMarket", "CommercialPaper", "Repo", "Treasuries"
    public decimal Amount { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime MaturityDate { get; set; }
    public string RiskLevel { get; set; } // "Low", "Medium", "High"
    public decimal MinimumInvestment { get; set; }
}

public class LiquidityOptimizationResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public decimal CurrentCashBalance { get; set; }
    public decimal RecommendedActions { get; set; }
    public List<LiquidityAction> Actions { get; set; } = new List<LiquidityAction>();
    public decimal ProjectedCashBalanceAfterActions { get; set; }
    public string OptimizationStrategy { get; set; }
}

public class LiquidityAction
{
    public string ActionType { get; set; } // "Invest", "Withdraw", "Borrow", "Repay"
    public string Target { get; set; } // Investment vehicle or credit facility
    public decimal Amount { get; set; }
    public string Reason { get; set; }
    public DateTime RecommendedDate { get; set; }
}

public class CashPoolingRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> PoolMemberAccountIds { get; set; } = new List<Guid>();
    public string PoolType { get; set; } // "Physical", "Notional", "Balance"
    public string PoolStructure { get; set; } // "ZeroBalance", "TargetBalance", "CashConcentration"
    public decimal TargetBalance { get; set; }
    DateTime AsOfDate { get; set; }
}

public class CashPoolingResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<CashPoolMember> Members { get; set; } = new List<CashPoolMember>();
    public decimal TotalPoolBalance { get; set; }
    public decimal PoolType { get; set; }
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
}

public class CashPoolMember
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal ContributionToPool { get; set; }
    public decimal DistributionFromPool { get; set; }
}

public class SweepAccountRequest
{
    public Guid CompanyId { get; set; }
    public Guid SweepAccountId { get; set; }
    public List<Guid> SourceAccountIds { get; set; } = new List<Guid>();
    public decimal MinimumBalance { get; set; }
    public decimal SweepThreshold { get; set; }
    public string SweepFrequency { get; set; } // "Daily", "Weekly", "Monthly"
    public DateTime AsOfDate { get; set; }
}

public class SweepAccountResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public Guid SweepAccountId { get; set; }
    public List<SweepTransaction> SweepTransactions { get; set; } = new List<SweepTransaction>();
    public decimal TotalSweptAmount { get; set; }
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
}

public class SweepTransaction
{
    public Guid SourceAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateTime SweepDate { get; set; }
    public string Status { get; set; } // "Completed", "Failed", "Pending"
}
