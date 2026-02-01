using Mee.Erp.Finance.Core.Contracts.Dtos;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IFinancialReportingService
{
    Task<TrialBalanceReportDto> GetTrialBalanceAsync(TrialBalanceRequest request);
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(BalanceSheetRequest request);
    Task<IncomeStatementReportDto> GetIncomeStatementAsync(IncomeStatementRequest request);
}