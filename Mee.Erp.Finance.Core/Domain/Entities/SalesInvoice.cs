using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Shared.Kernel.Domain.Entities;
using System;
using System.Collections.Generic;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class SalesInvoice : BaseEntity
{
    public Guid CustomerId { get; set; }
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Draft;
    
    public Guid CompanyId { get; set; }
    
    /// <summary>
    /// The generated Journal ID after this invoice is posted.
    /// </summary>
    public Guid? JournalId { get; set; }

    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();

	public virtual Customer Customer { get; set; }
}