using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Localization.UAE.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Localization.UAE.Services;

public class UaeTaxCalculator : ITaxCalculator
{
	private readonly UaeDbContext _context;

	public UaeTaxCalculator(UaeDbContext context)
	{
		_context = context;
	}

	public async Task<(decimal BaseAmount, decimal TaxAmount, decimal TaxRateUsed)> CalculateTaxAsync(Guid taxCodeId, decimal amount, DateTime transactionDate)
	{
		var taxCode = await _context.TaxCodes
			.Include(t => t.Rates)
			.FirstOrDefaultAsync(t => t.Id == taxCodeId);

		if (taxCode == null) throw new Exception("Invalid Tax Code ID");

		// Find the rate active at the transaction date
		var rateObj = taxCode.Rates
			.Where(r => r.EffectiveDate <= transactionDate)
			.OrderByDescending(r => r.EffectiveDate)
			.FirstOrDefault();

		decimal rate = rateObj?.RatePercentage ?? 0;

		decimal baseAmount = 0;
		decimal taxAmount = 0;

		if (taxCode.IsPriceInclusive)
		{
			// Retail Scenario: Price is 105. Rate is 5%.
			// Base = 105 / (1 + 0.05) = 100.
			// Tax = 105 - 100 = 5.
			baseAmount = amount / (1 + (rate / 100));
			taxAmount = amount - baseAmount;
		}
		else
		{
			// Enterprise Scenario: Price is 100. Rate is 5%.
			// Base = 100.
			// Tax = 100 * 0.05 = 5.
			baseAmount = amount;
			taxAmount = amount * (rate / 100);
		}

		return (Math.Round(baseAmount, 2), Math.Round(taxAmount, 2), rate);
	}
}