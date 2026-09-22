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
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using ErpClink.Modules.Prescriptions.Application.Medications.Models;
using ErpClink.Modules.Prescriptions.Application.Prescriptions.Models;
using ErpClink.Modules.Prescriptions.Domain.Prescriptions;
using ErpClink.Modules.Prescriptions.Infrastructure.Events;
using ErpClink.Modules.Queue.Application.Entries.Models;
using ErpClink.Modules.Scheduling.Application.Schedules.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class PrescriptionsApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PrescriptionsApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Full_prescription_flow_with_medication_snapshot()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        _factory.Services.GetRequiredService<PrescriptionsDomainEventCollector>().Clear();

        var med = await CreateMedicationAsync(client, "PARA500", "Paracetamol");
        var visit = await SeedVisitAsync(client);

        var create = await client.PostAsJsonAsync("/api/v1/prescriptions", new CreatePrescriptionRequest(
            visit.VisitId, "notes",
            [new PrescriptionItemInput(med.Id, "500mg", "TID", "5 days", "Oral", "after food", 15, null, 1)]));
        var body = await create.Content.ReadAsStringAsync();
        create.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        var rx = JsonSerializer.Deserialize<PrescriptionDto>(body, JsonOptions)!;
        rx.Status.Should().Be(nameof(PrescriptionStatus.Draft));
        rx.PatientId.Should().Be(visit.PatientId);
        rx.DoctorId.Should().Be(visit.DoctorId);
        rx.Items.Should().ContainSingle();
        rx.Items[0].MedicationNameSnapshot.Should().Contain("Paracetamol");

        await client.PutAsJsonAsync($"/api/v1/medications/{med.Id}",
            new UpdateMedicationRequest("Paracetamol Renamed", null, "500mg", "Tablet", "Oral"));

        (await client.PostAsync($"/api/v1/prescriptions/{rx.Id}/issue", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var issued = await client.GetFromJsonAsync<PrescriptionDto>($"/api/v1/prescriptions/{rx.Id}", JsonOptions);
        issued!.Status.Should().Be(nameof(PrescriptionStatus.Issued));
        issued.Items[0].MedicationNameSnapshot.Should().Contain("Paracetamol");
        issued.Items[0].MedicationNameSnapshot.Should().NotContain("Renamed");

        (await client.PutAsJsonAsync($"/api/v1/prescriptions/{rx.Id}", new UpdatePrescriptionRequest("x", issued.RowVersion)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        var history = await client.GetFromJsonAsync<PagedPrescriptionsResult>(
            $"/api/v1/prescriptions/patients/{visit.PatientId}/history?page=1&pageSize=10", JsonOptions);
        history!.Items.Should().Contain(i => i.Id == rx.Id);
    }

    [Fact]
    public async Task Validation_issue_cancel_and_items()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var med = await CreateMedicationAsync(client, "AMOX250", "Amoxicillin");
        var visit = await SeedVisitAsync(client);

        var emptyCreate = await client.PostAsJsonAsync("/api/v1/prescriptions",
            new CreatePrescriptionRequest(visit.VisitId, null, null));
        emptyCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var empty = await emptyCreate.Content.ReadFromJsonAsync<PrescriptionDto>(JsonOptions);
        (await client.PostAsync($"/api/v1/prescriptions/{empty!.Id}/issue", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var withItem = await client.PostAsJsonAsync($"/api/v1/prescriptions/{empty.Id}/items",
            new PrescriptionItemInput(med.Id, "250mg", "BID", "7d", "Oral", null, null, null, null));
        var withItemBody = await withItem.Content.ReadAsStringAsync();
        withItem.StatusCode.Should().Be(HttpStatusCode.OK, because: withItemBody);
        var draft = await withItem.Content.ReadFromJsonAsync<PrescriptionDto>(JsonOptions);
        var itemId = draft!.Items[0].Id;

        (await client.PutAsJsonAsync($"/api/v1/prescriptions/{draft.Id}/items/{itemId}",
            new UpdatePrescriptionItemRequest("500mg", "TID", "5d", "Oral", null, 10, null, 1, draft.RowVersion)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var afterUpdate = await client.GetFromJsonAsync<PrescriptionDto>($"/api/v1/prescriptions/{draft.Id}", JsonOptions);
        (await client.DeleteAsync($"/api/v1/prescriptions/{draft.Id}/items/{itemId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        await client.PostAsJsonAsync($"/api/v1/prescriptions/{draft.Id}/items",
            new PrescriptionItemInput(med.Id, "250mg", "BID", null, null, null, null, null, null));

        (await client.PostAsync($"/api/v1/prescriptions/{draft.Id}/issue", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync($"/api/v1/prescriptions/{draft.Id}/cancel", new CancelPrescriptionRequest("error")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        await client.PostAsJsonAsync($"/api/v1/medications/{med.Id}/deactivate", new { });
        var visit2 = await SeedVisitAsync(client);
        (await client.PostAsJsonAsync("/api/v1/prescriptions", new CreatePrescriptionRequest(
            visit2.VisitId, null, [new PrescriptionItemInput(med.Id, "1", "OD", null, null, null, null, null, null)])))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Duplicate_and_concurrency()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var visit = await SeedVisitAsync(client);
        var first = await client.PostAsJsonAsync("/api/v1/prescriptions", new CreatePrescriptionRequest(visit.VisitId, null, null));
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var rx = await first.Content.ReadFromJsonAsync<PrescriptionDto>(JsonOptions);

        (await client.PostAsJsonAsync("/api/v1/prescriptions", new CreatePrescriptionRequest(visit.VisitId, null, null)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await c.PostAsJsonAsync("/api/v1/prescriptions", new CreatePrescriptionRequest(visit.VisitId, null, null));
        });
        var responses = await Task.WhenAll(tasks);
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(0);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(20);

        var u1 = await client.PutAsJsonAsync($"/api/v1/prescriptions/{rx!.Id}", new UpdatePrescriptionRequest("a", rx.RowVersion));
        u1.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"/api/v1/prescriptions/{rx.Id}", new UpdatePrescriptionRequest("stale", rx.RowVersion)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Concurrent_create_unique_numbers_and_auth()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        var visits = new List<Guid>();
        for (var i = 0; i < 20; i++)
            visits.Add((await SeedVisitAsync(client)).VisitId);

        var tasks = visits.Select(async visitId =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await c.PostAsJsonAsync("/api/v1/prescriptions", new CreatePrescriptionRequest(visitId, null, null));
            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
            return (await response.Content.ReadFromJsonAsync<PrescriptionDto>(JsonOptions))!.PrescriptionNumber;
        });
        var numbers = await Task.WhenAll(tasks);
        numbers.Distinct().Should().HaveCount(20);

        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        (await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email, "TempUser!123", "Inventory", null, [AppRoles.InventoryManager]))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");
        (await client.GetAsync("/api/v1/prescriptions/search")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/medications/search")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Medication_search_and_requires_visit()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        await CreateMedicationAsync(client, "IBU200", "Ibuprofen");
        var search = await client.GetFromJsonAsync<PagedMedicationsResult>(
            "/api/v1/medications/search?q=Ibu&activeOnly=true&page=1&pageSize=10", JsonOptions);
        search!.Items.Should().Contain(m => m.Code == "IBU200");

        (await client.PostAsJsonAsync("/api/v1/prescriptions",
            new CreatePrescriptionRequest(Guid.Parse("99999999-9999-9999-9999-999999999999"), null, null)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<MedicationDto> CreateMedicationAsync(HttpClient client, string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/medications",
            new CreateMedicationRequest(code, name, null, "500mg", "Tablet", "Oral"));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<MedicationDto>(body, JsonOptions)!;
    }

    private async Task<VisitSeed> SeedVisitAsync(HttpClient client)
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
            book = await client.PostAsJsonAsync("/api/v1/appointments",
                new BookAppointmentRequest(patient.Id, doctor.Id, clinic.Id, today,
                    TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes)), null, null));
            if (book.StatusCode == HttpStatusCode.Created) break;
        }

        var apptBody = await book!.Content.ReadAsStringAsync();
        book.StatusCode.Should().Be(HttpStatusCode.Created, because: apptBody);
        var appointment = JsonSerializer.Deserialize<AppointmentDto>(apptBody, JsonOptions)!;
        var checkIn = await client.PostAsJsonAsync("/api/v1/queue/check-in", new CheckInRequest(appointment.Id, null));
        var queue = await checkIn.Content.ReadFromJsonAsync<QueueEntryDto>(JsonOptions);
        var start = await client.PostAsJsonAsync("/api/v1/medical-visits/start",
            new StartMedicalVisitRequest(appointment.Id, queue!.Id));
        var visitBody = await start.Content.ReadAsStringAsync();
        start.StatusCode.Should().Be(HttpStatusCode.Created, because: visitBody);
        var visit = JsonSerializer.Deserialize<MedicalVisitDto>(visitBody, JsonOptions)!;
        return new VisitSeed(visit.Id, patient.Id, doctor.Id);
    }

    private sealed record VisitSeed(Guid VisitId, Guid PatientId, Guid DoctorId);

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
            new CreateClinicRequest($"P{Guid.NewGuid():N}"[..8], "Rx Clinic", null, "R1"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClinicDto>(JsonOptions))!;
    }

    private static async Task<DoctorDto> RegisterDoctorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/doctors",
            new RegisterDoctorRequest("Rx", $"Doc{Guid.NewGuid():N}"[..8], null, null, null, null, null, null));
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
        return (await response.Content.ReadFromJsonAsync<RegisterPatientResult>(JsonOptions))!.Patient;
    }
}
