using Mee.Erp.Finance.Core.Contracts.Dtos;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IFinancialReportService
{
    Task<TrialBalanceReportDto> GetTrialBalanceAsync(TrialBalanceRequest request);
}