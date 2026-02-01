using Mee.Erp.Finance.Core.Contracts.Dtos;
using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Dtos;

public class BalanceSheetRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<Guid>? BusinessUnitIds { get; set; }
}

public class BalanceSheetLineDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public AccountType AccountType { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class BalanceSheetReportDto
{
    public DateTime AsOfDate { get; set; }
    public List<BalanceSheetLineDto> Assets { get; set; } = new();
    public List<BalanceSheetLineDto> Liabilities { get; set; } = new();
    public List<BalanceSheetLineDto> Equity { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal LiabilitiesAndEquity { get; set; }
}