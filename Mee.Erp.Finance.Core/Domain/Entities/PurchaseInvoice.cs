using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Shared.Kernel.Domain.Entities;
using System;
using System.Collections.Generic;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class PurchaseInvoice : BaseEntity
{
    public Guid SupplierId { get; set; }
    
    /// <summary>
    /// The supplier's own invoice number. Must be unique per supplier.
    /// </summary>
    public required string InvoiceNumber { get; set; }
    
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    
    public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Draft;
    
    public Guid CompanyId { get; set; }

    /// <summary>
    /// The generated Journal ID after this invoice is posted.
    /// </summary>
    public Guid? JournalId { get; set; }

    public ICollection<PurchaseInvoiceLine> Lines { get; set; } = new List<PurchaseInvoiceLine>();

	public virtual Supplier Supplier { get; set; }
}