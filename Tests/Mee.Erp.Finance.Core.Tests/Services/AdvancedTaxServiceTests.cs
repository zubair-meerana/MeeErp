using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Services;
using Mee.Erp.Finance.Localization.UAE.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class AdvancedTaxServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly AdvancedTaxService _service;

    public AdvancedTaxServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new AdvancedTaxService(_mockContext.Object);
    }

    [Fact]
    public async Task GetTaxCodesByJurisdictionAsync_ValidCountry_ReturnsTaxCodes()
    {
        // Arrange
        var countryCode = "US";
        var companyId = Guid.NewGuid();

        var taxCodes = new List<TaxCode>
        {
            new TaxCode { Id = Guid.NewGuid(), Code = "US-FED", Name = "US Federal Tax", Description = "Federal tax for US" },
            new TaxCode { Id = Guid.NewGuid(), Code = "US-CA", Name = "California State Tax", Description = "State tax for California" }
        };

        _mockContext.Setup(c => c.Set<TaxCode>().Where(It.IsAny<System.Linq.Expressions.Expression<Func<TaxCode, bool>>>()))
            .Returns(taxCodes.AsQueryable());

        // Act
        var result = await _service.GetTaxCodesByJurisdictionAsync(countryCode, companyId);

        // Assert
        Assert.NotNull(result);
        var resultCodes = result.ToList();
        Assert.Equal(2, resultCodes.Count);
        Assert.Contains(resultCodes, tc => tc.Code == "US-FED");
        Assert.Contains(resultCodes, tc => tc.Code == "US-CA");
    }

    [Fact]
    public async Task CalculateTaxesAsync_ValidRequest_ReturnsTaxResults()
    {
        // Arrange
        var request = new TaxCalculationRequest
        {
            CompanyId = Guid.NewGuid(),
            SourceCountry = "US",
            DestinationCountry = "US",
            CustomerType = "B2B",
            TaxableAmount = 1000,
            TaxCode = "US-FED",
            TransactionDate = DateTime.Today,
            Jurisdictions = new List<TaxJurisdiction>
            {
                new TaxJurisdiction { CountryCode = "US", StateCode = "CA", CityCode = "LA", TaxType = "SalesTax" },
                new TaxJurisdiction { CountryCode = "US", StateCode = "NY", CityCode = "NYC", TaxType = "SalesTax" }
            }
        };

        // Act
        var result = await _service.CalculateTaxesAsync(request);

        // Assert
        Assert.NotNull(result);
        var results = result.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, tr => tr.Jurisdiction == "US-CA-LA");
        Assert.Contains(results, tr => tr.Jurisdiction == "US-NY-NYC");
    }

    [Fact]
    public async Task GenerateTaxComplianceReportAsync_ValidRequest_ReturnsReport()
    {
        // Arrange
        var request = new TaxComplianceRequest
        {
            CompanyId = Guid.NewGuid(),
            CountryCode = "US",
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            ReportType = "SalesTax",
            FilingFrequency = "Monthly"
        };

        // Act
        var result = await _service.GenerateTaxComplianceReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CreateOrUpdateTaxCodeWithHistoryAsync_NewTaxCode_CreatesTaxCode()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var taxCode = new TaxCode
        {
            Id = Guid.NewGuid(),
            Code = "TEST-TAX",
            Name = "Test Tax Code",
            Description = "Test tax code for unit testing",
            IsSalesTax = true,
            IsPurchaseTax = true,
            IsPriceInclusive = false
        };

        var rate = new TaxCodeRate
        {
            Id = Guid.NewGuid(),
            RatePercentage = 10.0m,
            EffectiveDate = DateTime.Today
        };

        _mockContext.Setup(c => c.Set<TaxCode>().Add(It.IsAny<TaxCode>()));
        _mockContext.Setup(c => c.Set<TaxCodeRate>().Add(It.IsAny<TaxCodeRate>()));
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.CreateOrUpdateTaxCodeWithHistoryAsync(taxCode, rate, companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taxCode.Id, result.Id);
        Assert.Equal(taxCode.Name, result.Name);
    }

    [Fact]
    public async Task ProcessReverseChargeAsync_ValidRequest_ReturnsResult()
    {
        // Arrange
        var request = new ReverseChargeRequest
        {
            CompanyId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            SupplyType = "Services",
            CountryCode = "GB", // UK has reverse charge rules
            TaxableAmount = 1000,
            TaxCode = "VAT",
            TransactionDate = DateTime.Today
        };

        // Act
        var result = await _service.ProcessReverseChargeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsReverseChargeApplied);
        Assert.Equal("Reverse charge mechanism applied - customer accounts for VAT", result.ComplianceRequirement);
    }

    [Fact]
    public async Task ProcessTaxExemptionAsync_ValidExemption_ReturnsResult()
    {
        // Arrange
        var request = new TaxExemptionRequest
        {
            CompanyId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ExemptionCertificateNumber = "EXMPT-001",
            ExemptionReason = "Charitable organization",
            TaxCode = "SALES-TAX",
            TaxableAmount = 5000,
            TransactionDate = DateTime.Today
        };

        // Act
        var result = await _service.ProcessTaxExemptionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsExempt);
        Assert.Equal(request.ExemptionCertificateNumber, result.ExemptionCertificateNumber);
        Assert.Equal(0, result.TaxAmount);
    }

    [Fact]
    public async Task ProcessVatGstAsync_ValidRequest_ReturnsResult()
    {
        // Arrange
        var request = new VatGstRequest
        {
            CompanyId = Guid.NewGuid(),
            CountryCode = "AU",
            VatGstNumber = "123456789",
            TransactionType = "Sale",
            CustomerType = "Registered",
            TaxableAmount = 1000,
            TaxRate = 10.0m,
            TransactionDate = DateTime.Today,
            PlaceOfSupply = "Australia"
        };

        // Act
        var result = await _service.ProcessVatGstAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(request.VatGstNumber, result.VatGstNumber);
        Assert.Equal(100, result.TaxAmount); // 1000 * 10%
        Assert.Equal(1000, result.NetAmount);
        Assert.Equal(1100, result.GrossAmount);
    }
}
