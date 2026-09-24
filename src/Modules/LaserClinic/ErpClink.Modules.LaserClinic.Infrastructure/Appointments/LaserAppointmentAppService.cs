using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.LaserClinic.Application.Appointments;
using ErpClink.Modules.LaserClinic.Application.Customers;
using ErpClink.Modules.LaserClinic.Application.Scheduling;
using ErpClink.Modules.LaserClinic.Domain.Appointments;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using ErpClink.Modules.LaserClinic.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Appointments;

public sealed class LaserAppointmentAppService : ILaserAppointmentAppService
{
    private readonly LaserClinicDbContext _db;
    private readonly ClinicSettingsAppService _settings;

    public LaserAppointmentAppService(LaserClinicDbContext db, ClinicSettingsAppService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<IReadOnlyList<LaserAppointmentDto>> ListAsync(
        DateOnly? date,
        Guid? customerId,
        CancellationToken cancellationToken = default)
    {
        var q = BaseQuery();
        if (date.HasValue)
            q = q.Where(a => a.AppointmentDate == date.Value);
        if (customerId.HasValue)
            q = q.Where(a => a.CustomerId == customerId.Value);

        var items = await q
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .Take(500)
            .ToListAsync(cancellationToken);

        return await MapManyAsync(items, cancellationToken);
    }

    public async Task<LaserAppointmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
            return null;
        var list = await MapManyAsync([entity], cancellationToken);
        return list[0];
    }

    public async Task<LaserAppointmentDto> CreateAsync(
        CreateLaserAppointmentRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (request.CustomerId == Guid.Empty)
            throw new AppException("laser.appointment.customer_required", "يجب اختيار عميل.", 400);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.IsActive, cancellationToken)
            ?? throw new AppException("laser.customer.not_found", "العميل غير موجود.", 404);

        var plan = await BuildValidatedPlanAsync(
            request.AppointmentDate,
            request.StartTime,
            request.LaserServiceIds,
            cancellationToken,
            serviceDurationOverrides: ToOverrideMap(request.ServiceDurationOverrides));

        return await PersistAppointmentAsync(customer.Id, plan, request.Notes, userId, cancellationToken);
    }

    public async Task<LaserAppointmentDto> CreateBookingWithCustomerAsync(
        CreateBookingWithCustomerRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (request.LaserServiceIds is null || request.LaserServiceIds.Count == 0)
            throw new AppException("laser.appointment.service_required", "يجب اختيار خدمة واحدة على الأقل.", 400);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            Guid customerId;
            if (request.ExistingCustomerId is Guid existingId && existingId != Guid.Empty)
            {
                var existing = await _db.Customers.FirstOrDefaultAsync(c => c.Id == existingId && c.IsActive, cancellationToken)
                    ?? throw new AppException("laser.customer.not_found", "العميل غير موجود.", 404);
                customerId = existing.Id;
            }
            else
            {
                if (request.Customer is null)
                    throw new AppException("laser.appointment.customer_required", "يجب اختيار عميل.", 400);

                var phone = Domain.Customers.Customer.NormalizeEgyptianMobile(request.Customer.PhoneNumber)
                    ?? throw new AppException(
                        "laser.customer.phone_invalid",
                        "رقم الموبايل لازم يكون مصري من 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015.",
                        400);
                var duplicate = await _db.Customers.AnyAsync(c => c.IsActive && c.PhoneNumber == phone, cancellationToken);
                if (duplicate)
                    throw new AppException("laser.customer.phone_exists", "رقم الهاتف مستخدم بالفعل.", 409);

                var customer = Domain.Customers.Customer.Create(
                    request.Customer.FullName,
                    phone,
                    request.Customer.Age,
                    request.Customer.Notes,
                    userId,
                    DateTime.UtcNow);
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync(cancellationToken);
                customerId = customer.Id;
            }

            var plan = await BuildValidatedPlanAsync(
                request.AppointmentDate,
                request.StartTime,
                request.LaserServiceIds,
                cancellationToken,
                serviceDurationOverrides: ToOverrideMap(request.ServiceDurationOverrides));

            var created = await PersistAppointmentAsync(
                customerId,
                plan,
                request.AppointmentNotes,
                userId,
                cancellationToken);

            await tx.CommitAsync(cancellationToken);
            return created;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SlotCheckResultDto> CheckSlotAsync(SlotCheckQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var durationOverrides = query.ServiceDurationOverrides is null
                ? null
                : query.ServiceDurationOverrides.ToDictionary(
                    kv => kv.Key,
                    kv => new LineOverride(kv.Value, null));
            var plan = await BuildValidatedPlanAsync(
                query.Date,
                query.StartTime,
                query.LaserServiceIds,
                cancellationToken,
                serviceDurationOverrides: durationOverrides);

            return new SlotCheckResultDto(
                true,
                plan.ClinicalDurationMinutes,
                plan.ClinicalEnd,
                "الموعد متاح");
        }
        catch (AppException ex) when (ex.StatusCode is 400 or 409)
        {
            var duration = 0;
            TimeOnly end = query.StartTime;
            try
            {
                var services = await LoadActiveServicesAsync(query.LaserServiceIds, cancellationToken);
                duration = DurationCalculator.SumRecommended(services);
                end = query.StartTime.AddMinutes(duration);
            }
            catch
            {
                // keep defaults
            }

            return new SlotCheckResultDto(false, duration, end, ex.Message);
        }
    }

    public async Task<LaserAppointmentDto> UpdateStatusAsync(
        Guid id,
        LaserAppointmentStatus status,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Appointments.Include(a => a.Services).FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new AppException("laser.appointment.not_found", "الموعد غير موجود.", 404);

        if (status == LaserAppointmentStatus.Attended && entity.Status != LaserAppointmentStatus.Attended)
            EnsureAttendedOnAppointmentDay(entity);

        entity.ChangeStatus(status, userId, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(id, cancellationToken))!;
    }

    public async Task<LaserAppointmentDto> UpdateAsync(
        Guid id,
        UpdateLaserAppointmentRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Appointments
            .Include(a => a.Services)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new AppException("laser.appointment.not_found", "الموعد غير موجود.", 404);

        if (entity.Status is LaserAppointmentStatus.Cancelled)
            throw new AppException("laser.appointment.not_editable", "لا يمكن تعديل موعد ملغى.", 400);

        var previousPulses = entity.Services
            .Where(s => s.PulsesConsumed is > 0)
            .Select(s => (s.LaserServiceId, PulsesConsumed: s.PulsesConsumed!.Value))
            .ToList();

        var plan = await BuildValidatedPlanAsync(
            request.AppointmentDate,
            request.StartTime,
            request.LaserServiceIds,
            cancellationToken,
            excludeAppointmentId: id,
            serviceDurationOverrides: ToOverrideMap(request.ServiceDurationOverrides));

        var catalogIds = plan.Lines.Select(l => l.LaserServiceId).Distinct().ToList();
        var catalog = await _db.LaserServices.Where(s => catalogIds.Contains(s.Id)).ToListAsync(cancellationToken);
        await ApplyPulseBalanceAsync(
            entity.CustomerId,
            plan.Lines,
            catalog,
            userId,
            cancellationToken,
            previousPulses);

        await _db.AppointmentServices
            .Where(s => s.AppointmentId == id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var line in entity.Services.ToList())
            _db.Entry(line).State = EntityState.Detached;

        foreach (var line in plan.Lines)
        {
            _db.AppointmentServices.Add(
                AppointmentServiceLine.Create(id, line.LaserServiceId, line.DurationMinutes, line.PulsesConsumed));
        }

        entity.Reschedule(plan.Date, plan.StartTime, plan.ClinicalDurationMinutes, userId, DateTime.UtcNow);
        entity.UpdateNotes(request.Notes, userId, DateTime.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, string? userId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new AppException("laser.appointment.not_found", "الموعد غير موجود.", 404);

        entity.ChangeStatus(LaserAppointmentStatus.Cancelled, userId, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AvailabilityResultDto> GetAvailabilityAsync(
        AvailabilityQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.LaserServiceIds is null || query.LaserServiceIds.Count == 0)
            throw new AppException("laser.appointment.service_required", "يجب اختيار خدمة واحدة على الأقل.", 400);

        var serviceIds = query.LaserServiceIds.Distinct().ToList();
        var services = await _db.LaserServices
            .Where(s => serviceIds.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);
        if (services.Count != serviceIds.Count)
            throw new AppException("laser.service.not_found", "إحدى الخدمات غير موجودة أو غير نشطة.", 400);

        var durationOverrides = query.ServiceDurationOverrides is null
            ? null
            : query.ServiceDurationOverrides.ToDictionary(
                kv => kv.Key,
                kv => new LineOverride(kv.Value, null));
        var lines = ResolveServiceLines(services, durationOverrides, requirePulseCounts: false);
        var clinical = lines.Sum(l => l.DurationMinutes);
        var settings = await _settings.EnsureAsync(cancellationToken);
        var occupied = await GetOccupiedAsync(
            query.Date,
            query.ExcludeAppointmentId,
            settings.DefaultBufferMinutes,
            cancellationToken);
        var nowEgypt = GetEgyptNow();
        var todayEgypt = DateOnly.FromDateTime(nowEgypt);
        var nowLocal = TimeOnly.FromDateTime(nowEgypt);

        var timeline = AvailabilityCalculator.GetDayTimeline(
            settings.OpeningTime,
            settings.ClosingTime,
            settings.AppointmentSlotIntervalMinutes,
            clinical,
            settings.DefaultBufferMinutes,
            occupied,
            nowLocal,
            query.Date,
            todayEgypt);

        return new AvailabilityResultDto(
            query.Date,
            clinical,
            settings.DefaultBufferMinutes,
            DurationCalculator.EffectiveBlockedMinutes(clinical, settings.DefaultBufferMinutes),
            settings.OpeningTime,
            settings.ClosingTime,
            settings.AppointmentSlotIntervalMinutes,
            timeline.Select(s => new AvailabilitySlotDto(
                s.StartTime,
                s.EndTime,
                s.DurationMinutes,
                s.Status switch
                {
                    DaySlotStatus.Available => AvailabilitySlotStatus.Available,
                    DaySlotStatus.Booked => AvailabilitySlotStatus.Booked,
                    _ => AvailabilitySlotStatus.Unavailable
                },
                s.AppointmentId,
                s.CustomerName,
                s.ServiceNames,
                s.BookedDurationMinutes)).ToList());
    }

    public async Task<CustomerHistoryDto> GetCustomerHistoryAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new AppException("laser.customer.not_found", "العميل غير موجود.", 404);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var all = await MapManyAsync(
            await BaseQuery().Where(a => a.CustomerId == customerId).OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ToListAsync(cancellationToken),
            cancellationToken);

        var upcoming = all
            .Where(a => a.AppointmentDate > today
                        || (a.AppointmentDate == today && a.Status is LaserAppointmentStatus.Pending or LaserAppointmentStatus.Confirmed))
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            .ToList();
        var previous = all.Where(a => !upcoming.Any(u => u.Id == a.Id)).ToList();

        return new CustomerHistoryDto(
            customer.Id,
            customer.FullName,
            customer.PhoneNumber,
            upcoming,
            previous,
            await MapPulseBalanceAsync(customer, cancellationToken));
    }

    public async Task<SessionDetailDto?> GetSessionDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await GetByIdAsync(id, cancellationToken);
        if (appointment is null)
            return null;

        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == appointment.CustomerId, cancellationToken)
            ?? throw new AppException("laser.customer.not_found", "العميل غير موجود.", 404);

        var totalPaid = await _db.Appointments.AsNoTracking()
            .Where(a => a.CustomerId == appointment.CustomerId && a.AmountPaid != null)
            .SumAsync(a => a.AmountPaid ?? 0m, cancellationToken);

        var balance = await MapPulseBalanceAsync(customer, cancellationToken);
        var recordedOnSession = appointment.Services.Sum(s => s.PulsesConsumed ?? 0);
        var available = (balance.Remaining ?? 0) + recordedOnSession;

        return new SessionDetailDto(appointment, balance, totalPaid, available);
    }

    public async Task<SessionDetailDto> RecordSessionAsync(
        Guid id,
        RecordSessionRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (request.DurationMinutes is < 1 or > 480)
            throw new AppException("laser.appointment.invalid_duration", "مدة الجلسة يجب أن تكون بين 1 و 480 دقيقة.", 400);
        if (request.AmountPaid is < 0)
            throw new AppException("laser.appointment.invalid_amount", "المبلغ المدفوع غير صحيح.", 400);

        var entity = await _db.Appointments
            .Include(a => a.Services)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new AppException("laser.appointment.not_found", "الموعد غير موجود.", 404);

        if (entity.Status is LaserAppointmentStatus.Cancelled)
            throw new AppException("laser.appointment.not_editable", "لا يمكن تعديل موعد ملغى.", 400);

        var previousPulses = entity.Services
            .Where(s => s.PulsesConsumed is > 0)
            .Select(s => (s.LaserServiceId, PulsesConsumed: s.PulsesConsumed!.Value))
            .ToList();

        var serviceIds = entity.Services.Select(s => s.LaserServiceId).Distinct().ToList();
        var catalog = await LoadActiveServicesAsync(serviceIds, cancellationToken);
        var byId = catalog.ToDictionary(s => s.Id);
        var hasPulsePackage = catalog.Any(s => DurationCalculator.ParsePulsePackageSize(s.Name) is not null);
        if (hasPulsePackage)
        {
            if (request.PulsesConsumed is null or < 1)
                throw new AppException("laser.appointment.pulses_required", "أدخلي عدد النبضات المستهلكة في الجلسة.", 400);
            if (request.PulsesConsumed > 5000)
                throw new AppException("laser.appointment.invalid_pulses", "عدد النبضات غير صحيح.", 400);
        }

        var oldTotal = Math.Max(1, entity.Services.Sum(s => s.DurationMinutes));
        var rebuilt = new List<(Guid LaserServiceId, int DurationMinutes, int? PulsesConsumed)>();
        var allocated = 0;
        var ordered = entity.Services.ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var line = ordered[i];
            int duration;
            if (i == ordered.Count - 1)
                duration = Math.Max(1, request.DurationMinutes - allocated);
            else
            {
                duration = Math.Max(1, (int)Math.Round(line.DurationMinutes * (double)request.DurationMinutes / oldTotal));
                allocated += duration;
            }

            byId.TryGetValue(line.LaserServiceId, out var service);
            var isPulse = service is not null && DurationCalculator.ParsePulsePackageSize(service.Name) is not null;
            var pulses = isPulse ? request.PulsesConsumed : null;
            rebuilt.Add((line.LaserServiceId, duration, pulses));
        }

        var overrides = rebuilt
            .Select(l => new ServiceDurationOverrideDto(l.LaserServiceId, l.DurationMinutes, l.PulsesConsumed))
            .ToList();

        var plan = await BuildValidatedPlanAsync(
            entity.AppointmentDate,
            entity.StartTime,
            serviceIds,
            cancellationToken,
            excludeAppointmentId: id,
            serviceDurationOverrides: ToOverrideMap(overrides),
            allowExistingSlot: true);

        await ApplyPulseBalanceAsync(
            entity.CustomerId,
            plan.Lines,
            catalog,
            userId,
            cancellationToken,
            previousPulses);

        await _db.AppointmentServices
            .Where(s => s.AppointmentId == id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var line in entity.Services.ToList())
            _db.Entry(line).State = EntityState.Detached;

        foreach (var line in plan.Lines)
        {
            _db.AppointmentServices.Add(
                AppointmentServiceLine.Create(id, line.LaserServiceId, line.DurationMinutes, line.PulsesConsumed));
        }

        entity.Reschedule(plan.Date, plan.StartTime, plan.ClinicalDurationMinutes, userId, DateTime.UtcNow);
        entity.SetAmountPaid(request.AmountPaid, userId, DateTime.UtcNow);
        if (request.PackagePrice is >= 0)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == entity.CustomerId, cancellationToken)
                ?? throw new AppException("laser.customer.not_found", "العميل غير موجود.", 404);
            if (customer.PackagePriceTotal is null)
                customer.SetPackagePrice(request.PackagePrice.Value, userId, DateTime.UtcNow);
        }

        if (request.MarkAttended && entity.Status is not LaserAppointmentStatus.Attended && IsAttendedWindow(entity))
            entity.ChangeStatus(LaserAppointmentStatus.Attended, userId, DateTime.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetSessionDetailAsync(id, cancellationToken))!;
    }

    private async Task<CustomerPulseBalanceDto> MapPulseBalanceAsync(
        Domain.Customers.Customer c,
        CancellationToken cancellationToken)
    {
        var total = c.PulsePackageTotal;
        var remaining = c.PulsePackageRemaining;
        var consumed = total is int t && remaining is int r ? Math.Max(0, t - r) : 0;

        var amountPaid = await _db.Appointments.AsNoTracking()
            .Where(a => a.CustomerId == c.Id && a.Status != LaserAppointmentStatus.Cancelled)
            .SumAsync(a => a.AmountPaid ?? 0m, cancellationToken);

        var durationUsed = await _db.Appointments.AsNoTracking()
            .Where(a => a.CustomerId == c.Id && a.Status != LaserAppointmentStatus.Cancelled)
            .Where(a => a.AmountPaid != null || a.Services.Any(s => s.PulsesConsumed > 0))
            .SumAsync(a => (int?)a.DurationMinutes ?? 0, cancellationToken);

        var priceTotal = c.PackagePriceTotal ?? await CatalogPriceTotalAsync(c.Id, cancellationToken);
        var remainingAmount = priceTotal is decimal p ? Math.Max(0, p - amountPaid) : 0m;
        var durationTotal = c.PackageDurationTotalMinutes;
        var remainingDuration = durationTotal is int d ? Math.Max(0, d - durationUsed) : 0;

        return new CustomerPulseBalanceDto(
            total,
            remaining,
            consumed,
            priceTotal,
            amountPaid,
            remainingAmount,
            durationTotal,
            durationUsed,
            remainingDuration);
    }

    private async Task<decimal?> CatalogPriceTotalAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var prices = await (
            from line in _db.AppointmentServices.AsNoTracking()
            join appt in _db.Appointments.AsNoTracking() on line.AppointmentId equals appt.Id
            join svc in _db.LaserServices.AsNoTracking() on line.LaserServiceId equals svc.Id
            where appt.CustomerId == customerId && appt.Status != LaserAppointmentStatus.Cancelled
            select new { svc.Id, svc.Price }
        ).Distinct().ToListAsync(cancellationToken);

        if (prices.Count == 0)
            return null;
        return prices.Sum(p => p.Price);
    }

    private static CustomerPulseBalanceDto MapPulseBalance(Domain.Customers.Customer c)
    {
        // Sync fallback without money/time usage (prefer MapPulseBalanceAsync).
        var total = c.PulsePackageTotal;
        var remaining = c.PulsePackageRemaining;
        var consumed = total is int t && remaining is int r ? Math.Max(0, t - r) : 0;
        return new CustomerPulseBalanceDto(
            total,
            remaining,
            consumed,
            c.PackagePriceTotal,
            0,
            c.PackagePriceTotal ?? 0,
            c.PackageDurationTotalMinutes,
            0,
            c.PackageDurationTotalMinutes ?? 0);
    }

    private IQueryable<LaserAppointment> BaseQuery() =>
        _db.Appointments.AsNoTracking()
            .Include(a => a.Services)
            .Where(a => _db.Customers.Any(c => c.Id == a.CustomerId && c.IsActive));

    private sealed record LineOverride(int DurationMinutes, int? PulsesConsumed);

    private sealed record BookingPlan(
        DateOnly Date,
        TimeOnly StartTime,
        TimeOnly ClinicalEnd,
        int ClinicalDurationMinutes,
        IReadOnlyList<(Guid LaserServiceId, int DurationMinutes, int? PulsesConsumed)> Lines);

    private async Task<BookingPlan> BuildValidatedPlanAsync(
        DateOnly date,
        TimeOnly startTime,
        IReadOnlyList<Guid>? laserServiceIds,
        CancellationToken cancellationToken,
        Guid? excludeAppointmentId = null,
        IReadOnlyDictionary<Guid, LineOverride>? serviceDurationOverrides = null,
        bool allowExistingSlot = false)
    {
        var services = await LoadActiveServicesAsync(laserServiceIds, cancellationToken);
        // Duration is required for pulse services at booking; pulses are recorded later from the customer file.
        var lines = ResolveServiceLines(services, serviceDurationOverrides, requirePulseCounts: false);
        var clinicalDuration = lines.Sum(l => l.DurationMinutes);
        var settings = await _settings.EnsureAsync(cancellationToken);
        var blocked = DurationCalculator.EffectiveBlockedMinutes(clinicalDuration, settings.DefaultBufferMinutes);
        var clinicalEnd = startTime.AddMinutes(clinicalDuration);
        var blockedEnd = startTime.AddMinutes(blocked);

        if (startTime < settings.OpeningTime || clinicalEnd > settings.ClosingTime)
            throw new AppException("laser.appointment.outside_hours", "الموعد خارج مواعيد العمل.", 400);

        if (!allowExistingSlot)
        {
            var nowEgypt = GetEgyptNow();
            var todayEgypt = DateOnly.FromDateTime(nowEgypt);
            if (date < todayEgypt)
                throw new AppException("laser.appointment.past_date", "لا يمكن حجز موعد في تاريخ ماضٍ.", 400);
            if (date == todayEgypt && startTime <= TimeOnly.FromDateTime(nowEgypt))
                throw new AppException("laser.appointment.past_time", "لا يمكن حجز موعد في وقت ماضٍ.", 400);
        }

        var occupied = await GetOccupiedAsync(date, excludeAppointmentId, settings.DefaultBufferMinutes, cancellationToken);
        if (AvailabilityCalculator.Conflicts(startTime, blockedEnd, occupied))
            throw new AppException(
                "laser.appointment.conflict",
                "هذا الموعد تم حجزه بالفعل، يرجى اختيار موعد آخر.",
                409);

        return new BookingPlan(date, startTime, clinicalEnd, clinicalDuration, lines);
    }

    private static IReadOnlyDictionary<Guid, LineOverride>? ToOverrideMap(
        IReadOnlyList<ServiceDurationOverrideDto>? overrides)
    {
        if (overrides is null || overrides.Count == 0)
            return null;

        var map = new Dictionary<Guid, LineOverride>();
        foreach (var item in overrides)
        {
            if (item.ServiceId == Guid.Empty || item.DurationMinutes < 1)
                continue;
            map[item.ServiceId] = new LineOverride(item.DurationMinutes, item.PulsesConsumed);
        }

        return map.Count == 0 ? null : map;
    }

    private static List<(Guid LaserServiceId, int DurationMinutes, int? PulsesConsumed)> ResolveServiceLines(
        IReadOnlyList<Domain.Services.LaserService> services,
        IReadOnlyDictionary<Guid, LineOverride>? overrides,
        bool requirePulseCounts = true)
    {
        var lines = new List<(Guid LaserServiceId, int DurationMinutes, int? PulsesConsumed)>(services.Count);
        foreach (var service in services)
        {
            LineOverride? custom = null;
            if (overrides is not null && overrides.TryGetValue(service.Id, out var byId))
                custom = byId;

            if (custom is not null)
            {
                if (custom.DurationMinutes is < 1 or > 480)
                    throw new AppException(
                        "laser.appointment.invalid_duration",
                        "مدة النبضات يجب أن تكون بين 1 و 480 دقيقة.",
                        400);

                if (requirePulseCounts && DurationCalculator.RequiresManualDuration(service))
                {
                    if (custom.PulsesConsumed is null or < 1)
                        throw new AppException(
                            "laser.appointment.pulses_required",
                            $"أدخلي عدد النبضات المستهلكة لـ «{service.Name}».",
                            400);
                    if (custom.PulsesConsumed > 5000)
                        throw new AppException(
                            "laser.appointment.invalid_pulses",
                            "عدد النبضات غير صحيح.",
                            400);
                }

                lines.Add((service.Id, custom.DurationMinutes, custom.PulsesConsumed));
                continue;
            }

            if (DurationCalculator.RequiresManualDuration(service))
                throw new AppException(
                    "laser.appointment.pulse_duration_required",
                    $"أدخلي مدة الجلسة{(requirePulseCounts ? " وعدد النبضات" : "")} لـ «{service.Name}».",
                    400);

            lines.Add((service.Id, DurationCalculator.RecommendedMinutes(service), null));
        }

        return lines;
    }

    private async Task ApplyPulseBalanceAsync(
        Guid customerId,
        IReadOnlyList<(Guid LaserServiceId, int DurationMinutes, int? PulsesConsumed)> lines,
        IReadOnlyList<Domain.Services.LaserService> catalog,
        string? userId,
        CancellationToken cancellationToken,
        IReadOnlyList<(Guid LaserServiceId, int PulsesConsumed)>? previousConsumption = null)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new AppException("laser.customer.not_found", "العميل غير موجود.", 404);

        var utc = DateTime.UtcNow;
        if (previousConsumption is not null)
        {
            foreach (var prev in previousConsumption)
                customer.RestorePulses(prev.PulsesConsumed, userId, utc);
        }

        var byId = catalog.ToDictionary(s => s.Id);
        foreach (var line in lines)
        {
            if (!byId.TryGetValue(line.LaserServiceId, out var service))
                continue;
            var packageSize = DurationCalculator.ParsePulsePackageSize(service.Name);
            if (packageSize is null)
                continue;

            try
            {
                // Booking a 1000/5000 package opens/keeps the balance; consumption is optional until recorded after the session.
                customer.EnsurePulsePackage(
                    packageSize.Value,
                    userId,
                    utc,
                    packageDurationMinutes: line.DurationMinutes > 0 ? line.DurationMinutes : null);
                if (line.PulsesConsumed is null or < 1)
                    continue;
                customer.ConsumePulses(line.PulsesConsumed.Value, userId, utc);
            }
            catch (InvalidOperationException)
            {
                throw new AppException(
                    "laser.appointment.insufficient_pulses",
                    $"النبضات المتبقية غير كافية لـ «{service.Name}» (متبقي: {customer.PulsePackageRemaining ?? 0}).",
                    400);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new AppException("laser.appointment.invalid_pulses", ex.Message, 400);
            }
        }
    }

    private async Task<List<Domain.Services.LaserService>> LoadActiveServicesAsync(
        IReadOnlyList<Guid>? laserServiceIds,
        CancellationToken cancellationToken)
    {
        if (laserServiceIds is null || laserServiceIds.Count == 0)
            throw new AppException("laser.appointment.service_required", "يجب اختيار خدمة واحدة على الأقل.", 400);

        var serviceIds = laserServiceIds.Distinct().ToList();
        var services = await _db.LaserServices
            .Where(s => serviceIds.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);
        if (services.Count != serviceIds.Count)
            throw new AppException("laser.service.not_found", "إحدى الخدمات غير موجودة أو غير نشطة.", 400);

        return services;
    }

    private async Task<LaserAppointmentDto> PersistAppointmentAsync(
        Guid customerId,
        BookingPlan plan,
        string? notes,
        string? userId,
        CancellationToken cancellationToken)
    {
        var catalogIds = plan.Lines.Select(l => l.LaserServiceId).Distinct().ToList();
        var catalog = await _db.LaserServices.Where(s => catalogIds.Contains(s.Id)).ToListAsync(cancellationToken);
        await ApplyPulseBalanceAsync(customerId, plan.Lines, catalog, userId, cancellationToken);

        var appointment = LaserAppointment.Create(
            customerId,
            plan.Date,
            plan.StartTime,
            plan.ClinicalDurationMinutes,
            plan.Lines,
            notes,
            userId,
            DateTime.UtcNow);

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(appointment.Id, cancellationToken))!;
    }

    private async Task<IReadOnlyList<OccupiedInterval>> GetOccupiedAsync(
        DateOnly date,
        Guid? excludeId,
        int bufferMinutes,
        CancellationToken cancellationToken)
    {
        var q = _db.Appointments.AsNoTracking()
            .Include(a => a.Services)
            .Where(a => a.AppointmentDate == date
                        && (a.Status == LaserAppointmentStatus.Pending
                            || a.Status == LaserAppointmentStatus.Confirmed
                            || a.Status == LaserAppointmentStatus.Attended)
                        && _db.Customers.Any(c => c.Id == a.CustomerId && c.IsActive));
        if (excludeId.HasValue)
            q = q.Where(a => a.Id != excludeId.Value);

        var items = await q.ToListAsync(cancellationToken);
        if (items.Count == 0)
            return [];

        var customerIds = items.Select(a => a.CustomerId).Distinct().ToList();
        var customers = await _db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var serviceIds = items.SelectMany(a => a.Services).Select(s => s.LaserServiceId).Distinct().ToList();
        var services = await _db.LaserServices.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        return items.Select(a =>
        {
            customers.TryGetValue(a.CustomerId, out var customer);
            var names = a.Services
                .Select(l => services.TryGetValue(l.LaserServiceId, out var svc) ? svc.Name : null)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            var clinicalEnd = a.StartTime.AddMinutes(a.DurationMinutes);
            return new OccupiedInterval(
                a.StartTime,
                a.StartTime.AddMinutes(a.DurationMinutes + bufferMinutes),
                clinicalEnd,
                a.Id,
                customer?.FullName,
                a.DurationMinutes,
                names.Count == 0 ? null : string.Join(" + ", names));
        }).ToList();
    }

    private static void EnsureAttendedOnAppointmentDay(LaserAppointment entity)
    {
        if (IsAttendedWindow(entity))
            return;

        throw new AppException(
            "laser.appointment.attended_time",
            "تسجيل «حضرت» يتم في يوم الموعد وبعد وقت البداية فقط. يمكنك تغيير الحالة إلى أي حالة أخرى في أي وقت.",
            400);
    }

    private static bool IsAttendedWindow(LaserAppointment entity)
    {
        var now = GetEgyptNow();
        if (DateOnly.FromDateTime(now) != entity.AppointmentDate)
            return false;

        return TimeOnly.FromDateTime(now) >= entity.StartTime;
    }

    private static DateTime GetEgyptNow()
    {
        try
        {
            var tzId = OperatingSystem.IsWindows() ? "Egypt Standard Time" : "Africa/Cairo";
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.UtcNow.AddHours(2);
        }
        catch (InvalidTimeZoneException)
        {
            return DateTime.UtcNow.AddHours(2);
        }
    }

    private async Task<IReadOnlyList<LaserAppointmentDto>> MapManyAsync(
        IReadOnlyList<LaserAppointment> appointments,
        CancellationToken cancellationToken)
    {
        if (appointments.Count == 0)
            return [];

        var customerIds = appointments.Select(a => a.CustomerId).Distinct().ToList();
        var customers = await _db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var serviceIds = appointments.SelectMany(a => a.Services).Select(s => s.LaserServiceId).Distinct().ToList();
        var services = await _db.LaserServices.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        return appointments.Select(a =>
        {
            customers.TryGetValue(a.CustomerId, out var customer);
            return new LaserAppointmentDto(
                a.Id,
                a.CustomerId,
                customer?.FullName ?? "—",
                customer?.PhoneNumber ?? "—",
                a.AppointmentDate,
                a.StartTime,
                a.EndTime,
                a.DurationMinutes,
                a.Status,
                a.Notes,
                a.Services.Select(line =>
                {
                    services.TryGetValue(line.LaserServiceId, out var svc);
                    return new AppointmentServiceLineDto(
                        line.Id,
                        line.LaserServiceId,
                        svc?.Name ?? "—",
                        line.DurationMinutes,
                        line.PulsesConsumed);
                }).ToList(),
                a.AmountPaid);
        }).ToList();
    }
}
