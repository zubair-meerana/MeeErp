using System;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface ITaxCalculator
{
	/// <summary>
	/// Calculates the tax amount and base amount.
	/// </summary>
	/// <param name="taxCodeId">The ID of the tax code (Standard, Zero, etc).</param>
	/// <param name="amount">The line amount entered by the user.</param>
	/// <param name="transactionDate">Date to pick the correct historic rate.</param>
	/// <returns>Tuple: (BaseAmount, TaxAmount, TaxRateUsed)</returns>
	Task<(decimal BaseAmount, decimal TaxAmount, decimal TaxRateUsed)> CalculateTaxAsync(Guid taxCodeId, decimal amount, DateTime transactionDate);
}