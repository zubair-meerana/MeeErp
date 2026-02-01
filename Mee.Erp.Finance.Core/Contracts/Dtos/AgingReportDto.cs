using Mee.Erp.Finance.Core.Contracts.Dtos;
using System.Collections.Generic;

namespace Mee.Erp.Finance.Core.Contracts.Dtos;

public class AgingReportRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<Guid>? BusinessUnitIds { get; set; }
    public List<Guid>? CustomerIds { get; set; } // For AR aging
    public List<Guid>? SupplierIds { get; set; } // For AP aging
    public string Currency { get; set; } = "AED"; // Base currency for report
}

public class AgingBucketDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class AgingDetailDto
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
}

public class AgingReportDto
{
    public DateTime AsOfDate { get; set; }
    public string ReportType { get; set; } = string.Empty; // "AR" or "AP"
    public string Currency { get; set; } = string.Empty;
    
    // Summary buckets
    public AgingBucketDto Current { get; set; } = new();
    public AgingBucketDto Bucket1_30 { get; set; } = new();
    public AgingBucketDto Bucket31_60 { get; set; } = new();
    public AgingBucketDto Bucket61_90 { get; set; } = new();
    public AgingBucketDto Bucket91_120 { get; set; } = new();
    public AgingBucketDto BucketOver120 { get; set; } = new();
    
    // Totals
    public decimal TotalOutstanding { get; set; }
    public int TotalRecords { get; set; }
    
    // Details
    public List<AgingDetailDto> Details { get; set; } = new();
}