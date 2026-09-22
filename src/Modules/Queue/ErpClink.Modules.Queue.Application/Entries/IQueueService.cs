using ErpClink.Modules.Queue.Application.Entries.Models;

namespace ErpClink.Modules.Queue.Application.Entries;

public interface IQueueService
{
    Task<QueueEntryDto> CheckInAsync(CheckInRequest request, CancellationToken cancellationToken = default);
    Task<QueueEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueueListItemDto>> GetTodayAsync(TodayQueueRequest request, CancellationToken cancellationToken = default);
    Task<PagedQueueResult> SearchAsync(SearchQueueRequest request, CancellationToken cancellationToken = default);
    Task CallAsync(Guid id, CancellationToken cancellationToken = default);
    Task StartServiceAsync(Guid id, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task SkipAsync(Guid id, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IQueueNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, Guid branchId, Guid clinicId, DateOnly queueDate, CancellationToken cancellationToken = default);
}
