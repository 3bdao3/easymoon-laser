using ErpClink.Modules.Assets.Domain.Accounting;

namespace ErpClink.Modules.Assets.Application.Accounting.Models;

public sealed record CapitalizeAssetRequest(
    Guid AssetId,
    decimal? AcquisitionCost,
    decimal CapitalizedCost,
    decimal ResidualValue,
    DateOnly CapitalizationDate,
    DateOnly DepreciationStartDate,
    int UsefulLifeMonths,
    byte[]? AssetRowVersion);

public sealed record PostDepreciationRequest(
    Guid AssetId,
    string? PeriodKey,
    DateOnly? TransactionDate,
    byte[]? FinancialProfileRowVersion);

public sealed record DisposeAssetFinancialRequest(
    Guid AssetId,
    DateOnly DisposalDate,
    decimal? Proceeds,
    string? Notes,
    byte[]? FinancialProfileRowVersion);

public sealed record AssetFinancialProfileDto(
    Guid Id,
    Guid AssetId,
    Guid BranchId,
    string Status,
    decimal AcquisitionCost,
    decimal CapitalizedCost,
    decimal ResidualValue,
    decimal DepreciableBase,
    DateOnly CapitalizationDate,
    DateOnly DepreciationStartDate,
    int UsefulLifeMonths,
    string DepreciationMethod,
    int CalculationVersion,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    decimal RemainingDepreciableAmount,
    DateOnly? LastDepreciationPeriod,
    DateOnly? DisposedDate,
    decimal? DisposalProceeds,
    decimal? DisposalGainLoss,
    string? DisposalNotes,
    byte[] RowVersion);

public sealed record AssetAccountingListItemDto(
    Guid AssetId,
    string AssetNumber,
    string AssetName,
    Guid? FinancialProfileId,
    string FinancialStatus,
    decimal? CapitalizedCost,
    decimal? AccumulatedDepreciation,
    decimal? NetBookValue,
    string OperationalStatus);

public sealed record PagedAssetAccountingResult(
    IReadOnlyList<AssetAccountingListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record SearchAssetAccountingRequest(
    Guid? AssetId,
    Guid? CategoryId,
    Guid? LocationId,
    string? FinancialStatus,
    string? OperationalStatus,
    int Page = 1,
    int PageSize = 20);

public sealed record DepreciationScheduleLineDto(
    Guid Id,
    int PeriodIndex,
    string PeriodKey,
    DateOnly PeriodStartDate,
    decimal PlannedAmount,
    string Status,
    Guid? PostedTransactionId);

public sealed record PagedDepreciationScheduleResult(
    IReadOnlyList<DepreciationScheduleLineDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record DepreciationTransactionDto(
    Guid Id,
    Guid AssetId,
    string PeriodKey,
    DateOnly PeriodStartDate,
    DateOnly TransactionDate,
    decimal OpeningNetBookValue,
    decimal DepreciationAmount,
    decimal ClosingAccumulatedDepreciation,
    decimal ClosingNetBookValue,
    string Method,
    int CalculationVersion,
    Guid CorrelationId,
    DateTime CreatedAtUtc);

public sealed record PagedDepreciationTransactionsResult(
    IReadOnlyList<DepreciationTransactionDto> Items,
    decimal TotalDepreciation,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record SearchDepreciationTransactionsRequest(
    Guid? AssetId,
    DateOnly? FromPeriod,
    DateOnly? ToPeriod,
    int Page = 1,
    int PageSize = 50);

public sealed record AssetFinancialHistoryLineDto(
    Guid Id,
    string EventType,
    DateOnly TransactionDate,
    decimal Amount,
    decimal? OpeningNetBookValue,
    decimal? ClosingNetBookValue,
    decimal? AccumulatedDepreciation,
    string SourceType,
    Guid SourceId,
    Guid CorrelationId,
    string? Description,
    DateTime CreatedAtUtc);

public sealed record PagedAssetFinancialHistoryResult(
    IReadOnlyList<AssetFinancialHistoryLineDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AssetValuationLineDto(
    Guid AssetId,
    string AssetNumber,
    string AssetName,
    decimal CapitalizedCost,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    string FinancialStatus);

public sealed record AssetValuationResult(
    decimal TotalCapitalizedCost,
    decimal TotalAccumulatedDepreciation,
    decimal TotalNetBookValue,
    IReadOnlyList<AssetValuationLineDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AssetDisposalDto(
    Guid AssetId,
    string AssetNumber,
    string AssetName,
    DateOnly DisposedDate,
    decimal CapitalizedCost,
    decimal AccumulatedDepreciation,
    decimal NetBookValueAtDisposal,
    decimal? Proceeds,
    decimal? GainLoss,
    string? Notes);

public sealed record PagedAssetDisposalsResult(
    IReadOnlyList<AssetDisposalDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface IAssetAccountingService
{
    Task<AssetFinancialProfileDto> CapitalizeAsync(CapitalizeAssetRequest request, CancellationToken cancellationToken = default);
    Task<DepreciationTransactionDto> PostDepreciationAsync(PostDepreciationRequest request, CancellationToken cancellationToken = default);
    Task<AssetFinancialProfileDto> DisposeFinancialAsync(DisposeAssetFinancialRequest request, CancellationToken cancellationToken = default);
    Task<AssetFinancialProfileDto?> GetProfileAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<PagedAssetAccountingResult> SearchAsync(SearchAssetAccountingRequest request, CancellationToken cancellationToken = default);
    Task<PagedDepreciationScheduleResult> GetScheduleAsync(Guid assetId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<PagedDepreciationTransactionsResult> SearchDepreciationAsync(SearchDepreciationTransactionsRequest request, CancellationToken cancellationToken = default);
    Task<PagedAssetFinancialHistoryResult> GetFinancialHistoryAsync(Guid assetId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<AssetValuationResult> GetValuationAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<PagedAssetDisposalsResult> SearchDisposalsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}
