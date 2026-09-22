using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using ErpClink.Modules.Patients.Infrastructure.Events;
using ErpClink.Modules.Patients.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class PatientsApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PatientsApiTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_patient_succeeds_and_generates_number_and_audit_and_event()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        _factory.Services.GetRequiredService<DomainEventCollector>().Clear();

        var request = NewRegisterRequest(phone: UniquePhone());
        var response = await client.PostAsJsonAsync("/api/v1/patients", request);
        var bodyText = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: bodyText);

        var result = JsonSerializer.Deserialize<RegisterPatientResult>(bodyText, JsonOptions);
        result.Should().NotBeNull();
        result!.Patient.PatientNumber.Should().StartWith("P-");
        result.Patient.CreatedBy.Should().NotBeNullOrWhiteSpace();
        result.Patient.CreatedAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
        result.Patient.FirstName.Should().Be(request.FirstName);

        var events = _factory.Services.GetRequiredService<DomainEventCollector>().Events;
        events.OfType<PatientRegisteredDomainEvent>().Should().Contain(e => e.PatientId == result.Patient.Id);
    }

    [Fact]
    public async Task Required_field_validation_fails()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var request = NewRegisterRequest(phone: UniquePhone()) with { FirstName = "" };
        var response = await client.PostAsJsonAsync("/api/v1/patients", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patient_can_be_retrieved_searched_and_updated()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var created = await RegisterAsync(client, NewRegisterRequest(phone: UniquePhone(), lastName: $"SearchableLast{Guid.NewGuid():N}"[..20]));
        var byId = await client.GetAsync($"/api/v1/patients/{created.Patient.Id}");
        byId.StatusCode.Should().Be(HttpStatusCode.OK);

        var byNumber = await client.GetAsync($"/api/v1/patients/by-number/{created.Patient.PatientNumber}");
        byNumber.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchLast = created.Patient.LastName;
        var search = await client.GetAsync($"/api/v1/patients/search?lastName={Uri.EscapeDataString(searchLast)}&page=1&pageSize=20");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await search.Content.ReadFromJsonAsync<PagedPatientsResult>(JsonOptions);
        page!.Items.Should().Contain(i => i.Id == created.Patient.Id);

        var update = new UpdatePatientRequest(
            "Updated",
            null,
            created.Patient.LastName,
            created.Patient.DateOfBirth,
            Gender.Female,
            null,
            created.Patient.PhoneNumber,
            "updated@example.com",
            null,
            null,
            null,
            null,
            null);

        var updatedResponse = await client.PutAsJsonAsync($"/api/v1/patients/{created.Patient.Id}", update);
        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<PatientDto>(JsonOptions);
        updated!.FirstName.Should().Be("Updated");
        updated.UpdatedBy.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Patient_can_be_deactivated_and_inactive_cannot_be_updated()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var created = await RegisterAsync(client, NewRegisterRequest(phone: UniquePhone()));

        var deactivate = await client.PostAsync($"/api/v1/patients/{created.Patient.Id}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await client.GetFromJsonAsync<PatientDto>($"/api/v1/patients/{created.Patient.Id}", JsonOptions);
        get!.IsActive.Should().BeFalse();

        var update = new UpdatePatientRequest(
            created.Patient.FirstName,
            null,
            created.Patient.LastName,
            created.Patient.DateOfBirth,
            created.Patient.Gender,
            null,
            created.Patient.PhoneNumber,
            null,
            null,
            null,
            null,
            null,
            null);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/patients/{created.Patient.Id}", update);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Allergy_can_be_added_updated_and_deactivated()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var created = await RegisterAsync(client, NewRegisterRequest(phone: UniquePhone()));

        var add = await client.PostAsJsonAsync(
            $"/api/v1/patients/{created.Patient.Id}/allergies",
            new AddAllergyRequest("Penicillin", "Rash", AllergySeverity.Moderate, "note"));
        var addBody = await add.Content.ReadAsStringAsync();
        add.StatusCode.Should().Be(HttpStatusCode.Created, because: addBody);
        var allergy = await add.Content.ReadFromJsonAsync<AllergyDto>(JsonOptions);

        var update = await client.PutAsJsonAsync(
            $"/api/v1/patients/{created.Patient.Id}/allergies/{allergy!.Id}",
            new UpdateAllergyRequest("Penicillin", "Severe rash", AllergySeverity.Severe, "updated"));
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var deactivate = await client.PostAsync(
            $"/api/v1/patients/{created.Patient.Id}/allergies/{allergy.Id}/deactivate",
            null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await client.GetFromJsonAsync<List<AllergyDto>>(
            $"/api/v1/patients/{created.Patient.Id}/allergies",
            JsonOptions);
        list!.Single(a => a.Id == allergy.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Unauthorized_user_cannot_create_patient()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/patients", NewRegisterRequest(phone: UniquePhone()));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task User_without_Patients_View_cannot_retrieve_patients()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        var createUser = await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email,
            "TempUser!123",
            "Inventory User",
            null,
            [AppRoles.InventoryManager]));
        createUser.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");

        var response = await client.GetAsync("/api/v1/patients/search");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cross_organization_patient_access_returns_not_found()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var created = await RegisterAsync(client, NewRegisterRequest(phone: UniquePhone()));

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [patients].[Patients] SET [OrganizationId] = {Guid.Parse("99999999-9999-9999-9999-999999999999")} WHERE [Id] = {created.Patient.Id}");
        }

        var response = await client.GetAsync($"/api/v1/patients/{created.Patient.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Duplicate_protection_returns_conflict_unless_allowed()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var phone = UniquePhone();
        var first = NewRegisterRequest(phone: phone, firstName: "Dup", lastName: "Person", dob: new DateOnly(1991, 2, 2));
        (await client.PostAsJsonAsync("/api/v1/patients", first)).EnsureSuccessStatusCode();

        var conflict = await client.PostAsJsonAsync("/api/v1/patients", first);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var allowed = first with { AllowPossibleDuplicate = true };
        var forced = await client.PostAsJsonAsync("/api/v1/patients", allowed);
        forced.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task National_id_uniqueness_is_enforced()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var nationalId = $"NID-{Guid.NewGuid():N}"[..20];
        var first = NewRegisterRequest(phone: UniquePhone(), nationalId: nationalId);
        (await client.PostAsJsonAsync("/api/v1/patients", first)).EnsureSuccessStatusCode();

        var second = NewRegisterRequest(phone: UniquePhone(), nationalId: nationalId, allowDuplicate: true);
        var response = await client.PostAsJsonAsync("/api/v1/patients", second);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static RegisterPatientRequest NewRegisterRequest(
        string phone,
        string? firstName = null,
        string? lastName = null,
        DateOnly? dob = null,
        string? nationalId = null,
        bool allowDuplicate = false)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new(
            firstName ?? $"Amina{suffix}",
            null,
            lastName ?? $"Khaled{suffix}",
            dob ?? new DateOnly(1990, 1, 15),
            Gender.Female,
            nationalId,
            phone,
            $"{Guid.NewGuid():N}@example.com",
            "Street 1",
            null,
            "Riyadh",
            "Emergency Contact",
            "+966511111111",
            allowDuplicate);
    }

    private static string UniquePhone() => $"+9665{Random.Shared.NextInt64(10000000, 99999999)}";

    private async Task<RegisterPatientResult> RegisterAsync(HttpClient client, RegisterPatientRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/patients", request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<RegisterPatientResult>(body, JsonOptions)!;
    }

    private async Task LoginAsAdminAsync(HttpClient client) =>
        await LoginAsync(client, "admin@erpclink.local", "ChangeMe!Admin123");

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }
}
