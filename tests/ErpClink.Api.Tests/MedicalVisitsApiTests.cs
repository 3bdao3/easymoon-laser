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
using ErpClink.Modules.MedicalVisits.Application.Visits.Models;
using ErpClink.Modules.MedicalVisits.Domain.Visits;
using ErpClink.Modules.MedicalVisits.Infrastructure.Events;
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using ErpClink.Modules.Queue.Application.Entries.Models;
using ErpClink.Modules.Scheduling.Application.Schedules.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class MedicalVisitsApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MedicalVisitsApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Start_update_complete_history_and_search_work()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        _factory.Services.GetRequiredService<MedicalVisitsDomainEventCollector>().Clear();

        var seeded = await SeedCheckedInAsync(client);
        var start = await client.PostAsJsonAsync("/api/v1/medical-visits/start",
            new StartMedicalVisitRequest(seeded.AppointmentId, seeded.QueueEntryId));
        var body = await start.Content.ReadAsStringAsync();
        start.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        var visit = JsonSerializer.Deserialize<MedicalVisitDto>(body, JsonOptions)!;
        visit.Status.Should().Be(nameof(MedicalVisitStatus.Open));
        visit.VisitNumber.Should().StartWith("V-");

        (await client.GetAsync($"/api/v1/medical-visits/{visit.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/v1/medical-visits/by-number/{visit.VisitNumber}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await client.PutAsJsonAsync($"/api/v1/medical-visits/{visit.Id}/clinical-notes",
            new UpdateClinicalNotesRequest("headache", "exam ok", "clear", "tension", "rest", visit.RowVersion));
        var updatedBody = await updated.Content.ReadAsStringAsync();
        updated.StatusCode.Should().Be(HttpStatusCode.OK, because: updatedBody);
        var notes = JsonSerializer.Deserialize<MedicalVisitDto>(updatedBody, JsonOptions)!;
        notes.Status.Should().Be(nameof(MedicalVisitStatus.InProgress));
        notes.ChiefComplaint.Should().Be("headache");

        (await client.PostAsync($"/api/v1/medical-visits/{visit.Id}/complete", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var history = await client.GetFromJsonAsync<PagedMedicalVisitsResult>(
            $"/api/v1/medical-visits/patients/{seeded.PatientId}/history?page=1&pageSize=10", JsonOptions);
        history!.Items.Should().Contain(i => i.Id == visit.Id);

        var search = await client.GetFromJsonAsync<PagedMedicalVisitsResult>(
            $"/api/v1/medical-visits/search?appointmentId={seeded.AppointmentId}&page=1&pageSize=10", JsonOptions);
        search!.TotalCount.Should().BeGreaterThan(0);

        var events = _factory.Services.GetRequiredService<MedicalVisitsDomainEventCollector>().Events;
        events.OfType<MedicalVisitStartedDomainEvent>().Should().Contain(e => e.MedicalVisitId == visit.Id);
        events.OfType<MedicalVisitCompletedDomainEvent>().Should().Contain(e => e.MedicalVisitId == visit.Id);
    }

    [Fact]
    public async Task Start_requires_checked_in_and_rejects_cancelled_noshow()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var scheduled = await SeedAppointmentAsync(client);
        (await client.PostAsJsonAsync("/api/v1/medical-visits/start", new StartMedicalVisitRequest(scheduled.AppointmentId, null)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var cancelled = await SeedAppointmentAsync(client);
        await client.PostAsJsonAsync($"/api/v1/appointments/{cancelled.AppointmentId}/cancel", new CancelAppointmentRequest("x"));
        (await client.PostAsJsonAsync("/api/v1/medical-visits/start", new StartMedicalVisitRequest(cancelled.AppointmentId, null)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var noShow = await SeedAppointmentAsync(client);
        await client.PostAsync($"/api/v1/appointments/{noShow.AppointmentId}/confirm", null);
        await client.PostAsync($"/api/v1/appointments/{noShow.AppointmentId}/no-show", null);
        (await client.PostAsJsonAsync("/api/v1/medical-visits/start", new StartMedicalVisitRequest(noShow.AppointmentId, null)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Duplicate_and_invalid_transitions()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var seeded = await SeedCheckedInAsync(client);

        var first = await client.PostAsJsonAsync("/api/v1/medical-visits/start",
            new StartMedicalVisitRequest(seeded.AppointmentId, seeded.QueueEntryId));
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var visit = await first.Content.ReadFromJsonAsync<MedicalVisitDto>(JsonOptions);

        (await client.PostAsJsonAsync("/api/v1/medical-visits/start",
            new StartMedicalVisitRequest(seeded.AppointmentId, seeded.QueueEntryId)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await client.PostAsync($"/api/v1/medical-visits/{visit!.Id}/complete", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync($"/api/v1/medical-visits/{visit.Id}/cancel", new CancelMedicalVisitRequest("late")))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Concurrent_duplicate_start_allows_one()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var seeded = await SeedCheckedInAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await c.PostAsJsonAsync("/api/v1/medical-visits/start",
                new StartMedicalVisitRequest(seeded.AppointmentId, seeded.QueueEntryId));
        });
        var responses = await Task.WhenAll(tasks);
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(19);
    }

    [Fact]
    public async Task Concurrent_starts_produce_unique_visit_numbers()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        var clinic = await CreateClinicAsync(client);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var slots = new List<(Guid DoctorId, TimeOnly Start)>();
        for (var d = 0; d < 2; d++)
        {
            var doctor = await RegisterDoctorAsync(client);
            (await client.PostAsJsonAsync($"/api/v1/doctors/{doctor.Id}/clinics",
                new AssignDoctorToClinicRequest(clinic.Id, new DateOnly(2020, 1, 1), null))).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
                new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, today.DayOfWeek,
                    new TimeOnly(8, 0), new TimeOnly(11, 0), 15, today, null))).EnsureSuccessStatusCode();
            for (var minutes = 8 * 60; minutes < 11 * 60; minutes += 15)
                slots.Add((doctor.Id, TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes))));
        }

        var checkedIn = new List<(Guid AppointmentId, Guid QueueEntryId)>();
        foreach (var (doctorId, start) in slots.Take(20))
        {
            var patient = await RegisterPatientAsync(client);
            var book = await client.PostAsJsonAsync("/api/v1/appointments",
                new BookAppointmentRequest(patient.Id, doctorId, clinic.Id, today, start, null, null));
            var bookBody = await book.Content.ReadAsStringAsync();
            book.StatusCode.Should().Be(HttpStatusCode.Created, because: bookBody);
            var appointment = JsonSerializer.Deserialize<AppointmentDto>(bookBody, JsonOptions)!;
            var checkIn = await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(appointment.Id, null));
            var q = await checkIn.Content.ReadFromJsonAsync<QueueEntryDto>(JsonOptions);
            checkedIn.Add((appointment.Id, q!.Id));
        }

        var tasks = checkedIn.Select(async item =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await c.PostAsJsonAsync("/api/v1/medical-visits/start",
                new StartMedicalVisitRequest(item.AppointmentId, item.QueueEntryId));
            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
            return (await response.Content.ReadFromJsonAsync<MedicalVisitDto>(JsonOptions))!.VisitNumber;
        });

        var numbers = await Task.WhenAll(tasks);
        numbers.Distinct().Should().HaveCount(20);
    }

    [Fact]
    public async Task Clinical_notes_concurrency_and_authorization()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var seeded = await SeedCheckedInAsync(client);
        var visit = await (await client.PostAsJsonAsync("/api/v1/medical-visits/start",
            new StartMedicalVisitRequest(seeded.AppointmentId, seeded.QueueEntryId)))
            .Content.ReadFromJsonAsync<MedicalVisitDto>(JsonOptions);

        var first = await client.PutAsJsonAsync($"/api/v1/medical-visits/{visit!.Id}/clinical-notes",
            new UpdateClinicalNotesRequest("a", null, null, null, null, visit.RowVersion));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterFirst = await first.Content.ReadFromJsonAsync<MedicalVisitDto>(JsonOptions);

        (await client.PutAsJsonAsync($"/api/v1/medical-visits/{visit.Id}/clinical-notes",
            new UpdateClinicalNotesRequest("stale", null, null, null, null, visit.RowVersion)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await client.PutAsJsonAsync($"/api/v1/medical-visits/{visit.Id}/clinical-notes",
            new UpdateClinicalNotesRequest("b", null, null, null, null, afterFirst!.RowVersion)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsJsonAsync("/api/v1/medical-visits/start",
            new StartMedicalVisitRequest(Guid.Parse("99999999-9999-9999-9999-999999999999"), null)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        (await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email, "TempUser!123", "Inventory", null, [AppRoles.InventoryManager]))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");
        (await client.GetAsync($"/api/v1/medical-visits/{visit.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_open_visit_works()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var seeded = await SeedCheckedInAsync(client);
        var visit = await (await client.PostAsJsonAsync("/api/v1/medical-visits/start",
            new StartMedicalVisitRequest(seeded.AppointmentId, seeded.QueueEntryId)))
            .Content.ReadFromJsonAsync<MedicalVisitDto>(JsonOptions);

        (await client.PostAsJsonAsync($"/api/v1/medical-visits/{visit!.Id}/cancel", new CancelMedicalVisitRequest("left")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var done = await client.GetFromJsonAsync<MedicalVisitDto>($"/api/v1/medical-visits/{visit.Id}", JsonOptions);
        done!.Status.Should().Be(nameof(MedicalVisitStatus.Cancelled));
    }

    private async Task<CheckedInContext> SeedCheckedInAsync(HttpClient client)
    {
        var appt = await SeedAppointmentAsync(client);
        var checkIn = await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(appt.AppointmentId, null));
        var body = await checkIn.Content.ReadAsStringAsync();
        checkIn.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        var queue = JsonSerializer.Deserialize<QueueEntryDto>(body, JsonOptions)!;
        return new CheckedInContext(appt.AppointmentId, appt.PatientId, queue.Id);
    }

    private async Task<SeededAppointment> SeedAppointmentAsync(HttpClient client)
    {
        var clinic = await CreateClinicAsync(client);
        var doctor = await RegisterDoctorAsync(client);
        (await client.PostAsJsonAsync($"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(clinic.Id, new DateOnly(2020, 1, 1), null))).EnsureSuccessStatusCode();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        (await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, today.DayOfWeek,
                new TimeOnly(8, 0), new TimeOnly(20, 0), 30, today, null))).EnsureSuccessStatusCode();

        var patient = await RegisterPatientAsync(client);
        HttpResponseMessage? book = null;
        for (var minutes = 8 * 60; minutes < 20 * 60; minutes += 30)
        {
            var start = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes));
            book = await client.PostAsJsonAsync("/api/v1/appointments",
                new BookAppointmentRequest(patient.Id, doctor.Id, clinic.Id, today, start, null, null));
            if (book.StatusCode == HttpStatusCode.Created)
            {
                var body = await book.Content.ReadAsStringAsync();
                var appointment = JsonSerializer.Deserialize<AppointmentDto>(body, JsonOptions)!;
                return new SeededAppointment(appointment.Id, patient.Id);
            }
        }

        var fail = book is null ? "no attempt" : await book.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Could not book: {fail}");
    }

    private sealed record CheckedInContext(Guid AppointmentId, Guid PatientId, Guid QueueEntryId);
    private sealed record SeededAppointment(Guid AppointmentId, Guid PatientId);

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
            new CreateClinicRequest($"M{Guid.NewGuid():N}"[..8], "MV Clinic", null, "R1"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClinicDto>(JsonOptions))!;
    }

    private static async Task<DoctorDto> RegisterDoctorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/doctors",
            new RegisterDoctorRequest("Med", $"Doc{Guid.NewGuid():N}"[..8], null, null, null, null, null, null));
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
