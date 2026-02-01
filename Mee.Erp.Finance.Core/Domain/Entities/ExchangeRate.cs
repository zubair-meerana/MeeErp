using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class ExchangeRate : BaseEntity
{
    public required string FromCurrencyCode { get; set; }
    public required string ToCurrencyCode { get; set; }
    public decimal Rate { get; set; } // Rate = 1 unit of FromCurrency = Rate units of ToCurrency
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid CompanyId { get; set; }
    public bool IsActive { get; set; } = true;
    public ExchangeRateSource Source { get; set; } = ExchangeRateSource.Manual;
    public string? SourceReference { get; set; }
}

public enum ExchangeRateSource
{
    Manual = 0,
    CentralBank = 1,
    BankFeed = 2,
    Api = 3
}