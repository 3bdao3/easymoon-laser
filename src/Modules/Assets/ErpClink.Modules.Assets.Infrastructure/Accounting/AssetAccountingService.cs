using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Assets.Application.Accounting;
using ErpClink.Modules.Assets.Application.Accounting.Models;
using ErpClink.Modules.Assets.Domain;
using ErpClink.Modules.Assets.Domain.Accounting;
using ErpClink.Modules.Assets.Domain.Assets;
using ErpClink.Modules.Assets.Domain.History;
using ErpClink.Modules.Assets.Infrastructure.Common;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using ErpClink.Modules.Finance.Application.Integration;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Assets.Infrastructure.Accounting;

public sealed class AssetAccountingService : IAssetAccountingService
{
    private readonly AssetsDbContext _db;
    private readonly IDepreciationMethod _method;
    private readonly IAccountingPostingPort _accounting;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IValidator<CapitalizeAssetRequest> _capitalizeValidator;
    private readonly IValidator<PostDepreciationRequest> _postValidator;
    private readonly IValidator<DisposeAssetFinancialRequest> _disposeValidator;
    private readonly ILogger<AssetAccountingService> _logger;

    public AssetAccountingService(
        AssetsDbContext db,
        IDepreciationMethod method,
        IAccountingPostingPort accounting,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IValidator<CapitalizeAssetRequest> capitalizeValidator,
        IValidator<PostDepreciationRequest> postValidator,
        IValidator<DisposeAssetFinancialRequest> disposeValidator,
        ILogger<AssetAccountingService> logger)
    {
        _db = db;
        _method = method;
        _accounting = accounting;
        _org = org;
        _user = user;
        _clock = clock;
        _capitalizeValidator = capitalizeValidator;
        _postValidator = postValidator;
        _disposeValidator = disposeValidator;
        _logger = logger;
    }

    public async Task<AssetFinancialProfileDto> CapitalizeAsync(
        CapitalizeAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _capitalizeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var asset = await OrgAssets().SingleOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new AppException("assets.asset_not_found", "Asset was not found.", 404);

        if (asset.Status == AssetStatus.Retired)
            throw new AppException("assets.asset_retired", "Retired assets cannot be capitalized.", 409);

        AssetsPersistenceHelper.ApplyRowVersion(_db, asset, request.AssetRowVersion, a => a.RowVersion);

        var existing = await _db.AssetFinancialProfiles
            .SingleOrDefaultAsync(p => p.OrganizationId == _org.OrganizationId && p.AssetId == asset.Id, cancellationToken);
        if (existing is not null)
            throw new AppException("assets.already_capitalized", "Asset already has a financial profile.", 409);

        var acquisition = request.AcquisitionCost ?? asset.AcquisitionCost
            ?? throw new AppException("assets.acquisition_cost_required",
                "Acquisition cost is required for capitalization (do not invent legacy costs).", 400);

        var correlationId = Guid.NewGuid();
        var profile = AssetFinancialProfile.Capitalize(
            _org.OrganizationId,
            asset.BranchId,
            asset.Id,
            acquisition,
            request.CapitalizedCost,
            request.ResidualValue,
            request.CapitalizationDate,
            request.DepreciationStartDate,
            request.UsefulLifeMonths,
            _method.Method,
            _user.UserId,
            _clock.UtcNow);

        _db.AssetFinancialProfiles.Add(profile);

        var schedule = StraightLineDepreciationMethod.BuildSchedule(
            profile.CapitalizedCost,
            profile.ResidualValue,
            profile.UsefulLifeMonths,
            profile.DepreciationStartDate);

        var index = 0;
        foreach (var (periodStart, planned) in schedule)
        {
            _db.AssetDepreciationScheduleLines.Add(AssetDepreciationScheduleLine.CreatePlanned(
                _org.OrganizationId, asset.BranchId, asset.Id, profile.Id, index++, periodStart, planned));
        }

        var capTx = AssetFinancialTransaction.Create(
            _org.OrganizationId,
            asset.BranchId,
            asset.Id,
            profile.Id,
            AssetFinancialEventType.Capitalization,
            request.CapitalizationDate,
            profile.CapitalizedCost,
            null,
            profile.NetBookValue,
            0m,
            "Capitalization",
            profile.Id,
            $"cap:{asset.Id:N}:v{profile.CalculationVersion}",
            correlationId,
            "Asset capitalized",
            _user.UserId,
            _clock.UtcNow);
        _db.AssetFinancialTransactions.Add(capTx);

        _db.AssetHistory.Add(AssetHistoryEntry.Record(
            asset.Id, _org.OrganizationId, "Capitalized",
            "Asset financial profile capitalized",
            _clock.UtcNow, _user.UserId,
            null, profile.CapitalizedCost.ToString("0.00"), null));

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            await RequestAccountingAsync(
                "AssetFinancialProfile",
                profile.Id.ToString("N"),
                "AssetCapitalized",
                capTx.IdempotencyKey,
                correlationId,
                $"Capitalized asset {asset.AssetNumber}",
                cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return MapProfile(profile);
    }

    public async Task<DepreciationTransactionDto> PostDepreciationAsync(
        PostDepreciationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _postValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var asset = await OrgAssets().AsNoTracking().SingleOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new AppException("assets.asset_not_found", "Asset was not found.", 404);

        if (asset.Status == AssetStatus.Retired)
            throw new AppException("assets.asset_retired", "Retired assets cannot be depreciated.", 409);

        var profile = await _db.AssetFinancialProfiles
            .SingleOrDefaultAsync(p => p.OrganizationId == _org.OrganizationId && p.AssetId == request.AssetId, cancellationToken)
            ?? throw new AppException("assets.financial_incomplete",
                "Asset has no financial profile. Capitalize before depreciating; legacy costs are not fabricated.", 409);

        AssetsPersistenceHelper.ApplyRowVersion(_db, profile, request.FinancialProfileRowVersion, p => p.RowVersion);

        if (!string.IsNullOrWhiteSpace(request.PeriodKey))
        {
            var existingForPeriod = await _db.AssetDepreciationTransactions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    t => t.OrganizationId == _org.OrganizationId
                         && t.AssetId == request.AssetId
                         && t.PeriodKey == request.PeriodKey.Trim(),
                    cancellationToken);
            if (existingForPeriod is not null)
                return MapDepTx(existingForPeriod);
        }

        try
        {
            profile.EnsureCanDepreciate();
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("assets.cannot_depreciate", ex.Message, 409);
        }

        var nextLine = await _db.AssetDepreciationScheduleLines
            .Where(l => l.OrganizationId == _org.OrganizationId
                        && l.AssetId == request.AssetId
                        && l.Status == DepreciationScheduleLineStatus.Planned)
            .OrderBy(l => l.PeriodIndex)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppException("assets.schedule_exhausted", "No planned depreciation periods remain.", 409);

        if (!string.IsNullOrWhiteSpace(request.PeriodKey) &&
            !string.Equals(request.PeriodKey, nextLine.PeriodKey, StringComparison.Ordinal))
            throw new AppException("assets.period_mismatch",
                $"Next planned period is {nextLine.PeriodKey}.", 409);

        var periodKey = nextLine.PeriodKey;
        var idempotencyKey = $"dep:{request.AssetId:N}:{periodKey}:v{profile.CalculationVersion}";

        var existingTx = await _db.AssetDepreciationTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                t => t.OrganizationId == _org.OrganizationId && t.IdempotencyKey == idempotencyKey,
                cancellationToken);
        if (existingTx is not null)
            return MapDepTx(existingTx);

        var postedCount = await _db.AssetDepreciationTransactions
            .CountAsync(t => t.OrganizationId == _org.OrganizationId && t.AssetId == request.AssetId, cancellationToken);

        var calc = _method.CalculatePeriod(new PeriodDepreciationRequest(
            profile.CapitalizedCost,
            profile.ResidualValue,
            profile.AccumulatedDepreciation,
            profile.UsefulLifeMonths,
            nextLine.PeriodIndex,
            postedCount,
            nextLine.PlannedAmount));

        if (calc.DepreciationAmount <= 0)
            throw new AppException("assets.zero_depreciation", "Calculated depreciation is zero.", 409);

        var openingNbv = profile.NetBookValue;
        var correlationId = Guid.NewGuid();
        var transactionDate = request.TransactionDate ?? nextLine.PeriodStartDate.AddMonths(1).AddDays(-1);

        var depTx = AssetDepreciationTransaction.Create(
            _org.OrganizationId,
            profile.BranchId,
            profile.AssetId,
            profile.Id,
            nextLine.Id,
            periodKey,
            nextLine.PeriodStartDate,
            transactionDate,
            openingNbv,
            calc.DepreciationAmount,
            Money.Round(profile.AccumulatedDepreciation + calc.DepreciationAmount),
            Money.Round(openingNbv - calc.DepreciationAmount),
            profile.DepreciationMethod,
            profile.CalculationVersion,
            idempotencyKey,
            correlationId,
            $"Depreciation {periodKey}",
            _user.UserId,
            _clock.UtcNow);

        try
        {
            profile.ApplyDepreciation(calc.DepreciationAmount, nextLine.PeriodStartDate, _user.UserId, _clock.UtcNow);
            nextLine.MarkPosted(depTx.Id, calc.DepreciationAmount);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("assets.depreciation_failed", ex.Message, 409);
        }

        _db.AssetDepreciationTransactions.Add(depTx);

        var hist = AssetFinancialTransaction.Create(
            _org.OrganizationId,
            profile.BranchId,
            profile.AssetId,
            profile.Id,
            AssetFinancialEventType.Depreciation,
            transactionDate,
            calc.DepreciationAmount,
            openingNbv,
            profile.NetBookValue,
            profile.AccumulatedDepreciation,
            "DepreciationTransaction",
            depTx.Id,
            idempotencyKey,
            correlationId,
            depTx.Description,
            _user.UserId,
            _clock.UtcNow);
        _db.AssetFinancialTransactions.Add(hist);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            await RequestAccountingAsync(
                "AssetDepreciationTransaction",
                depTx.Id.ToString("N"),
                "AssetDepreciationPosted",
                idempotencyKey,
                correlationId,
                $"Depreciation {asset.AssetNumber} {periodKey}",
                cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (AssetsPersistenceHelper.IsUniqueViolation(ex))
        {
            await tx.RollbackAsync(cancellationToken);
            var winner = await _db.AssetDepreciationTransactions.AsNoTracking()
                .SingleAsync(t => t.OrganizationId == _org.OrganizationId && t.IdempotencyKey == idempotencyKey, cancellationToken);
            return MapDepTx(winner);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return MapDepTx(depTx);
    }

    public async Task<AssetFinancialProfileDto> DisposeFinancialAsync(
        DisposeAssetFinancialRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _disposeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("assets.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var asset = await OrgAssets().SingleOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new AppException("assets.asset_not_found", "Asset was not found.", 404);

        var profile = await _db.AssetFinancialProfiles
            .SingleOrDefaultAsync(p => p.OrganizationId == _org.OrganizationId && p.AssetId == request.AssetId, cancellationToken)
            ?? throw new AppException("assets.financial_incomplete",
                "Asset has no financial profile; disposal financial facts require capitalization first.", 409);

        AssetsPersistenceHelper.ApplyRowVersion(_db, profile, request.FinancialProfileRowVersion, p => p.RowVersion);

        var openingNbv = profile.NetBookValue;
        var correlationId = Guid.NewGuid();
        var idempotencyKey = $"disp:{request.AssetId:N}:{request.DisposalDate:yyyyMMdd}";

        try
        {
            profile.MarkDisposed(request.DisposalDate, request.Proceeds, request.Notes, _user.UserId, _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("assets.dispose_failed", ex.Message, 409);
        }

        var amount = profile.DisposalProceeds ?? 0m;
        var finTx = AssetFinancialTransaction.Create(
            _org.OrganizationId,
            profile.BranchId,
            profile.AssetId,
            profile.Id,
            AssetFinancialEventType.Disposal,
            request.DisposalDate,
            amount,
            openingNbv,
            profile.NetBookValue,
            profile.AccumulatedDepreciation,
            "FinancialDisposal",
            profile.Id,
            idempotencyKey,
            correlationId,
            request.Notes,
            _user.UserId,
            _clock.UtcNow);
        _db.AssetFinancialTransactions.Add(finTx);

        if (asset.Status != AssetStatus.Retired)
        {
            asset.Retire(request.Notes ?? "Financial disposal", _user.UserId, _clock.UtcNow);
            _db.AssetHistory.Add(AssetHistoryEntry.Record(
                asset.Id, _org.OrganizationId, "Retired",
                "Asset retired via financial disposal",
                _clock.UtcNow, _user.UserId, null, null, request.Notes));
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await AssetsPersistenceHelper.SaveChangesAsync(_db, "assets.concurrency_conflict", cancellationToken);
            await RequestAccountingAsync(
                "AssetFinancialProfile",
                profile.Id.ToString("N"),
                "AssetDisposed",
                idempotencyKey,
                correlationId,
                $"Disposed asset {asset.AssetNumber}",
                cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return MapProfile(profile);
    }

    public async Task<AssetFinancialProfileDto?> GetProfileAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.AssetFinancialProfiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.OrganizationId == _org.OrganizationId && p.AssetId == assetId, cancellationToken);
        return profile is null ? null : MapProfile(profile);
    }

    public async Task<PagedAssetAccountingResult> SearchAsync(
        SearchAssetAccountingRequest request,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query =
            from a in OrgAssets().AsNoTracking()
            join p in _db.AssetFinancialProfiles.AsNoTracking().Where(x => x.OrganizationId == _org.OrganizationId)
                on a.Id equals p.AssetId into gj
            from p in gj.DefaultIfEmpty()
            select new { a, p };

        if (request.AssetId.HasValue)
            query = query.Where(x => x.a.Id == request.AssetId);
        if (request.CategoryId.HasValue)
            query = query.Where(x => x.a.AssetCategoryId == request.CategoryId);
        if (request.LocationId.HasValue)
            query = query.Where(x => x.a.AssetLocationId == request.LocationId);
        if (!string.IsNullOrWhiteSpace(request.OperationalStatus) &&
            Enum.TryParse<AssetStatus>(request.OperationalStatus, true, out var op))
            query = query.Where(x => x.a.Status == op);
        if (!string.IsNullOrWhiteSpace(request.FinancialStatus) &&
            Enum.TryParse<AssetFinancialStatus>(request.FinancialStatus, true, out var fs))
            query = query.Where(x => x.p != null && x.p.Status == fs);

        var total = await query.CountAsync(cancellationToken);
        var page = await query
            .OrderBy(x => x.a.AssetNumber)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .Select(x => new AssetAccountingListItemDto(
                x.a.Id,
                x.a.AssetNumber,
                x.a.Name,
                x.p != null ? x.p.Id : null,
                x.p != null ? x.p.Status.ToString() : AssetFinancialStatus.Incomplete.ToString(),
                x.p != null ? x.p.CapitalizedCost : null,
                x.p != null ? x.p.AccumulatedDepreciation : null,
                x.p != null ? x.p.NetBookValue : null,
                x.a.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new PagedAssetAccountingResult(page, total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<PagedDepreciationScheduleResult> GetScheduleAsync(
        Guid assetId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        await EnsureAssetExistsAsync(assetId, cancellationToken);
        var paging = new PagedRequest(page, pageSize);
        var query = _db.AssetDepreciationScheduleLines.AsNoTracking()
            .Where(l => l.OrganizationId == _org.OrganizationId && l.AssetId == assetId)
            .OrderBy(l => l.PeriodIndex);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize)
            .Select(l => new DepreciationScheduleLineDto(
                l.Id, l.PeriodIndex, l.PeriodKey, l.PeriodStartDate, l.PlannedAmount, l.Status.ToString(), l.PostedTransactionId))
            .ToListAsync(cancellationToken);
        return new PagedDepreciationScheduleResult(items, total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<PagedDepreciationTransactionsResult> SearchDepreciationAsync(
        SearchDepreciationTransactionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = _db.AssetDepreciationTransactions.AsNoTracking()
            .Where(t => t.OrganizationId == _org.OrganizationId);
        if (request.AssetId.HasValue)
            query = query.Where(t => t.AssetId == request.AssetId);
        if (request.FromPeriod.HasValue)
            query = query.Where(t => t.PeriodStartDate >= request.FromPeriod);
        if (request.ToPeriod.HasValue)
            query = query.Where(t => t.PeriodStartDate <= request.ToPeriod);

        var total = await query.CountAsync(cancellationToken);
        var totalDep = await query.SumAsync(t => (decimal?)t.DepreciationAmount, cancellationToken) ?? 0m;
        var items = await query.OrderByDescending(t => t.PeriodStartDate).ThenByDescending(t => t.CreatedAtUtc)
            .Skip(paging.Skip).Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);
        return new PagedDepreciationTransactionsResult(
            items.Select(MapDepTx).ToList(), totalDep, total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<PagedAssetFinancialHistoryResult> GetFinancialHistoryAsync(
        Guid assetId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        await EnsureAssetExistsAsync(assetId, cancellationToken);
        var paging = new PagedRequest(page, pageSize);
        var query = _db.AssetFinancialTransactions.AsNoTracking()
            .Where(t => t.OrganizationId == _org.OrganizationId && t.AssetId == assetId)
            .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize)
            .Select(t => new AssetFinancialHistoryLineDto(
                t.Id, t.EventType.ToString(), t.TransactionDate, t.Amount,
                t.OpeningNetBookValue, t.ClosingNetBookValue, t.AccumulatedDepreciation,
                t.SourceType, t.SourceId, t.CorrelationId, t.Description, t.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return new PagedAssetFinancialHistoryResult(items, total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<AssetValuationResult> GetValuationAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(page, pageSize);
        var query =
            from p in _db.AssetFinancialProfiles.AsNoTracking()
            join a in OrgAssets().AsNoTracking() on p.AssetId equals a.Id
            where p.OrganizationId == _org.OrganizationId
                  && p.Status != AssetFinancialStatus.Incomplete
                  && p.Status != AssetFinancialStatus.Disposed
            select new { p, a };

        var totals = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cap = g.Sum(x => x.p.CapitalizedCost),
                Acc = g.Sum(x => x.p.AccumulatedDepreciation),
                Nbv = g.Sum(x => x.p.NetBookValue)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.a.AssetNumber)
            .Skip(paging.Skip).Take(paging.NormalizedPageSize)
            .Select(x => new AssetValuationLineDto(
                x.a.Id, x.a.AssetNumber, x.a.Name,
                x.p.CapitalizedCost, x.p.AccumulatedDepreciation, x.p.NetBookValue, x.p.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new AssetValuationResult(
            totals?.Cap ?? 0m,
            totals?.Acc ?? 0m,
            totals?.Nbv ?? 0m,
            items,
            total,
            paging.NormalizedPage,
            paging.NormalizedPageSize);
    }

    public async Task<PagedAssetDisposalsResult> SearchDisposalsAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(page, pageSize);
        var query =
            from p in _db.AssetFinancialProfiles.AsNoTracking()
            join a in OrgAssets().AsNoTracking() on p.AssetId equals a.Id
            where p.OrganizationId == _org.OrganizationId && p.Status == AssetFinancialStatus.Disposed
            select new { p, a };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.p.DisposedDate)
            .Skip(paging.Skip).Take(paging.NormalizedPageSize)
            .Select(x => new AssetDisposalDto(
                x.a.Id, x.a.AssetNumber, x.a.Name, x.p.DisposedDate!.Value,
                x.p.CapitalizedCost, x.p.AccumulatedDepreciation, x.p.NetBookValue,
                x.p.DisposalProceeds, x.p.DisposalGainLoss, x.p.DisposalNotes))
            .ToListAsync(cancellationToken);
        return new PagedAssetDisposalsResult(items, total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    private async Task RequestAccountingAsync(
        string sourceType,
        string sourceId,
        string eventType,
        string idempotencyKey,
        Guid correlationId,
        string? description,
        CancellationToken cancellationToken)
    {
        try
        {
            await _accounting.RequestPostingAsync(
                new AccountingPostingRequest(
                    _org.OrganizationId,
                    _org.BranchId,
                    AccountingSourceModules.Assets,
                    sourceType,
                    sourceId,
                    eventType,
                    _clock.UtcNow,
                    correlationId,
                    idempotencyKey,
                    description),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Local Assets transaction remains source of truth; STEP 18 has no durable outbox yet.
            _logger.LogWarning(ex,
                "Accounting integration request failed for {EventType} {IdempotencyKey}", eventType, idempotencyKey);
        }
    }

    private async Task EnsureAssetExistsAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var exists = await OrgAssets().AsNoTracking().AnyAsync(a => a.Id == assetId, cancellationToken);
        if (!exists)
            throw new AppException("assets.asset_not_found", "Asset was not found.", 404);
    }

    private IQueryable<Asset> OrgAssets() =>
        _db.Assets.Where(a => a.OrganizationId == _org.OrganizationId && a.BranchId == _org.BranchId);

    private static AssetFinancialProfileDto MapProfile(AssetFinancialProfile p) =>
        new(
            p.Id, p.AssetId, p.BranchId, p.Status.ToString(),
            p.AcquisitionCost, p.CapitalizedCost, p.ResidualValue, p.DepreciableBase,
            p.CapitalizationDate, p.DepreciationStartDate, p.UsefulLifeMonths,
            p.DepreciationMethod.ToString(), p.CalculationVersion,
            p.AccumulatedDepreciation, p.NetBookValue, p.RemainingDepreciableAmount,
            p.LastDepreciationPeriod, p.DisposedDate, p.DisposalProceeds, p.DisposalGainLoss,
            p.DisposalNotes, p.RowVersion);

    private static DepreciationTransactionDto MapDepTx(AssetDepreciationTransaction t) =>
        new(
            t.Id, t.AssetId, t.PeriodKey, t.PeriodStartDate, t.TransactionDate,
            t.OpeningNetBookValue, t.DepreciationAmount, t.ClosingAccumulatedDepreciation,
            t.ClosingNetBookValue, t.Method.ToString(), t.CalculationVersion,
            t.CorrelationId, t.CreatedAtUtc);
}
