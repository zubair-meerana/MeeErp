using Mee.Erp.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Domain.Enums; // We need to reference Finance Core Enums or move Enums to Shared
using System;
using System.Threading.Tasks;

namespace Mee.Erp.Configuration.Contracts.Interfaces;

public interface IErpConfigurationService
{
    // --- Technical Settings (from appsettings.json) ---
    string GetConnectionString(string databaseName);

    // --- Business Settings (from Database) ---
    
    /// <summary>
    /// Checks if the company is in Enterprise or Retail mode.
    /// </summary>
    Task<OperatingMode> GetOperatingModeAsync(Guid companyId);

    /// <summary>
    /// Gets the default accounting method (Cash/Accrual) for the company.
    /// This is the "fallback" if the Account or Business Unit doesn't specify one.
    /// </summary>
    // Note: Since AccountingMethod enum is in Finance.Core, strictly speaking 
    // Configuration shouldn't depend on Finance. 
    // Ideally, Enums move to Shared.Kernel. For now, we will return int or assume reference.
    Task<int> GetCompanyDefaultAccountingMethodAsync(Guid companyId);
}