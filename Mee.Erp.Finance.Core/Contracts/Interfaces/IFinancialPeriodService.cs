using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IFinancialPeriodService
{
    Task<FinancialPeriod> CreatePeriodAsync(FinancialPeriod period);
    Task<FinancialPeriod> ClosePeriodAsync(Guid periodId, Guid userId, string? notes = null);
    Task<FinancialPeriod> ReopenPeriodAsync(Guid periodId, Guid userId);
    Task<List<FinancialPeriod>> GetPeriodsAsync(Guid companyId);
    Task<FinancialPeriod?> GetCurrentPeriodAsync(Guid companyId);
    Task<FinancialPeriod?> GetPeriodByDateAsync(Guid companyId, DateTime date);
    Task<bool> CanPostToPeriodAsync(Guid periodId);
    Task<List<FinancialPeriod>> GetOpenPeriodsAsync(Guid companyId);
    Task<FinancialPeriod> CreateFiscalYearAsync(Guid companyId, int fiscalYear, DateTime startDate, DateTime endDate, int numberOfPeriods = 12);
}