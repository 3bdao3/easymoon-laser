using ErpClink.Modules.Billing.Application.Invoices.Models;

namespace ErpClink.Modules.Billing.Application.Invoices;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateDraftAsync(CreateDraftInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<PagedInvoicesResult> SearchAsync(SearchInvoicesRequest request, CancellationToken cancellationToken = default);
    Task<PagedInvoicesResult> GetPatientInvoicesAsync(Guid patientId, PatientInvoicesRequest request, CancellationToken cancellationToken = default);
    Task<PatientOutstandingDto> GetPatientOutstandingAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<InvoiceDto> UpdateDraftAsync(Guid id, UpdateDraftInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto> AddLineAsync(Guid id, AddInvoiceLineRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto> UpdateLineAsync(Guid id, Guid lineId, UpdateInvoiceLineRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto> RemoveLineAsync(Guid id, Guid lineId, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<InvoiceDto> IssueAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<InvoiceDto> VoidAsync(Guid id, VoidInvoiceRequest request, CancellationToken cancellationToken = default);
}

public interface IInvoiceNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
