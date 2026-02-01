using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IAssetDisposalService
{
    Task<FixedAsset> DisposeAssetAsync(Guid assetId, DateTime disposalDate, decimal disposalValue, string reason);
    Task<FixedAsset> TransferAssetAsync(Guid assetId, Guid newBusinessUnitId, DateTime transferDate, string reason);
}