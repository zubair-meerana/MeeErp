namespace Mee.Erp.Finance.Core.Domain.Enums;

public enum SalesInvoiceStatus
{
    Draft = 1,
    AwaitingPayment = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Void = 5
}