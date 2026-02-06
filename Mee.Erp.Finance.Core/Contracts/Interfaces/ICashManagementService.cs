using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for cash management and liquidity functions
/// </summary>
public interface ICashManagementService
{
    /// <summary>
    /// Generates cash flow forecasts
    /// </summary>
    Task<IEnumerable<CashFlowForecastLine>> GenerateCashFlowForecastAsync(Guid companyId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Manages liquidity positions
    /// </summary>
    Task<LiquidityPosition> GetLiquidityPositionAsync(Guid companyId, DateTime asOfDate);

    /// <summary>
    /// Manages cash pooling arrangements
    /// </summary>
    Task<bool> ProcessCashPoolingAsync(Guid companyId, IEnumerable<Guid> bankAccountIds);

    /// <summary>
    /// Tracks investment income
    /// </summary>
    Task<IEnumerable<InvestmentIncome>> GetInvestmentIncomeAsync(Guid companyId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Generates cash position reports
    /// </summary>
    Task<CashPositionReport> GenerateCashPositionReportAsync(Guid companyId, DateTime asOfDate);
}

public class CashFlowForecastLine
{
    public DateTime Date { get; set; }
    public decimal ExpectedInflow { get; set; }
    public decimal ExpectedOutflow { get; set; }
    public decimal NetCashFlow { get; set; }
    public decimal CumulativeBalance { get; set; }
    public string Description { get; set; }
}

public class LiquidityPosition
{
    public decimal TotalCashAndCashEquivalents { get; set; }
    public decimal AvailableCreditLines { get; set; }
    public decimal ShortTermInvestments { get; set; }
    public decimal CurrentLiabilities { get; set; }
    public decimal NetLiquidity { get; set; }
    public decimal LiquidityRatio { get; set; }
}

public class InvestmentIncome
{
    public Guid InvestmentId { get; set; }
    public string InvestmentDescription { get; set; }
    public decimal IncomeAmount { get; set; }
    public DateTime IncomeDate { get; set; }
    public string IncomeType { get; set; }
}

public class CashPositionReport
{
    public DateTime ReportDate { get; set; }
    public Dictionary<string, decimal> CashPositionsByAccount { get; set; }
    public decimal TotalCashPosition { get; set; }
    public decimal DailyCashFlowProjection { get; set; }
    public List<CashFlowForecastLine> CashFlowProjection { get; set; }
}
