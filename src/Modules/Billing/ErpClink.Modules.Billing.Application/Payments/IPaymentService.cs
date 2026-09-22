using ErpClink.Modules.Billing.Application.Payments.Models;

namespace ErpClink.Modules.Billing.Application.Payments;

public interface IPaymentService
{
    Task<PaymentDto> RecordAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedPaymentsResult> SearchAsync(SearchPaymentsRequest request, CancellationToken cancellationToken = default);
    Task<PaymentDto> ReverseAsync(Guid id, ReversePaymentRequest request, CancellationToken cancellationToken = default);
}

public interface IPaymentNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
