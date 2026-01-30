using System.Collections.Generic;

namespace Mee.Erp.Finance.Core.Contracts.Dtos;

public class TrialBalanceReportDto
{
    public List<TrialBalanceLineDto> Lines { get; set; } = new();
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
}

public class TrialBalanceLineDto
{
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}