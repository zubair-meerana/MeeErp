using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mee.Erp.Finance.Core;

/// <summary>
/// Extension methods for registering finance core services
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds all finance core services to the service collection
    /// </summary>
    public static IServiceCollection AddFinanceCoreServices(this IServiceCollection services)
    {
        // Register validation services
        services.AddScoped<IFinancialValidationService, FinancialValidationService>();

        // Register control services
        services.AddScoped<IFinancialControlService, FinancialControlService>();

        // Register advanced accounting services
        services.AddScoped<IAdvancedAccountingService, AdvancedAccountingService>();

        // Register compliance services
        services.AddScoped<IComplianceService, ComplianceService>();

        // Register cash management services
        services.AddScoped<ICashManagementService, CashManagementService>();

        // Register error correction services
        services.AddScoped<IErrorCorrectionService, ErrorCorrectionService>();

        // Register existing services
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IJournalPostingService, JournalPostingService>();
        services.AddScoped<IFinancialReportService, FinancialReportService>();
        services.AddScoped<IAccountsPayableService, AccountsPayableService>();
        services.AddScoped<IAccountsReceivableService, AccountsReceivableService>();
        services.AddScoped<IBankService, BankService>();
        services.AddScoped<IFixedAssetService, FixedAssetService>();
        services.AddScoped<ICreditDebitNoteService, CreditDebitNoteService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<IFinancialPeriodService, FinancialPeriodService>();
        services.AddScoped<IAgingReportService, AgingReportService>();
        services.AddScoped<IAssetDisposalService, AssetDisposalService>();
        services.AddScoped<IPartnerService, PartnerService>();

        // Register new advanced financial services
        services.AddScoped<IAdvancedFinancialControlService, AdvancedFinancialControlService>();
        services.AddScoped<IMultiCurrencyFxService, MultiCurrencyFxService>();
        services.AddScoped<IAdvancedTaxService, AdvancedTaxService>();
        services.AddScoped<IBudgetingPlanningService, BudgetingPlanningService>();
        services.AddScoped<ITreasuryCashManagementService, TreasuryCashManagementService>();
        services.AddScoped<IFinancialRiskManagementService, FinancialRiskManagementService>();
        services.AddScoped<IAdvancedReconciliationService, AdvancedReconciliationService>();
        services.AddScoped<IFpaService, FpaService>();
        services.AddScoped<IComplianceManagementService, ComplianceManagementService>();
        services.AddScoped<IAdvancedJournalProcessingService, AdvancedJournalProcessingService>();

        return services;
    }
}
