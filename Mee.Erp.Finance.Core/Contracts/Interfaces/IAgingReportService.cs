using Mee.Erp.Finance.Core.Contracts.Dtos;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IAgingReportService
{
    Task<AgingReportDto> GetAccountsReceivableAgingAsync(AgingReportRequest request);
    Task<AgingReportDto> GetAccountsPayableAgingAsync(AgingReportRequest request);
}