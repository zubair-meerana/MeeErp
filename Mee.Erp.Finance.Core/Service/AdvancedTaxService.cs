using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Localization.UAE.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

/// <summary>
/// Implements advanced tax management
/// </summary>
public class AdvancedTaxService : IAdvancedTaxService
{
    private readonly FinanceDbContext _context;

    public AdvancedTaxService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaxCode>> GetTaxCodesByJurisdictionAsync(string countryCode, Guid companyId)
    {
        // Get tax codes for the specified jurisdiction and company
        var taxCodes = await _context.Set<TaxCode>()
            .Where(tc => tc.Code.StartsWith(countryCode) || tc.Name.Contains(countryCode))
            .ToListAsync();

        return taxCodes;
    }

    public async Task<IEnumerable<TaxCalculationResult>> CalculateTaxesAsync(TaxCalculationRequest request)
    {
        var results = new List<TaxCalculationResult>();

        foreach (var jurisdiction in request.Jurisdictions)
        {
            var taxRate = await GetApplicableTaxRateAsync(jurisdiction, request.TransactionDate);

            var taxAmount = request.TaxableAmount * (taxRate / 100);

            var result = new TaxCalculationResult
            {
                Jurisdiction = $"{jurisdiction.CountryCode}-{jurisdiction.StateCode}-{jurisdiction.CityCode}",
                TaxType = jurisdiction.TaxType,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                TaxableAmount = request.TaxableAmount,
                EffectiveDate = request.TransactionDate,
                IsExempt = false // Will be updated if exemption applies
            };

            results.Add(result);
        }

        return results;
    }

    public async Task<byte[]> GenerateTaxComplianceReportAsync(TaxComplianceRequest request)
    {
        // Generate tax compliance report based on the request parameters
        var transactions = await GetTaxableTransactionsAsync(request.CompanyId, request.StartDate, request.EndDate);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Tax Compliance Report - {request.ReportType}");
        sb.AppendLine($"Company ID: {request.CompanyId}");
        sb.AppendLine($"Country: {request.CountryCode}");
        sb.AppendLine($"Period: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}");
        sb.AppendLine();

        sb.AppendLine("Transaction ID | Date | Type | Amount | Tax Amount | Tax Rate");
        sb.AppendLine("---------------|------|-----|-------|------------|---------");

        foreach (var transaction in transactions)
        {
            sb.AppendLine($"{transaction.Id} | {transaction.TransactionDate:yyyy-MM-dd} | {transaction.Type} | {transaction.Amount:C} | {transaction.TaxAmount:C} | {transaction.TaxRate}%");
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<TaxCode> CreateOrUpdateTaxCodeWithHistoryAsync(TaxCode taxCode, TaxCodeRate rate, Guid companyId)
    {
        // Check if tax code already exists
        var existingTaxCode = await _context.Set<TaxCode>()
            .FirstOrDefaultAsync(tc => tc.Code == taxCode.Code && tc.Id == taxCode.Id);

        if (existingTaxCode != null)
        {
            // Update existing tax code
            existingTaxCode.Name = taxCode.Name;
            existingTaxCode.Description = taxCode.Description;
            existingTaxCode.IsSalesTax = taxCode.IsSalesTax;
            existingTaxCode.IsPurchaseTax = taxCode.IsPurchaseTax;
            existingTaxCode.IsPriceInclusive = taxCode.IsPriceInclusive;

            // Add new rate to history
            var newRate = new TaxCodeRate
            {
                TaxCodeId = existingTaxCode.Id,
                RatePercentage = rate.RatePercentage,
                EffectiveDate = rate.EffectiveDate
            };

            _context.Set<TaxCodeRate>().Add(newRate);
        }
        else
        {
            // Create new tax code
            _context.Set<TaxCode>().Add(taxCode);

            // Add initial rate
            rate.TaxCodeId = taxCode.Id;
            _context.Set<TaxCodeRate>().Add(rate);
        }

        await _context.SaveChangesAsync();
        return taxCode;
    }

    public async Task<ReverseChargeResult> ProcessReverseChargeAsync(ReverseChargeRequest request)
    {
        // Determine if reverse charge applies based on country and supply type
        var isReverseChargeApplicable = await IsReverseChargeApplicableAsync(request);

        if (!isReverseChargeApplicable)
        {
            return new ReverseChargeResult
            {
                IsReverseChargeApplied = false,
                TaxAmount = 0,
                ComplianceRequirement = "Reverse charge not applicable for this transaction"
            };
        }

        // Calculate tax amount
        var taxAmount = request.TaxableAmount * 0.05m; // Assuming 5% VAT rate as example

        // Create accounting entries for reverse charge
        var accountingEntries = await CreateReverseChargeAccountingEntriesAsync(request, taxAmount);

        return new ReverseChargeResult
        {
            IsReverseChargeApplied = true,
            TaxAmount = taxAmount,
            TaxAccount = "VAT Payable",
            AccountingEntries = accountingEntries,
            ComplianceRequirement = "Reverse charge mechanism applied - customer accounts for VAT"
        };
    }

    public async Task<TaxExemptionResult> ProcessTaxExemptionAsync(TaxExemptionRequest request)
    {
        // Validate exemption certificate
        var isValidExemption = await ValidateExemptionCertificateAsync(request.ExemptionCertificateNumber, request.CustomerId);

        if (!isValidExemption)
        {
            return new TaxExemptionResult
            {
                IsExempt = false,
                TaxableAmount = request.TaxableAmount,
                TaxAmount = request.TaxableAmount * 0.05m // Assuming 5% rate
            };
        }

        // Create accounting entries for exempt transaction
        var accountingEntries = await CreateExemptTransactionAccountingEntriesAsync(request);

        return new TaxExemptionResult
        {
            IsExempt = true,
            ExemptionCertificateNumber = request.ExemptionCertificateNumber,
            ExemptionReason = request.ExemptionReason,
            TaxableAmount = request.TaxableAmount,
            TaxAmount = 0,
            AccountingEntries = accountingEntries
        };
    }

    public async Task<VatGstResult> ProcessVatGstAsync(VatGstRequest request)
    {
        // Validate VAT/GST number if provided
        var isValidVatNumber = string.IsNullOrEmpty(request.VatGstNumber) ||
                              await ValidateVatGstNumberAsync(request.VatGstNumber, request.CountryCode);

        // Calculate tax amount
        var taxAmount = request.TaxableAmount * (request.TaxRate / 100);
        var grossAmount = request.TaxableAmount + taxAmount;

        // Create accounting entries
        var accountingEntries = await CreateVatGstAccountingEntriesAsync(request, taxAmount);

        return new VatGstResult
        {
            IsValid = isValidVatNumber,
            VatGstNumber = request.VatGstNumber,
            TaxAmount = taxAmount,
            NetAmount = request.TaxableAmount,
            GrossAmount = grossAmount,
            AccountingEntries = accountingEntries,
            ComplianceStatus = isValidVatNumber ? "Valid" : "Invalid VAT Number",
            NextFilingDate = CalculateNextFilingDate(request.CountryCode, request.TransactionDate)
        };
    }

    #region Helper Methods

    private async Task<decimal> GetApplicableTaxRateAsync(TaxJurisdiction jurisdiction, DateTime asOfDate)
    {
        // In a real implementation, this would look up the tax rate for the specific jurisdiction
        // considering the date and other factors
        // For this example, returning a default rate based on jurisdiction type

        return jurisdiction.TaxType.ToLower() switch
        {
            "vat" => 5.0m, // UAE VAT rate
            "gst" => 10.0m, // Australian GST rate
            "salestax" => 8.0m, // US average sales tax
            _ => 5.0m // Default rate
        };
    }

    private async Task<List<Journal>> CreateReverseChargeAccountingEntriesAsync(ReverseChargeRequest request, decimal taxAmount)
    {
        var entries = new List<Journal>();

        // Create journal entry for reverse charge
        var journal = new Journal
        {
            JournalDate = DateTime.Today,
            Description = $"Reverse Charge VAT - Supplier: {request.SupplierId}, Customer: {request.CustomerId}",
            CompanyId = request.CompanyId,
            Status = Domain.Enums.JournalStatus.Draft
        };

        // Debit VAT Input Tax (since customer is accounting for VAT)
        journal.Entries.Add(new JournalEntry
        {
            AccountId = await GetVatInputAccountIdAsync(request.CompanyId),
            Debit = taxAmount,
            Credit = 0,
            Description = "VAT Input Tax - Reverse Charge"
        });

        // Credit VAT Output Tax (since customer is accounting for VAT)
        journal.Entries.Add(new JournalEntry
        {
            AccountId = await GetVatOutputAccountIdAsync(request.CompanyId),
            Debit = 0,
            Credit = taxAmount,
            Description = "VAT Output Tax - Reverse Charge"
        });

        entries.Add(journal);
        return entries;
    }

    private async Task<List<Journal>> CreateExemptTransactionAccountingEntriesAsync(TaxExemptionRequest request)
    {
        var entries = new List<Journal>();

        // Create journal entry for exempt transaction
        var journal = new Journal
        {
            JournalDate = DateTime.Today,
            Description = $"Tax Exempt Transaction - Certificate: {request.ExemptionCertificateNumber}",
            CompanyId = request.CompanyId,
            Status = Domain.Enums.JournalStatus.Draft
        };

        // Only record the base transaction without tax
        journal.Entries.Add(new JournalEntry
        {
            AccountId = await GetSalesAccountIdAsync(request.CompanyId),
            Debit = 0,
            Credit = request.TaxableAmount,
            Description = "Sales - Exempt Transaction"
        });

        journal.Entries.Add(new JournalEntry
        {
            AccountId = await GetReceivablesAccountIdAsync(request.CompanyId),
            Debit = request.TaxableAmount,
            Credit = 0,
            Description = "Accounts Receivable - Exempt Transaction"
        });

        entries.Add(journal);
        return entries;
    }

    private async Task<List<Journal>> CreateVatGstAccountingEntriesAsync(VatGstRequest request, decimal taxAmount)
    {
        var entries = new List<Journal>();

        // Create journal entry for VAT/GST transaction
        var journal = new Journal
        {
            JournalDate = DateTime.Today,
            Description = $"VAT/GST Transaction - {request.TransactionType} for {request.CountryCode}",
            CompanyId = request.CompanyId,
            Status = Domain.Enums.JournalStatus.Draft
        };

        if (request.TransactionType.ToLower() == "sale")
        {
            // For sales: debit receivables, credit sales and VAT
            journal.Entries.Add(new JournalEntry
            {
                AccountId = await GetReceivablesAccountIdAsync(request.CompanyId),
                Debit = request.TaxableAmount + taxAmount,
                Credit = 0,
                Description = "Accounts Receivable"
            });

            journal.Entries.Add(new JournalEntry
            {
                AccountId = await GetSalesAccountIdAsync(request.CompanyId),
                Debit = 0,
                Credit = request.TaxableAmount,
                Description = "Sales Revenue"
            });

            journal.Entries.Add(new JournalEntry
            {
                AccountId = await GetVatOutputAccountIdAsync(request.CompanyId),
                Debit = 0,
                Credit = taxAmount,
                Description = "VAT Collected"
            });
        }
        else // Purchase
        {
            // For purchases: debit expense/asset and VAT, credit payables
            journal.Entries.Add(new JournalEntry
            {
                AccountId = await GetExpenseAccountIdAsync(request.CompanyId),
                Debit = request.TaxableAmount,
                Credit = 0,
                Description = "Expense/Asset"
            });

            journal.Entries.Add(new JournalEntry
            {
                AccountId = await GetVatInputAccountIdAsync(request.CompanyId),
                Debit = taxAmount,
                Credit = 0,
                Description = "VAT Paid"
            });

            journal.Entries.Add(new JournalEntry
            {
                AccountId = await GetPayablesAccountIdAsync(request.CompanyId),
                Debit = 0,
                Credit = request.TaxableAmount + taxAmount,
                Description = "Accounts Payable"
            });
        }

        entries.Add(journal);
        return entries;
    }

    private async Task<bool> IsReverseChargeApplicableAsync(ReverseChargeRequest request)
    {
        // Determine if reverse charge applies based on:
        // - Country regulations
        // - Supply type (goods vs services)
        // - Customer type (B2B vs B2C)
        // - VAT registration status

        // For this example, assuming reverse charge applies for B2B services in certain countries
        var reverseChargeCountries = new[] { "GB", "DE", "FR", "AE" }; // Countries with reverse charge rules

        return reverseChargeCountries.Contains(request.CountryCode.ToUpper()) &&
               request.CustomerType == "B2B" &&
               request.SupplyType == "Services";
    }

    private async Task<bool> ValidateExemptionCertificateAsync(string certificateNumber, Guid customerId)
    {
        // In a real implementation, this would validate the certificate against a registry
        // For this example, returning true for demonstration
        return !string.IsNullOrEmpty(certificateNumber);
    }

    private async Task<bool> ValidateVatGstNumberAsync(string vatNumber, string countryCode)
    {
        // In a real implementation, this would validate the VAT number against official registries
        // For this example, returning true for demonstration
        return !string.IsNullOrEmpty(vatNumber);
    }

    private DateTime? CalculateNextFilingDate(string countryCode, DateTime transactionDate)
    {
        // Calculate next filing date based on country and frequency
        // This is a simplified approach
        return transactionDate.AddMonths(1); // Monthly filing as default
    }

    private async Task<Guid> GetVatInputAccountIdAsync(Guid companyId)
    {
        // In a real implementation, this would look up the appropriate VAT input account
        return Guid.NewGuid();
    }

    private async Task<Guid> GetVatOutputAccountIdAsync(Guid companyId)
    {
        // In a real implementation, this would look up the appropriate VAT output account
        return Guid.NewGuid();
    }

    private async Task<Guid> GetSalesAccountIdAsync(Guid companyId)
    {
        // In a real implementation, this would look up the appropriate sales account
        return Guid.NewGuid();
    }

    private async Task<Guid> GetReceivablesAccountIdAsync(Guid companyId)
    {
        // In a real implementation, this would look up the appropriate receivables account
        return Guid.NewGuid();
    }

    private async Task<Guid> GetExpenseAccountIdAsync(Guid companyId)
    {
        // In a real implementation, this would look up the appropriate expense account
        return Guid.NewGuid();
    }

    private async Task<Guid> GetPayablesAccountIdAsync(Guid companyId)
    {
        // In a real implementation, this would look up the appropriate payables account
        return Guid.NewGuid();
    }

    private async Task<List<TaxableTransaction>> GetTaxableTransactionsAsync(Guid companyId, DateTime startDate, DateTime endDate)
    {
        // This would typically aggregate from various sources:
        // - Sales invoices
        // - Purchase invoices
        // - Other taxable transactions

        // For this example, returning a placeholder list
        return new List<TaxableTransaction>
        {
            new TaxableTransaction { Id = Guid.NewGuid(), TransactionDate = DateTime.Today.AddDays(-10), Type = "Sale", Amount = 10000, TaxAmount = 500, TaxRate = 5.0m },
            new TaxableTransaction { Id = Guid.NewGuid(), TransactionDate = DateTime.Today.AddDays(-5), Type = "Purchase", Amount = 5000, TaxAmount = 250, TaxRate = 5.0m }
        };
    }

    #endregion
}

internal class TaxableTransaction
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Type { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
}
