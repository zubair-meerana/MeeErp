using System;
using System.Collections.Generic;

namespace Mee.Erp.Finance.Core.Contracts.Dtos;

public class TrialBalanceRequest
{
    public Guid CompanyId { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid>? BusinessUnitIds { get; set; } // Null or empty means all units
}