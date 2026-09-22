using ErpClink.Modules.Finance.Application.Journals.Models;

namespace ErpClink.Modules.Finance.Application.Journals;

public interface IJournalService
{
    Task<JournalEntryDto> CreateDraftAsync(CreateJournalDraftRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedJournalsResult> SearchAsync(SearchJournalsRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto> UpdateDraftAsync(Guid id, UpdateJournalDraftRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto> ReplaceLinesAsync(Guid id, ReplaceJournalLinesRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto> PostAsync(Guid id, PostJournalRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto> ReverseAsync(Guid id, ReverseJournalRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JournalHistoryEntryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IJournalNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
