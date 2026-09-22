using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Appointments.Application.Appointments.Models;
using ErpClink.Modules.Doctors.Application.Clinics.Models;
using ErpClink.Modules.Doctors.Application.Doctors.Models;
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using ErpClink.Modules.Queue.Application.Entries.Models;
using ErpClink.Modules.Queue.Domain.Entries;
using ErpClink.Modules.Queue.Infrastructure.Events;
using ErpClink.Modules.Scheduling.Application.Schedules.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class QueueApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public QueueApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Checkin_flow_call_start_complete_works()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        _factory.Services.GetRequiredService<QueueDomainEventCollector>().Clear();

        var ctx = await SeedTodayAppointmentAsync(client);
        var checkIn = await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(ctx.AppointmentId, null));
        var body = await checkIn.Content.ReadAsStringAsync();
        checkIn.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        var entry = JsonSerializer.Deserialize<QueueEntryDto>(body, JsonOptions)!;
        entry.Status.Should().Be(nameof(QueueStatus.Waiting));
        entry.QueueNumber.Should().StartWith("Q-");
        entry.CreatedBy.Should().NotBeNullOrWhiteSpace();

        (await client.GetAsync($"/api/v1/queue/{entry.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        var today = await client.GetFromJsonAsync<List<QueueListItemDto>>("/api/v1/queue/today", JsonOptions);
        today!.Should().Contain(i => i.Id == entry.Id);

        (await client.PostAsync($"/api/v1/queue/{entry.Id}/call", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/v1/queue/{entry.Id}/start-service", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/v1/queue/{entry.Id}/complete", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var done = await client.GetFromJsonAsync<QueueEntryDto>($"/api/v1/queue/{entry.Id}", JsonOptions);
        done!.Status.Should().Be(nameof(QueueStatus.Completed));

        var events = _factory.Services.GetRequiredService<QueueDomainEventCollector>().Events;
        events.OfType<QueueEntryCheckedInDomainEvent>().Should().Contain(e => e.QueueEntryId == entry.Id);
    }

    [Fact]
    public async Task Cancelled_and_noshow_checkin_fail_and_invalid_transitions()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var cancelled = await SeedTodayAppointmentAsync(client);
        await client.PostAsJsonAsync($"/api/v1/appointments/{cancelled.AppointmentId}/cancel", new CancelAppointmentRequest("x"));
        (await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(cancelled.AppointmentId, null)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var noShow = await SeedTodayAppointmentAsync(client);
        await client.PostAsync($"/api/v1/appointments/{noShow.AppointmentId}/confirm", null);
        await client.PostAsync($"/api/v1/appointments/{noShow.AppointmentId}/no-show", null);
        (await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(noShow.AppointmentId, null)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var ok = await SeedTodayAppointmentAsync(client);
        var entry = await (await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(ok.AppointmentId, null)))
            .Content.ReadFromJsonAsync<QueueEntryDto>(JsonOptions);
        (await client.PostAsync($"/api/v1/queue/{entry!.Id}/complete", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsync($"/api/v1/queue/{entry.Id}/skip", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Concurrent_duplicate_checkin_allows_one()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var ctx = await SeedTodayAppointmentAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await c.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(ctx.AppointmentId, null));
        });
        var responses = await Task.WhenAll(tasks);
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(19);
    }

    [Fact]
    public async Task Concurrent_checkins_produce_unique_queue_numbers()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        // Same clinic scope so queue numbers share Organization+Branch+Clinic+Date.
        // Two doctors × 15-min slots provides enough distinct appointments for concurrency.
        var clinic = await CreateClinicAsync(client);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var doctorSlots = new List<(Guid DoctorId, TimeOnly Start)>();

        for (var d = 0; d < 2; d++)
        {
            var doctor = await RegisterDoctorAsync(client);
            (await client.PostAsJsonAsync($"/api/v1/doctors/{doctor.Id}/clinics",
                new AssignDoctorToClinicRequest(clinic.Id, new DateOnly(2020, 1, 1), null))).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
                new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, today.DayOfWeek,
                    new TimeOnly(8, 0), new TimeOnly(11, 0), 15, today, null))).EnsureSuccessStatusCode();

            for (var minutes = 8 * 60; minutes < 11 * 60; minutes += 15)
                doctorSlots.Add((doctor.Id, TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes))));
        }

        doctorSlots.Should().HaveCountGreaterThanOrEqualTo(20);

        var appointments = new List<Guid>();
        foreach (var (doctorId, start) in doctorSlots.Take(20))
        {
            var patient = await RegisterPatientAsync(client);
            var book = await client.PostAsJsonAsync("/api/v1/appointments",
                new BookAppointmentRequest(patient.Id, doctorId, clinic.Id, today, start, null, null));
            var body = await book.Content.ReadAsStringAsync();
            book.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
            appointments.Add(JsonSerializer.Deserialize<AppointmentDto>(body, JsonOptions)!.Id);
        }

        var tasks = appointments.Select(async id =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await c.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(id, null));
            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
            var entry = await response.Content.ReadFromJsonAsync<QueueEntryDto>(JsonOptions);
            return entry!.QueueNumber;
        });

        var numbers = await Task.WhenAll(tasks);
        numbers.Distinct().Should().HaveCount(20);
    }

    [Fact]
    public async Task Concurrent_call_allows_exactly_one_success()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var ctx = await SeedTodayAppointmentAsync(client);
        var entry = await (await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(ctx.AppointmentId, null)))
            .Content.ReadFromJsonAsync<QueueEntryDto>(JsonOptions);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await c.PostAsync($"/api/v1/queue/{entry!.Id}/call", null);
        });
        var responses = await Task.WhenAll(tasks);
        responses.Count(r => r.StatusCode == HttpStatusCode.NoContent).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(19);
    }

    [Fact]
    public async Task Search_permission_and_cross_org()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var ctx = await SeedTodayAppointmentAsync(client);
        await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(ctx.AppointmentId, null));

        var search = await client.GetFromJsonAsync<PagedQueueResult>(
            $"/api/v1/queue/search?appointmentId={ctx.AppointmentId}&page=1&pageSize=10", JsonOptions);
        search!.TotalCount.Should().BeGreaterThan(0);

        (await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(Guid.Parse("99999999-9999-9999-9999-999999999999"), null)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        (await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email, "TempUser!123", "Inventory", null, [AppRoles.InventoryManager]))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");
        (await client.GetAsync("/api/v1/queue/today")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<SharedClinicDay> CreateSharedClinicDayAsync(HttpClient client)
    {
        var clinic = await CreateClinicAsync(client);
        var doctor = await RegisterDoctorAsync(client);
        (await client.PostAsJsonAsync($"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(clinic.Id, new DateOnly(2020, 1, 1), null))).EnsureSuccessStatusCode();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Keep within a single calendar day without wrapping TimeOnly (avoids SlotGenerator hang near midnight).
        // 8:00–20:00 @ 30min => 24 slots (enough for concurrency tests).
        (await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, today.DayOfWeek,
                new TimeOnly(8, 0), new TimeOnly(20, 0), 30, today, null))).EnsureSuccessStatusCode();

        return new SharedClinicDay(clinic.Id, doctor.Id, today);
    }

    private async Task<SeededAppointment> SeedTodayAppointmentAsync(HttpClient client, SharedClinicDay? shared = null)
    {
        shared ??= await CreateSharedClinicDayAsync(client);

        var patient = await RegisterPatientAsync(client);
        HttpResponseMessage? book = null;
        for (var minutes = 8 * 60; minutes < 20 * 60; minutes += 30)
        {
            var start = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes));
            book = await client.PostAsJsonAsync("/api/v1/appointments",
                new BookAppointmentRequest(patient.Id, shared.DoctorId, shared.ClinicId, shared.Today, start, null, null));
            if (book.StatusCode == HttpStatusCode.Created)
            {
                var body = await book.Content.ReadAsStringAsync();
                var appointment = JsonSerializer.Deserialize<AppointmentDto>(body, JsonOptions)!;
                return new SeededAppointment(appointment.Id, patient.Id, shared.DoctorId, shared.ClinicId, shared.Today);
            }
        }

        var failBody = book is null ? "no attempt" : await book.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Could not book appointment: {failBody}");
    }

    private sealed record SharedClinicDay(Guid ClinicId, Guid DoctorId, DateOnly Today);
    private sealed record SeededAppointment(Guid AppointmentId, Guid PatientId, Guid DoctorId, Guid ClinicId, DateOnly Date);

    private async Task LoginAsAdminAsync(HttpClient client) =>
        await LoginAsync(client, "admin@erpclink.local", "ChangeMe!Admin123");

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

    private static async Task<ClinicDto> CreateClinicAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/clinics",
            new CreateClinicRequest($"Q{Guid.NewGuid():N}"[..8], "Queue Clinic", null, "R1"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClinicDto>(JsonOptions))!;
    }

    private static async Task<DoctorDto> RegisterDoctorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/doctors",
            new RegisterDoctorRequest("Queue", $"Doc{Guid.NewGuid():N}"[..8], null, null, null, null, null, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DoctorDto>(JsonOptions))!;
    }

    private static async Task<PatientDto> RegisterPatientAsync(HttpClient client)
    {
        var phone = $"+9665{Random.Shared.NextInt64(10000000, 99999999)}";
        var request = new RegisterPatientRequest(
            "Pat", null, $"Last{Guid.NewGuid():N}"[..8],
            new DateOnly(1990, 1, 1), Gender.Female, null, phone, null,
            null, null, null, null, null, AllowPossibleDuplicate: true);
        var response = await client.PostAsJsonAsync("/api/v1/patients", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegisterPatientResult>(JsonOptions);
        return result!.Patient;
    }
}
