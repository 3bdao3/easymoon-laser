using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Doctors.Application.Clinics.Models;
using ErpClink.Modules.Doctors.Application.Doctors.Models;
using ErpClink.Modules.Scheduling.Application.Schedules.Models;
using ErpClink.Modules.Scheduling.Domain.Schedules;
using ErpClink.Modules.Scheduling.Infrastructure.Events;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class SchedulingApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SchedulingApiTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Complete_schedule_and_availability_flow_works()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        _factory.Services.GetRequiredService<SchedulingDomainEventCollector>().Clear();

        var (doctor, clinic) = await CreateDoctorClinicAssignedAsync(client);
        var monday = Next(DayOfWeek.Monday);

        var create = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(
                doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(9, 0), new TimeOnly(13, 0), 30,
                monday, null));
        var createBody = await create.Content.ReadAsStringAsync();
        create.StatusCode.Should().Be(HttpStatusCode.Created, because: createBody);
        var schedule = JsonSerializer.Deserialize<DoctorScheduleDto>(createBody, JsonOptions)!;
        schedule.CreatedBy.Should().NotBeNullOrWhiteSpace();
        schedule.CreatedAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));

        var list = await client.GetFromJsonAsync<List<DoctorScheduleDto>>(
            $"/api/v1/scheduling/doctors/{doctor.Id}/schedules", JsonOptions);
        list!.Should().Contain(s => s.Id == schedule.Id);

        var availability = await client.GetFromJsonAsync<DoctorAvailabilityDto>(
            $"/api/v1/scheduling/doctors/{doctor.Id}/availability?date={monday:yyyy-MM-dd}&clinicId={clinic.Id}",
            JsonOptions);
        availability!.Slots.Select(s => s.Start).Should().Equal(
            new TimeOnly(9, 0), new TimeOnly(9, 30), new TimeOnly(10, 0), new TimeOnly(10, 30),
            new TimeOnly(11, 0), new TimeOnly(11, 30), new TimeOnly(12, 0), new TimeOnly(12, 30));

        var deactivate = await client.PostAsync($"/api/v1/scheduling/schedules/{schedule.Id}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await client.GetFromJsonAsync<DoctorScheduleDto>(
            $"/api/v1/scheduling/schedules/{schedule.Id}", JsonOptions);
        after!.IsActive.Should().BeFalse();

        var events = _factory.Services.GetRequiredService<SchedulingDomainEventCollector>().Events;
        events.OfType<DoctorScheduleCreatedDomainEvent>().Should().Contain(e => e.ScheduleId == schedule.Id);
        events.OfType<DoctorScheduleDeactivatedDomainEvent>().Should().Contain(e => e.ScheduleId == schedule.Id);
    }

    [Fact]
    public async Task Validation_and_overlap_rules_are_enforced()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var (doctor, clinic) = await CreateDoctorClinicAssignedAsync(client);
        var monday = Next(DayOfWeek.Monday);

        var invalidTime = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(13, 0), new TimeOnly(9, 0), 30, monday, null));
        invalidTime.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var invalidDuration = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(9, 0), new TimeOnly(13, 0), 0, monday, null));
        invalidDuration.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var first = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(9, 0), new TimeOnly(13, 0), 30, monday, null));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var overlap = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(12, 0), new TimeOnly(15, 0), 30, monday, null));
        overlap.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var evening = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(17, 0), new TimeOnly(21, 0), 30, monday, null));
        evening.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Incomplete_slot_and_multiple_periods_availability()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var (doctor, clinic) = await CreateDoctorClinicAssignedAsync(client);
        var tuesday = Next(DayOfWeek.Tuesday);

        await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Tuesday,
                new TimeOnly(9, 0), new TimeOnly(10, 15), 30, tuesday, null));
        await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Tuesday,
                new TimeOnly(14, 0), new TimeOnly(16, 0), 30, tuesday, null));

        // Replace morning with 09-11 and keep afternoon via third period on Wednesday pattern:
        // For Tuesday we have 09-10:15 and 14-16 — check incomplete + afternoon
        var availability = await client.GetFromJsonAsync<DoctorAvailabilityDto>(
            $"/api/v1/scheduling/doctors/{doctor.Id}/availability?date={tuesday:yyyy-MM-dd}&clinicId={clinic.Id}",
            JsonOptions);
        availability!.Slots.Select(s => s.Start).Should().Equal(
            new TimeOnly(9, 0), new TimeOnly(9, 30),
            new TimeOnly(14, 0), new TimeOnly(14, 30), new TimeOnly(15, 0), new TimeOnly(15, 30));
    }

    [Fact]
    public async Task Inactive_doctor_or_clinic_or_missing_assignment_rejected()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var (doctor, clinic) = await CreateDoctorClinicAssignedAsync(client);
        var monday = Next(DayOfWeek.Monday);

        await client.PostAsync($"/api/v1/doctors/{doctor.Id}/deactivate", null);
        var inactiveDoctor = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(9, 0), new TimeOnly(12, 0), 30, monday, null));
        inactiveDoctor.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await client.PostAsync($"/api/v1/doctors/{doctor.Id}/activate", null);
        await client.PostAsync($"/api/v1/clinics/{clinic.Id}/deactivate", null);
        var inactiveClinic = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(9, 0), new TimeOnly(12, 0), 30, monday, null));
        inactiveClinic.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await client.PostAsync($"/api/v1/clinics/{clinic.Id}/activate", null);
        await client.PostAsync($"/api/v1/doctors/{doctor.Id}/clinics/{clinic.Id}/deactivate", null);
        var noAssignment = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Monday,
                new TimeOnly(9, 0), new TimeOnly(12, 0), 30, monday, null));
        noAssignment.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cross_organization_schedule_rejected()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var doctor = await RegisterDoctorAsync(client, "Cross", $"Sch{Guid.NewGuid():N}"[..8]);
        var foreignClinicId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var response = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, foreignClinicId, DayOfWeek.Monday,
                new TimeOnly(9, 0), new TimeOnly(12, 0), 30, Next(DayOfWeek.Monday), null));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Activate_deactivate_and_permissions()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var (doctor, clinic) = await CreateDoctorClinicAssignedAsync(client);
        var wednesday = Next(DayOfWeek.Wednesday);

        var created = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Wednesday,
                new TimeOnly(9, 0), new TimeOnly(11, 0), 30, wednesday, null));
        var schedule = await created.Content.ReadFromJsonAsync<DoctorScheduleDto>(JsonOptions);

        await client.PostAsync($"/api/v1/scheduling/schedules/{schedule!.Id}/deactivate", null);
        var activate = await client.PostAsync($"/api/v1/scheduling/schedules/{schedule.Id}/activate", null);
        activate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        var createUser = await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email, "TempUser!123", "Inventory User", null, [AppRoles.InventoryManager]));
        createUser.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");

        var viewDenied = await client.GetAsync($"/api/v1/scheduling/doctors/{doctor.Id}/schedules");
        viewDenied.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var createDenied = await client.PostAsJsonAsync("/api/v1/scheduling/schedules",
            new CreateDoctorScheduleRequest(doctor.Id, clinic.Id, DayOfWeek.Thursday,
                new TimeOnly(9, 0), new TimeOnly(10, 0), 30, Next(DayOfWeek.Thursday), null));
        createDenied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(DoctorDto Doctor, ClinicDto Clinic)> CreateDoctorClinicAssignedAsync(HttpClient client)
    {
        var clinic = await CreateClinicAsync(client, $"S{Guid.NewGuid():N}"[..8], "Sched Clinic");
        var doctor = await RegisterDoctorAsync(client, "Sched", $"Doc{Guid.NewGuid():N}"[..8]);
        var assign = await client.PostAsJsonAsync(
            $"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(clinic.Id, new DateOnly(2020, 1, 1), null));
        assign.EnsureSuccessStatusCode();
        return (doctor, clinic);
    }

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

    private static async Task<ClinicDto> CreateClinicAsync(HttpClient client, string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/clinics",
            new CreateClinicRequest(code, name, null, "Room 1"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClinicDto>(JsonOptions))!;
    }

    private static async Task<DoctorDto> RegisterDoctorAsync(HttpClient client, string firstName, string lastName)
    {
        var response = await client.PostAsJsonAsync("/api/v1/doctors",
            new RegisterDoctorRequest(firstName, lastName, null, null, null, null, null, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DoctorDto>(JsonOptions))!;
    }
}
