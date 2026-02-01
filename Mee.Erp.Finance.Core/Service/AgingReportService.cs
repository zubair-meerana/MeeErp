using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Contracts.Dtos;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class AgingReportService : IAgingReportService
{
    private readonly FinanceDbContext _context;
    private readonly ICurrencyService _currencyService;

    public AgingReportService(FinanceDbContext context, ICurrencyService currencyService)
    {
        _context = context;
        _currencyService = currencyService;
    }

    public async Task<AgingReportDto> GetAccountsReceivableAgingAsync(AgingReportRequest request)
    {
        var report = new AgingReportDto
        {
            AsOfDate = request.AsOfDate,
            ReportType = "AR",
            Currency = request.Currency
        };

        // Get unpaid sales invoices
        var query = _context.SalesInvoices
            .Include(si => si.Customer)
            .Where(si => si.CompanyId == request.CompanyId)
            .Where(si => si.InvoiceDate <= request.AsOfDate)
            .Where(si => si.Status != SalesInvoiceStatus.Paid);

        // Note: BusinessUnit filtering would require extending SalesInvoice entity

        if (request.CustomerIds != null && request.CustomerIds.Any())
        {
            query = query.Where(si => request.CustomerIds.Contains(si.CustomerId));
        }

        var unpaidInvoices = await query.ToListAsync();

        foreach (var invoice in unpaidInvoices)
        {
            // Note: In current implementation, we can't track payments to specific invoices
            // This would require extending the entities to include payment-invoice relationships
            // For now, we'll assume no payments have been made
            var outstandingAmount = invoice.TotalAmount;

            if (outstandingAmount <= 0) continue;

            // Convert to base currency if needed
            // Note: Invoice currency field would need to be added to SalesInvoice entity
            var baseCurrencyAmount = outstandingAmount; // Simplified for current structure

            // Calculate aging
            var daysOverdue = (request.AsOfDate - invoice.DueDate).Days;
            var agingBucket = GetAgingBucket(daysOverdue);

            // Add to details
            report.Details.Add(new AgingDetailDto
            {
                EntityId = invoice.CustomerId,
                EntityName = invoice.Customer.Name,
                DocumentNumber = invoice.InvoiceNumber,
                DocumentDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                OriginalAmount = invoice.TotalAmount,
                OutstandingAmount = baseCurrencyAmount,
                DaysOverdue = Math.Max(0, daysOverdue),
                AgingBucket = agingBucket,
                Currency = request.Currency
            });

            // Update bucket totals
            UpdateAgingBucket(report, agingBucket, baseCurrencyAmount, 1);
        }

        CalculatePercentages(report);
        return report;
    }

    public async Task<AgingReportDto> GetAccountsPayableAgingAsync(AgingReportRequest request)
    {
        var report = new AgingReportDto
        {
            AsOfDate = request.AsOfDate,
            ReportType = "AP",
            Currency = request.Currency
        };

        // Get unpaid purchase invoices
        var query = _context.PurchaseInvoices
            .Include(pi => pi.Supplier)
            .Where(pi => pi.CompanyId == request.CompanyId)
            .Where(pi => pi.InvoiceDate <= request.AsOfDate)
            .Where(pi => pi.Status != PurchaseInvoiceStatus.Paid);

        // Note: BusinessUnit filtering would require extending PurchaseInvoice entity

        if (request.SupplierIds != null && request.SupplierIds.Any())
        {
            query = query.Where(pi => request.SupplierIds.Contains(pi.SupplierId));
        }

        var unpaidInvoices = await query.ToListAsync();

        foreach (var invoice in unpaidInvoices)
        {
            // Note: In current implementation, we can't track payments to specific invoices
            // This would require extending the entities to include payment-invoice relationships
            // For now, we'll assume no payments have been made
            var outstandingAmount = invoice.TotalAmount;

            if (outstandingAmount <= 0) continue;

            // Convert to base currency if needed
            // Note: Invoice currency field would need to be added to PurchaseInvoice entity
            var baseCurrencyAmount = outstandingAmount; // Simplified for current structure

            // Calculate aging
            var daysOverdue = (request.AsOfDate - invoice.DueDate).Days;
            var agingBucket = GetAgingBucket(daysOverdue);

            // Add to details
            report.Details.Add(new AgingDetailDto
            {
                EntityId = invoice.SupplierId,
                EntityName = invoice.Supplier.Name,
                DocumentNumber = invoice.InvoiceNumber,
                DocumentDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                OriginalAmount = invoice.TotalAmount,
                OutstandingAmount = baseCurrencyAmount,
                DaysOverdue = Math.Max(0, daysOverdue),
                AgingBucket = agingBucket,
                Currency = request.Currency
            });

            // Update bucket totals
            UpdateAgingBucket(report, agingBucket, baseCurrencyAmount, 1);
        }

        CalculatePercentages(report);
        return report;
    }

    private string GetAgingBucket(int daysOverdue)
    {
        return daysOverdue switch
        {
            <= 0 => "Current",
            > 0 and <= 30 => "1-30",
            > 30 and <= 60 => "31-60",
            > 60 and <= 90 => "61-90",
            > 90 and <= 120 => "91-120",
            > 120 => "Over120"
        };
    }

    private void UpdateAgingBucket(AgingReportDto report, string bucket, decimal amount, int count)
    {
        switch (bucket)
        {
            case "Current":
                report.Current.Amount += amount;
                report.Current.Count += count;
                break;
            case "1-30":
                report.Bucket1_30.Amount += amount;
                report.Bucket1_30.Count += count;
                break;
            case "31-60":
                report.Bucket31_60.Amount += amount;
                report.Bucket31_60.Count += count;
                break;
            case "61-90":
                report.Bucket61_90.Amount += amount;
                report.Bucket61_90.Count += count;
                break;
            case "91-120":
                report.Bucket91_120.Amount += amount;
                report.Bucket91_120.Count += count;
                break;
            case "Over120":
                report.BucketOver120.Amount += amount;
                report.BucketOver120.Count += count;
                break;
        }

        report.TotalOutstanding += amount;
        report.TotalRecords += count;
    }

    private void CalculatePercentages(AgingReportDto report)
    {
        if (report.TotalOutstanding > 0)
        {
            report.Current.Percentage = (report.Current.Amount / report.TotalOutstanding) * 100;
            report.Bucket1_30.Percentage = (report.Bucket1_30.Amount / report.TotalOutstanding) * 100;
            report.Bucket31_60.Percentage = (report.Bucket31_60.Amount / report.TotalOutstanding) * 100;
            report.Bucket61_90.Percentage = (report.Bucket61_90.Amount / report.TotalOutstanding) * 100;
            report.Bucket91_120.Percentage = (report.Bucket91_120.Amount / report.TotalOutstanding) * 100;
            report.BucketOver120.Percentage = (report.BucketOver120.Amount / report.TotalOutstanding) * 100;
        }
    }
}