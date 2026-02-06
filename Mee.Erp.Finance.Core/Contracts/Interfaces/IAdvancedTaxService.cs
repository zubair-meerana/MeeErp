using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Localization.UAE.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for advanced tax management
/// </summary>
public interface IAdvancedTaxService
{
    /// <summary>
    /// Gets tax codes for multiple jurisdictions
    /// </summary>
    Task<IEnumerable<TaxCode>> GetTaxCodesByJurisdictionAsync(string countryCode, Guid companyId);

    /// <summary>
    /// Calculates taxes for multiple jurisdictions
    /// </summary>
    Task<IEnumerable<TaxCalculationResult>> CalculateTaxesAsync(TaxCalculationRequest request);

    /// <summary>
    /// Generates tax compliance reports for multiple jurisdictions
    /// </summary>
    Task<byte[]> GenerateTaxComplianceReportAsync(TaxComplianceRequest request);

    /// <summary>
    /// Manages tax codes with historical rates
    /// </summary>
    Task<TaxCode> CreateOrUpdateTaxCodeWithHistoryAsync(TaxCode taxCode, TaxCodeRate rate, Guid companyId);

    /// <summary>
    /// Processes reverse charge mechanisms
    /// </summary>
    Task<ReverseChargeResult> ProcessReverseChargeAsync(ReverseChargeRequest request);

    /// <summary>
    /// Handles tax exemptions and special rates
    /// </summary>
    Task<TaxExemptionResult> ProcessTaxExemptionAsync(TaxExemptionRequest request);

    /// <summary>
    /// Manages VAT/GST for multiple countries beyond UAE
    /// </summary>
    Task<VatGstResult> ProcessVatGstAsync(VatGstRequest request);
}

public class TaxCalculationRequest
{
    public Guid CompanyId { get; set; }
    public string SourceCountry { get; set; }
    public string DestinationCountry { get; set; }
    public string CustomerType { get; set; } // "B2B", "B2C", "Government"
    public decimal TaxableAmount { get; set; }
    public string TaxCode { get; set; }
    public DateTime TransactionDate { get; set; }
    public List<TaxJurisdiction> Jurisdictions { get; set; } = new List<TaxJurisdiction>();
}

public class TaxJurisdiction
{
    public string CountryCode { get; set; }
    public string StateCode { get; set; }
    public string CityCode { get; set; }
    public string TaxType { get; set; } // "VAT", "GST", "SalesTax", "UseTax", "Excise"
}

public class TaxCalculationResult
{
    public string Jurisdiction { get; set; }
    public string TaxType { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public DateTime EffectiveDate { get; set; }
    public bool IsExempt { get; set; }
    public string ExemptionReason { get; set; }
}

public class TaxComplianceRequest
{
    public Guid CompanyId { get; set; }
    public string CountryCode { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ReportType { get; set; } // "VAT", "GST", "SalesTax", "Withholding"
    public string FilingFrequency { get; set; } // "Monthly", "Quarterly", "Annually"
}

public class ReverseChargeRequest
{
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid CustomerId { get; set; }
    public string SupplyType { get; set; } // "Goods", "Services"
    public string CountryCode { get; set; }
    public decimal TaxableAmount { get; set; }
    public string TaxCode { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class ReverseChargeResult
{
    public bool IsReverseChargeApplied { get; set; }
    public decimal TaxAmount { get; set; }
    public string TaxAccount { get; set; }
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
    public string ComplianceRequirement { get; set; }
}

public class TaxExemptionRequest
{
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public string ExemptionCertificateNumber { get; set; }
    public string ExemptionReason { get; set; }
    public string TaxCode { get; set; }
    public decimal TaxableAmount { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class TaxExemptionResult
{
    public bool IsExempt { get; set; }
    public string ExemptionCertificateNumber { get; set; }
    public string ExemptionReason { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
}

public class VatGstRequest
{
    public Guid CompanyId { get; set; }
    public string CountryCode { get; set; }
    public string VatGstNumber { get; set; }
    public string TransactionType { get; set; } // "Sale", "Purchase", "Import", "Export"
    public string CustomerType { get; set; } // "Registered", "NonRegistered", "Overseas"
    public decimal TaxableAmount { get; set; }
    public decimal TaxRate { get; set; }
    public DateTime TransactionDate { get; set; }
    public string PlaceOfSupply { get; set; }
}

public class VatGstResult
{
    public bool IsValid { get; set; }
    public string VatGstNumber { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
    public string ComplianceStatus { get; set; }
    public DateTime? NextFilingDate { get; set; }
}
