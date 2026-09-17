namespace ERP.Domain.Sales
{
    public record ProformaInvoiceSettledDomainEvent(
        int ProformaInvoiceId,
        int? SalesOrderId,
        string PiNumber
    );
}
