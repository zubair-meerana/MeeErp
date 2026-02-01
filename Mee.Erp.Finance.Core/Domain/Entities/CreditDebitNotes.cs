using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Shared.Kernel.Domain.Entities;

namespace Mee.Erp.Finance.Core.Domain.Entities;

public class CreditNote : BaseEntity
{
    public Guid CustomerId { get; set; }
    public required string CreditNoteNumber { get; set; }
    public DateTime CreditNoteDate { get; set; }
    public Guid? OriginalInvoiceId { get; set; }
    public string? Reason { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public CreditNoteStatus Status { get; set; } = CreditNoteStatus.Draft;
    public Guid CompanyId { get; set; }
    public Guid? JournalId { get; set; }
    
    public virtual Customer Customer { get; set; }
    public virtual SalesInvoice? OriginalInvoice { get; set; }
    public ICollection<CreditNoteLine> Lines { get; set; } = new List<CreditNoteLine>();
}

public class CreditNoteLine : BaseEntity
{
    public Guid CreditNoteId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public Guid AccountId { get; set; }
    
    public virtual CreditNote CreditNote { get; set; }
    public virtual Account Account { get; set; }
}

public class DebitNote : BaseEntity
{
    public Guid SupplierId { get; set; }
    public required string DebitNoteNumber { get; set; }
    public DateTime DebitNoteDate { get; set; }
    public Guid? OriginalInvoiceId { get; set; }
    public string? Reason { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DebitNoteStatus Status { get; set; } = DebitNoteStatus.Draft;
    public Guid CompanyId { get; set; }
    public Guid? JournalId { get; set; }
    
    public virtual Supplier Supplier { get; set; }
    public virtual PurchaseInvoice? OriginalInvoice { get; set; }
    public ICollection<DebitNoteLine> Lines { get; set; } = new List<DebitNoteLine>();
}

public class DebitNoteLine : BaseEntity
{
    public Guid DebitNoteId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public Guid AccountId { get; set; }
    
    public virtual DebitNote DebitNote { get; set; }
    public virtual Account Account { get; set; }
}

public enum CreditNoteStatus
{
    Draft = 0,
    Posted = 1,
    Applied = 2,
    Cancelled = 3
}

public enum DebitNoteStatus
{
    Draft = 0,
    Posted = 1,
    Applied = 2,
    Cancelled = 3
}