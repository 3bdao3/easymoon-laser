using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.LaserClinic.Application.Customers;
using ErpClink.Modules.LaserClinic.Domain.Appointments;
using ErpClink.Modules.LaserClinic.Domain.Customers;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Customers;

public sealed class CustomerAppService : ICustomerAppService
{
    private readonly LaserClinicDbContext _db;

    public CustomerAppService(LaserClinicDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomerListItemDto>> SearchAsync(
        string? query,
        bool? isActive = true,
        CustomerListSort sort = CustomerListSort.Name,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Customers.AsNoTracking().AsQueryable();
        if (isActive.HasValue)
            q = q.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(c => c.FullName.Contains(term) || c.PhoneNumber.Contains(term));
        }

        q = sort switch
        {
            CustomerListSort.Newest => q.OrderByDescending(c => c.CreatedAtUtc),
            CustomerListSort.Oldest => q.OrderBy(c => c.CreatedAtUtc),
            _ => q.OrderBy(c => c.FullName)
        };

        List<Customer> customers;
        if (sort == CustomerListSort.LastAppointment)
        {
            var baseCustomers = _db.Customers.AsNoTracking().AsQueryable();
            if (isActive.HasValue)
                baseCustomers = baseCustomers.Where(c => c.IsActive == isActive.Value);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                baseCustomers = baseCustomers.Where(c => c.FullName.Contains(term) || c.PhoneNumber.Contains(term));
            }

            // Pull a working set, then rank by last booking (date + time) in memory.
            var candidates = await baseCustomers.Take(500).ToListAsync(cancellationToken);
            var candidateIds = candidates.Select(c => c.Id).ToList();
            var lastPreview = await BuildLastAppointmentsAsync(candidateIds, cancellationToken);
            customers = candidates
                .OrderByDescending(c => lastPreview.TryGetValue(c.Id, out var l) ? l.Date : DateOnly.MinValue)
                .ThenByDescending(c => lastPreview.TryGetValue(c.Id, out var l) ? l.StartTime : TimeOnly.MinValue)
                .ThenBy(c => c.FullName)
                .Take(200)
                .ToList();
        }
        else
        {
            customers = await q.Take(200).ToListAsync(cancellationToken);
        }

        if (customers.Count == 0)
            return [];

        var clinicNow = GetClinicLocalNow();
        var today = DateOnly.FromDateTime(clinicNow);
        var nowTime = TimeOnly.FromDateTime(clinicNow);
        var customerIds = customers.Select(c => c.Id).ToList();

        var nextByCustomer = await BuildNextAppointmentsAsync(customerIds, today, nowTime, cancellationToken);
        var lastByCustomer = await BuildLastAppointmentsAsync(customerIds, cancellationToken);

        IEnumerable<Customer> ordered = customers;
        if (sort == CustomerListSort.NextAppointment)
        {
            ordered = customers
                .OrderBy(c => nextByCustomer.ContainsKey(c.Id) ? 0 : 1)
                .ThenBy(c => nextByCustomer.TryGetValue(c.Id, out var n) ? n.Date : DateOnly.MaxValue)
                .ThenBy(c => nextByCustomer.TryGetValue(c.Id, out var n) ? n.StartTime : TimeOnly.MaxValue)
                .ThenBy(c => c.FullName);
        }
        else if (sort == CustomerListSort.LastAppointment)
        {
            ordered = customers
                .OrderByDescending(c => lastByCustomer.TryGetValue(c.Id, out var l) ? l.Date : DateOnly.MinValue)
                .ThenByDescending(c => lastByCustomer.TryGetValue(c.Id, out var l) ? l.StartTime : TimeOnly.MinValue)
                .ThenBy(c => c.FullName);
        }

        return ordered
            .Select(c => new CustomerListItemDto(
                c.Id,
                c.FullName,
                c.PhoneNumber,
                c.Age,
                c.Notes,
                c.IsActive,
                c.CreatedAtUtc,
                c.UpdatedAtUtc,
                nextByCustomer.TryGetValue(c.Id, out var next) ? next : null,
                lastByCustomer.TryGetValue(c.Id, out var last) ? last : null))
            .ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
            return null;
        return await MapAsync(entity, cancellationToken);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        var phone = RequireEgyptianMobile(request.PhoneNumber);
        var duplicate = await _db.Customers.AnyAsync(
            c => c.IsActive && c.PhoneNumber == phone,
            cancellationToken);
        if (duplicate)
            throw new AppException("laser.customer.phone_exists", "رقم الهاتف مستخدم بالفعل.", 409);

        var entity = Customer.Create(request.FullName, phone, request.Age, request.Notes, userId, DateTime.UtcNow);
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new AppException("laser.customer.not_found", "العميل غير موجود.", 404);

        var phone = RequireEgyptianMobile(request.PhoneNumber);
        var duplicate = await _db.Customers.AnyAsync(
            c => c.IsActive && c.PhoneNumber == phone && c.Id != id,
            cancellationToken);
        if (duplicate)
            throw new AppException("laser.customer.phone_exists", "رقم الهاتف مستخدم بالفعل.", 409);

        entity.Update(request.FullName, phone, request.Age, request.Notes, userId, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, string? userId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new AppException("laser.customer.not_found", "العميل غير موجود.", 404);

        if (isActive)
        {
            entity.Activate(userId, DateTime.UtcNow);
        }
        else
        {
            entity.Deactivate(userId, DateTime.UtcNow);
            var now = DateTime.UtcNow;
            var openAppointments = await _db.Appointments
                .Where(a => a.CustomerId == id && a.Status != LaserAppointmentStatus.Cancelled)
                .ToListAsync(cancellationToken);
            foreach (var appointment in openAppointments)
                appointment.ChangeStatus(LaserAppointmentStatus.Cancelled, userId, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, CustomerNextAppointmentDto>> BuildNextAppointmentsAsync(
        IReadOnlyList<Guid> customerIds,
        DateOnly today,
        TimeOnly nowTime,
        CancellationToken cancellationToken)
    {
        var appointments = await _db.Appointments.AsNoTracking()
            .Include(a => a.Services)
            .Where(a => customerIds.Contains(a.CustomerId)
                        && (a.Status == LaserAppointmentStatus.Pending
                            || a.Status == LaserAppointmentStatus.Confirmed))
            .ToListAsync(cancellationToken);

        appointments = appointments
            .Where(a => NextAppointmentSelector.IsUpcoming(a.AppointmentDate, a.StartTime, a.Status, today, nowTime))
            .ToList();

        if (appointments.Count == 0)
            return new Dictionary<Guid, CustomerNextAppointmentDto>();

        var serviceIds = appointments.SelectMany(a => a.Services).Select(s => s.LaserServiceId).Distinct().ToList();
        var names = await _db.LaserServices.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return appointments
            .GroupBy(a => a.CustomerId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var next = NextAppointmentSelector.PickNext(g, a => a.AppointmentDate, a => a.StartTime)!;
                    var serviceNames = next.Services
                        .Select(s => names.TryGetValue(s.LaserServiceId, out var n) ? n : "—")
                        .ToList();
                    return new CustomerNextAppointmentDto(
                        next.Id,
                        next.AppointmentDate,
                        next.StartTime,
                        next.EndTime,
                        next.DurationMinutes,
                        next.Status,
                        serviceNames);
                });
    }

    private async Task<Dictionary<Guid, CustomerLastAppointmentDto>> BuildLastAppointmentsAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var appointments = await _db.Appointments.AsNoTracking()
            .Include(a => a.Services)
            .Where(a => customerIds.Contains(a.CustomerId)
                        && a.Status != LaserAppointmentStatus.Cancelled)
            .ToListAsync(cancellationToken);

        if (appointments.Count == 0)
            return new Dictionary<Guid, CustomerLastAppointmentDto>();

        var serviceIds = appointments.SelectMany(a => a.Services).Select(s => s.LaserServiceId).Distinct().ToList();
        var names = await _db.LaserServices.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return appointments
            .GroupBy(a => a.CustomerId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var last = g
                        .OrderByDescending(a => a.AppointmentDate)
                        .ThenByDescending(a => a.StartTime)
                        .First();
                    var serviceNames = last.Services
                        .Select(s => names.TryGetValue(s.LaserServiceId, out var n) ? n : "—")
                        .ToList();
                    return new CustomerLastAppointmentDto(
                        last.Id,
                        last.AppointmentDate,
                        last.StartTime,
                        last.EndTime,
                        last.DurationMinutes,
                        last.Status,
                        serviceNames);
                });
    }

    private static DateTime GetClinicLocalNow()
    {
        try
        {
            var tzId = OperatingSystem.IsWindows() ? "Egypt Standard Time" : "Africa/Cairo";
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.UtcNow.AddHours(3);
        }
        catch (InvalidTimeZoneException)
        {
            return DateTime.UtcNow.AddHours(3);
        }
    }

    private async Task<CustomerPulseBalanceDto> MapPulseAsync(Customer c, CancellationToken cancellationToken)
    {
        var total = c.PulsePackageTotal;
        var remaining = c.PulsePackageRemaining;
        var consumed = total is int t && remaining is int r ? Math.Max(0, t - r) : 0;

        var amountPaid = await _db.Appointments.AsNoTracking()
            .Where(a => a.CustomerId == c.Id && a.Status != LaserAppointmentStatus.Cancelled)
            .SumAsync(a => a.AmountPaid ?? 0m, cancellationToken);

        var durationUsed = await _db.Appointments.AsNoTracking()
            .Where(a => a.CustomerId == c.Id && a.Status == LaserAppointmentStatus.Attended)
            .SumAsync(a => (int?)a.DurationMinutes ?? 0, cancellationToken);

        var priceTotal = c.PackagePriceTotal;
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

    private static string RequireEgyptianMobile(string phoneNumber)
    {
        var phone = Customer.NormalizeEgyptianMobile(phoneNumber);
        if (phone is null)
            throw new AppException(
                "laser.customer.phone_invalid",
                "رقم الموبايل لازم يكون مصري من 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015.",
                400);
        return phone;
    }

    private async Task<CustomerDto> MapAsync(Customer c, CancellationToken cancellationToken) =>
        new(c.Id, c.FullName, c.PhoneNumber, c.Age, c.Notes, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc,
            await MapPulseAsync(c, cancellationToken));
}
