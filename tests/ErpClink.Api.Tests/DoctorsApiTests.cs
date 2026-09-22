using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Doctors.Application.Clinics.Models;
using ErpClink.Modules.Doctors.Application.Doctors.Models;
using ErpClink.Modules.Doctors.Application.Specialties.Models;
using ErpClink.Modules.Doctors.Domain.Clinics;
using ErpClink.Modules.Doctors.Domain.Doctors;
using ErpClink.Modules.Doctors.Infrastructure.Events;
using ErpClink.Modules.Doctors.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class DoctorsApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Guid DefaultOrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public DoctorsApiTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Complete_doctor_clinic_flow_works()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        _factory.Services.GetRequiredService<DoctorsDomainEventCollector>().Clear();

        var clinic = await CreateClinicAsync(client, $"CL{Guid.NewGuid():N}"[..8], "Main Clinic");
        var doctor = await RegisterDoctorAsync(client, "Ahmed", $"Flow{Guid.NewGuid():N}"[..8]);

        doctor.DoctorNumber.Should().StartWith("D-");
        doctor.CreatedBy.Should().NotBeNullOrWhiteSpace();
        doctor.CreatedAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));

        var assign = await client.PostAsJsonAsync(
            $"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(clinic.Id, DateOnly.FromDateTime(DateTime.UtcNow), null));
        var assignBody = await assign.Content.ReadAsStringAsync();
        assign.StatusCode.Should().Be(HttpStatusCode.Created, because: assignBody);

        var search = await client.GetAsync($"/api/v1/doctors/search?name={Uri.EscapeDataString(doctor.LastName)}");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await search.Content.ReadFromJsonAsync<PagedDoctorsResult>(JsonOptions);
        page!.Items.Should().Contain(i => i.Id == doctor.Id);

        var get = await client.GetFromJsonAsync<DoctorDto>($"/api/v1/doctors/{doctor.Id}", JsonOptions);
        get!.Id.Should().Be(doctor.Id);

        var deactivate = await client.PostAsync($"/api/v1/doctors/{doctor.Id}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await client.GetFromJsonAsync<DoctorDto>($"/api/v1/doctors/{doctor.Id}", JsonOptions);
        after!.IsActive.Should().BeFalse();

        var events = _factory.Services.GetRequiredService<DoctorsDomainEventCollector>().Events;
        events.OfType<DoctorRegisteredDomainEvent>().Should().Contain(e => e.DoctorId == doctor.Id);
        events.OfType<ClinicCreatedDomainEvent>().Should().Contain(e => e.ClinicId == clinic.Id);
        events.OfType<DoctorAssignedToClinicDomainEvent>().Should().Contain(e => e.DoctorId == doctor.Id);
        events.OfType<DoctorDeactivatedDomainEvent>().Should().Contain(e => e.DoctorId == doctor.Id);
    }

    [Fact]
    public async Task Duplicate_doctor_number_is_rejected()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var number = $"D-DUP-{Guid.NewGuid():N}"[..20];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DoctorsDbContext>();
            var doctor = Doctor.Register(DefaultOrgId, DefaultBranchId, number, null,
                "Dup", "One", null, null, null, null, null, "seed", DateTime.UtcNow);
            db.Doctors.Add(doctor);
            await db.SaveChangesAsync();
        }

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<DoctorsDbContext>();
        var duplicate = Doctor.Register(DefaultOrgId, DefaultBranchId, number, null,
            "Dup", "Two", null, null, null, null, null, "seed", DateTime.UtcNow);
        db2.Doctors.Add(duplicate);
        var act = async () => await db2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Search_update_activate_deactivate_doctor()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var doctor = await RegisterDoctorAsync(client, "Search", $"Doc{Guid.NewGuid():N}"[..8], phone: UniquePhone());
        var byNumber = await client.GetAsync($"/api/v1/doctors/by-number/{doctor.DoctorNumber}");
        byNumber.StatusCode.Should().Be(HttpStatusCode.OK);

        var update = new UpdateDoctorRequest(
            "Updated", doctor.LastName, null, null, null, doctor.PhoneNumber, "updated@example.com", null);
        var updatedResponse = await client.PutAsJsonAsync($"/api/v1/doctors/{doctor.Id}", update);
        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<DoctorDto>(JsonOptions);
        updated!.FirstName.Should().Be("Updated");
        updated.UpdatedBy.Should().NotBeNullOrWhiteSpace();

        var deactivate = await client.PostAsync($"/api/v1/doctors/{doctor.Id}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var activate = await client.PostAsync($"/api/v1/doctors/{doctor.Id}/activate", null);
        activate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await client.GetFromJsonAsync<DoctorDto>($"/api/v1/doctors/{doctor.Id}", JsonOptions);
        after!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Clinic_create_duplicate_code_update_and_lifecycle()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var code = $"C{Guid.NewGuid():N}"[..8];
        var clinic = await CreateClinicAsync(client, code, "Clinic A");
        clinic.CreatedBy.Should().NotBeNullOrWhiteSpace();

        var dup = await client.PostAsJsonAsync("/api/v1/clinics",
            new CreateClinicRequest(code, "Clinic B", null, null));
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var update = await client.PutAsJsonAsync($"/api/v1/clinics/{clinic.Id}",
            new UpdateClinicRequest("Clinic A Updated", "desc", "Room 9"));
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var deactivate = await client.PostAsync($"/api/v1/clinics/{clinic.Id}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var activate = await client.PostAsync($"/api/v1/clinics/{clinic.Id}/activate", null);
        activate.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Assignment_rules_are_enforced()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var clinic = await CreateClinicAsync(client, $"A{Guid.NewGuid():N}"[..8], "Assign Clinic");
        var doctor = await RegisterDoctorAsync(client, "Assign", $"Me{Guid.NewGuid():N}"[..8]);

        var first = await client.PostAsJsonAsync(
            $"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(clinic.Id, new DateOnly(2026, 1, 1), null));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync(
            $"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(clinic.Id, new DateOnly(2026, 2, 1), null));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var invalidDates = await client.PostAsJsonAsync(
            $"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(Guid.NewGuid(), new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1)));
        invalidDates.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await client.PostAsync($"/api/v1/doctors/{doctor.Id}/deactivate", null);
        var inactiveDoctorClinic = await CreateClinicAsync(client, $"B{Guid.NewGuid():N}"[..8], "Other Clinic");
        var inactiveAssign = await client.PostAsJsonAsync(
            $"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(inactiveDoctorClinic.Id, new DateOnly(2026, 3, 1), null));
        inactiveAssign.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await client.PostAsync($"/api/v1/doctors/{doctor.Id}/activate", null);
        await client.PostAsync($"/api/v1/clinics/{inactiveDoctorClinic.Id}/deactivate", null);
        var inactiveClinicAssign = await client.PostAsJsonAsync(
            $"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(inactiveDoctorClinic.Id, new DateOnly(2026, 3, 1), null));
        inactiveClinicAssign.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cross_organization_assignment_is_rejected()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var doctor = await RegisterDoctorAsync(client, "Cross", $"Org{Guid.NewGuid():N}"[..8]);
        Guid foreignClinicId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DoctorsDbContext>();
            var foreignClinic = Clinic.Create(
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                $"FX{Guid.NewGuid():N}"[..8],
                "Foreign Clinic",
                null,
                null,
                "seed",
                DateTime.UtcNow);
            db.Clinics.Add(foreignClinic);
            await db.SaveChangesAsync();
            foreignClinicId = foreignClinic.Id;
        }

        var response = await client.PostAsJsonAsync(
            $"/api/v1/doctors/{doctor.Id}/clinics",
            new AssignDoctorToClinicRequest(foreignClinicId, new DateOnly(2026, 1, 1), null));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unauthorized_and_missing_permissions_are_rejected()
    {
        var client = _factory.CreateClient();
        var unauth = await client.PostAsJsonAsync("/api/v1/doctors",
            new RegisterDoctorRequest("A", "B", null, null, null, null, null, null));
        unauth.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await LoginAsAdminAsync(client);
        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        var createUser = await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email, "TempUser!123", "Inventory User", null, [AppRoles.InventoryManager]));
        createUser.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");

        var doctorsView = await client.GetAsync("/api/v1/doctors/search");
        doctorsView.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var clinicsView = await client.GetAsync("/api/v1/clinics/search");
        clinicsView.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Specialty_can_be_managed_and_doctor_can_use_it()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var code = $"SP{Guid.NewGuid():N}"[..8];
        var create = await client.PostAsJsonAsync("/api/v1/specialties",
            new CreateSpecialtyRequest(code, "Cardiology", "Heart"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var specialty = await create.Content.ReadFromJsonAsync<SpecialtyDto>(JsonOptions);

        var doctor = await RegisterDoctorAsync(client, "Spec", $"Doc{Guid.NewGuid():N}"[..8], specialty!.Id);
        doctor.SpecialtyId.Should().Be(specialty.Id);
        doctor.SpecialtyName.Should().Be("Cardiology");
    }

    [Fact]
    public async Task Cross_organization_doctor_lookup_returns_not_found()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        Guid foreignDoctorId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DoctorsDbContext>();
            var foreign = Doctor.Register(
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                $"D-FX-{Guid.NewGuid():N}"[..18],
                null, "Foreign", "Doc", null, null, null, null, null, "seed", DateTime.UtcNow);
            db.Doctors.Add(foreign);
            await db.SaveChangesAsync();
            foreignDoctorId = foreign.Id;
        }

        var response = await client.GetAsync($"/api/v1/doctors/{foreignDoctorId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<ClinicDto>(body, JsonOptions)!;
    }

    private static async Task<DoctorDto> RegisterDoctorAsync(
        HttpClient client,
        string firstName,
        string lastName,
        Guid? specialtyId = null,
        string? phone = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/doctors",
            new RegisterDoctorRequest(firstName, lastName, null, specialtyId, null, phone, null, null));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<DoctorDto>(body, JsonOptions)!;
    }

    private static string UniquePhone() => $"+9665{Random.Shared.NextInt64(10000000, 99999999)}";
}
