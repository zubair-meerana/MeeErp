using Mee.Erp.Finance.Core.Contracts.Dtos;
using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Dtos;

public class IncomeStatementRequest
{
    public Guid CompanyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid>? BusinessUnitIds { get; set; }
    public bool CompareToPreviousPeriod { get; set; } = false;
}

public class IncomeStatementLineDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public AccountType AccountType { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal? PreviousPeriodAmount { get; set; }
    public decimal? Variance { get; set; }
    public decimal? VariancePercentage { get; set; }
}

public class IncomeStatementReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? PreviousStartDate { get; set; }
    public DateTime? PreviousEndDate { get; set; }
    
    public List<IncomeStatementLineDto> Revenue { get; set; } = new();
    public List<IncomeStatementLineDto> CostOfGoodsSold { get; set; } = new();
    public List<IncomeStatementLineDto> OperatingExpenses { get; set; } = new();
    public List<IncomeStatementLineDto> OtherIncome { get; set; } = new();
    public List<IncomeStatementLineDto> OtherExpenses { get; set; } = new();
    
    public decimal TotalRevenue { get; set; }
    public decimal TotalCostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalOperatingExpenses { get; set; }
    public decimal OperatingIncome { get; set; }
    public decimal TotalOtherIncome { get; set; }
    public decimal TotalOtherExpenses { get; set; }
    public decimal NetIncome { get; set; }
    
    public decimal? PreviousPeriodNetIncome { get; set; }
    public decimal? NetIncomeVariance { get; set; }
    public decimal? NetIncomeVariancePercentage { get; set; }
}