namespace ErpClink.Modules.Billing.Domain.Invoices;

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Voided = 4
}

public enum InvoiceLineSource
{
    Service = 0,
    PackageItem = 1
}
