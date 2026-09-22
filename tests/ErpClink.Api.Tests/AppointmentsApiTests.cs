using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Appointments.Application.Appointments.Models;
using ErpClink.Modules.Appointments.Domain.Appointments;
using ErpClink.Modules.Appointments.Infrastructure.Events;
using ErpClink.Modules.Doctors.Application.Clinics.Models;
using ErpClink.Modules.Doctors.Application.Doctors.Models;
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using ErpClink.Modules.Scheduling.Application.Schedules.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class AppointmentsApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppointmentsApiTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Complete_booking_confirm_cancel_flow_works()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        _factory.Services.GetRequiredService<AppointmentsDomainEventCollector>().Clear();

        var ctx = await SeedBookableContextAsync(client);
        var book = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(ctx.PatientId, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(9, 0), "Checkup", null));
        var bookBody = await book.Content.ReadAsStringAsync();
        book.StatusCode.Should().Be(HttpStatusCode.Created, because: bookBody);
        var appointment = JsonSerializer.Deserialize<AppointmentDto>(bookBody, JsonOptions)!;
        appointment.AppointmentNumber.Should().StartWith("A-");
        appointment.CreatedBy.Should().NotBeNullOrWhiteSpace();
        appointment.Status.Should().Be(nameof(AppointmentStatus.Scheduled));

        var byNumber = await client.GetAsync($"/api/v1/appointments/by-number/{appointment.AppointmentNumber}");
        byNumber.StatusCode.Should().Be(HttpStatusCode.OK);

        var search = await client.GetFromJsonAsync<PagedAppointmentsResult>(
            $"/api/v1/appointments/search?doctorId={ctx.DoctorId}&date={ctx.Date:yyyy-MM-dd}", JsonOptions);
        search!.Items.Should().Contain(i => i.Id == appointment.Id);

        (await client.PostAsync($"/api/v1/appointments/{appointment.Id}/confirm", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var confirmed = await client.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{appointment.Id}", JsonOptions);
        confirmed!.Status.Should().Be(nameof(AppointmentStatus.Confirmed));

        (await client.PostAsJsonAsync($"/api/v1/appointments/{appointment.Id}/cancel", new CancelAppointmentRequest("changed mind")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var cancelled = await client.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{appointment.Id}", JsonOptions);
        cancelled!.Status.Should().Be(nameof(AppointmentStatus.Cancelled));
        cancelled.CancelledBy.Should().NotBeNullOrWhiteSpace();

        var events = _factory.Services.GetRequiredService<AppointmentsDomainEventCollector>().Events;
        events.OfType<AppointmentBookedDomainEvent>().Should().Contain(e => e.AppointmentId == appointment.Id);
        events.OfType<AppointmentConfirmedDomainEvent>().Should().Contain(e => e.AppointmentId == appointment.Id);
        events.OfType<AppointmentCancelledDomainEvent>().Should().Contain(e => e.AppointmentId == appointment.Id);
    }

    [Fact]
    public async Task Duplicate_slot_and_invalid_slot_rejected()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var ctx = await SeedBookableContextAsync(client);

        var first = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(ctx.PatientId, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(9, 0), null, null));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var patient2 = await RegisterPatientAsync(client);
        var second = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(patient2.Id, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(9, 0), null, null));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var invalid = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(patient2.Id, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(9, 15), null, null));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Concurrent_booking_allows_exactly_one_success()
    {
        var setupClient = _factory.CreateClient();
        await LoginAsAdminAsync(setupClient);
        var ctx = await SeedBookableContextAsync(setupClient);
        var token = setupClient.DefaultRequestHeaders.Authorization!.Parameter!;

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.PostAsJsonAsync("/api/v1/appointments",
                new BookAppointmentRequest(ctx.PatientId, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(10, 0), null, null));
        });

        var responses = await Task.WhenAll(tasks);
        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflicts = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        created.Should().Be(1, because: string.Join(",", responses.Select(r => r.StatusCode)));
        (created + conflicts).Should().Be(20);
    }

    [Fact]
    public async Task Past_inactive_and_permission_rules()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var ctx = await SeedBookableContextAsync(client);

        var past = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(ctx.PatientId, ctx.DoctorId, ctx.ClinicId,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), new TimeOnly(9, 0), null, null));
        past.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await client.PostAsync($"/api/v1/patients/{ctx.PatientId}/deactivate", null);
        var inactivePatient = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(ctx.PatientId, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(11, 0), null, null));
        inactivePatient.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await client.PostAsync($"/api/v1/patients/{ctx.PatientId}/activate", null);

        await client.PostAsync($"/api/v1/doctors/{ctx.DoctorId}/deactivate", null);
        var inactiveDoctor = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(ctx.PatientId, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(11, 0), null, null));
        inactiveDoctor.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await client.PostAsync($"/api/v1/doctors/{ctx.DoctorId}/activate", null);

        await client.PostAsync($"/api/v1/clinics/{ctx.ClinicId}/deactivate", null);
        var inactiveClinic = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(ctx.PatientId, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(11, 0), null, null));
        inactiveClinic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await client.PostAsync($"/api/v1/clinics/{ctx.ClinicId}/activate", null);

        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        (await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email, "TempUser!123", "Inventory User", null, [AppRoles.InventoryManager]))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");
        var denied = await client.GetAsync("/api/v1/appointments/search");
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reschedule_and_no_show_work()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var ctx = await SeedBookableContextAsync(client);

        var book = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(ctx.PatientId, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(9, 0), null, null));
        var appointment = await book.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);

        var reschedule = await client.PostAsJsonAsync($"/api/v1/appointments/{appointment!.Id}/reschedule",
            new RescheduleAppointmentRequest(ctx.Date, new TimeOnly(11, 0)));
        reschedule.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var moved = await client.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{appointment.Id}", JsonOptions);
        moved!.StartTime.Should().Be(new TimeOnly(11, 0));

        await client.PostAsync($"/api/v1/appointments/{appointment.Id}/confirm", null);
        (await client.PostAsync($"/api/v1/appointments/{appointment.Id}/no-show", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var noShow = await client.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{appointment.Id}", JsonOptions);
        noShow!.Status.Should().Be(nameof(AppointmentStatus.NoShow));
    }

    [Fact]
    public async Task Cross_organization_patient_rejected()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var ctx = await SeedBookableContextAsync(client);
        var foreignPatientId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var response = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentRequest(foreignPatientId, ctx.DoctorId, ctx.ClinicId, ctx.Date, new TimeOnly(9, 0), null, null));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<BookableContext> SeedBookableContextAsync(HttpClient client)
    {
        var clinic = await CreateClinicAsync(client);
        var doctor = await RegisterDoctorAsync(client);
        (await client.PostAsJsonAsync($"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(clinic.Id, new DateOnly(2020, 1, 1), null))).EnsureSuccessStatusCode();

        var date = Next(DayOfWeek.Monday);
        (await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(9, 0), new TimeOnly(13, 0), 30, date, null))).EnsureSuccessStatusCode();

        var patient = await RegisterPatientAsync(client);
        return new BookableContext(patient.Id, doctor.Id, clinic.Id, date);
    }

    private sealed record BookableContext(Guid PatientId, Guid DoctorId, Guid ClinicId, DateOnly Date);

    private static DateOnly Next(DayOfWeek day)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var offset = ((int)day - (int)today.DayOfWeek + 7) % 7;
        if (offset == 0) offset = 7;
        return today.AddDays(offset);
    }

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
            new CreateClinicRequest($"A{Guid.NewGuid():N}"[..8], "Appt Clinic", null, "R1"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClinicDto>(JsonOptions))!;
    }

    private static async Task<DoctorDto> RegisterDoctorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/doctors",
            new RegisterDoctorRequest("Appt", $"Doc{Guid.NewGuid():N}"[..8], null, null, null, null, null, null));
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
