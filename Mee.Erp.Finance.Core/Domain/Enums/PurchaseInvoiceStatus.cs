namespace Mee.Erp.Finance.Core.Domain.Enums;

public enum PurchaseInvoiceStatus
{
    Draft = 1,
    AwaitingApproval = 2,
    AwaitingPayment = 3,
    Paid = 4,
    Cancelled = 5
}