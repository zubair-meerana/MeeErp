using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IFixedAssetService
{
	Task RunDepreciationForMonthAsync(Guid companyId, DateTime periodEnd, Guid userId);
}

