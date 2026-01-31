using Mee.Erp.Configuration.Contracts.Interfaces;
using Mee.Erp.Core.Domain.Enums;
using Mee.Erp.Core.Persistence; // Needs access to Core tables
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Mee.Erp.Configuration.Services;

public class ErpConfigurationService : IErpConfigurationService
{
    private readonly IConfiguration _technicalConfig;
    private readonly CoreDbContext _coreDbContext;

    public ErpConfigurationService(IConfiguration technicalConfig, CoreDbContext coreDbContext)
    {
        _technicalConfig = technicalConfig;
        _coreDbContext = coreDbContext;
    }

    // 1. Technical Settings
    public string GetConnectionString(string databaseName)
    {
        // Looks for "ConnectionStrings:FinanceDb" or "ConnectionStrings:CoreDb" in appsettings.json
        return _technicalConfig.GetConnectionString(databaseName) 
               ?? throw new Exception($"Connection string '{databaseName}' not found.");
    }

    // 2. Business Settings
    public async Task<OperatingMode> GetOperatingModeAsync(Guid companyId)
    {
        var company = await _coreDbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId);

        if (company == null) throw new Exception("Company not found.");

        return company.OperatingMode;
    }

    public async Task<int> GetCompanyDefaultAccountingMethodAsync(Guid companyId)
    {
        // In a real scenario, we would have a 'CompanySettings' table.
        // For this simple build, let's assume we added a 'DefaultAccountingMethod' field to the Company entity
        // or we return a hardcoded default (Accrual = 1) if not found.
        
        // For demonstration, returning 1 (Accrual).
        return 1; 
    }
}