using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using FluentAssertions;

namespace ErpClink.Api.Tests;

public sealed class PatientDocumentsApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PatientDocumentsApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Upload_list_download_update_and_deactivate_document()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var patient = await RegisterPatientAsync(client);

        var upload = await UploadPdfAsync(
            client,
            patient.Id,
            "Chest_XRay.pdf",
            "%PDF-1.4 test content",
            PatientDocumentCategory.MedicalImaging,
            PatientDocumentType.XRay);

        upload.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploaded = await upload.Content.ReadFromJsonAsync<UploadPatientDocumentResult>(JsonOptions);
        uploaded.Should().NotBeNull();
        uploaded!.Document.OriginalFileName.Should().Be("Chest_XRay.pdf");
        uploaded.Document.IsActive.Should().BeTrue();

        var list = await client.GetFromJsonAsync<PagedPatientDocumentsResult>(
            $"/api/v1/patients/{patient.Id}/documents?page=1&pageSize=20",
            JsonOptions);
        list!.TotalCount.Should().Be(1);
        list.Items.Should().ContainSingle(d => d.Id == uploaded.Document.Id);

        var summary = await client.GetFromJsonAsync<PatientDocumentSummaryDto>(
            $"/api/v1/patients/{patient.Id}/documents/summary",
            JsonOptions);
        summary!.TotalActive.Should().Be(1);
        summary.ByCategory.Should().Contain(c => c.Category == PatientDocumentCategory.MedicalImaging && c.Count == 1);

        var download = await client.GetAsync(
            $"/api/v1/patients/{patient.Id}/documents/{uploaded.Document.Id}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        (await download.Content.ReadAsStringAsync()).Should().Contain("%PDF-1.4");

        var update = await client.PutAsJsonAsync(
            $"/api/v1/patients/{patient.Id}/documents/{uploaded.Document.Id}",
            new UpdatePatientDocumentMetadataRequest(
                PatientDocumentCategory.MedicalImaging,
                PatientDocumentType.XRay,
                new DateOnly(2026, 9, 14),
                "External chest film",
                null));
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var deactivate = await client.PostAsync(
            $"/api/v1/patients/{patient.Id}/documents/{uploaded.Document.Id}/deactivate",
            null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var activeList = await client.GetFromJsonAsync<PagedPatientDocumentsResult>(
            $"/api/v1/patients/{patient.Id}/documents?isActive=true",
            JsonOptions);
        activeList!.TotalCount.Should().Be(0);

        var inactiveList = await client.GetFromJsonAsync<PagedPatientDocumentsResult>(
            $"/api/v1/patients/{patient.Id}/documents?isActive=false",
            JsonOptions);
        inactiveList!.Items.Should().ContainSingle(d => d.Id == uploaded.Document.Id && !d.IsActive);
    }

    [Fact]
    public async Task Rejects_unsupported_extension_and_oversized_file()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var patient = await RegisterPatientAsync(client);

        var exe = await UploadBytesAsync(
            client,
            patient.Id,
            "malware.exe",
            Encoding.UTF8.GetBytes("MZ"),
            "application/octet-stream",
            PatientDocumentCategory.Other,
            PatientDocumentType.Other);
        exe.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var large = new byte[11 * 1024 * 1024];
        var tooLarge = await UploadBytesAsync(
            client,
            patient.Id,
            "large.pdf",
            large,
            "application/pdf",
            PatientDocumentCategory.Other,
            PatientDocumentType.Other);
        tooLarge.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Duplicate_name_and_size_returns_conflict_unless_allowed()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var patient = await RegisterPatientAsync(client);

        var first = await UploadPdfAsync(
            client,
            patient.Id,
            "same.pdf",
            "same-bytes",
            PatientDocumentCategory.Other,
            PatientDocumentType.Other);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await UploadPdfAsync(
            client,
            patient.Id,
            "same.pdf",
            "same-bytes",
            PatientDocumentCategory.Other,
            PatientDocumentType.Other);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var allowed = await UploadPdfAsync(
            client,
            patient.Id,
            "same.pdf",
            "same-bytes",
            PatientDocumentCategory.Other,
            PatientDocumentType.Other,
            allowPossibleDuplicate: true);
        allowed.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task User_without_documents_permission_cannot_list()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var patient = await RegisterPatientAsync(client);

        var email = $"inv-doc-{Guid.NewGuid():N}@erpclink.local";
        var createUser = await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email,
            "TempUser!123",
            "Inventory User",
            null,
            [AppRoles.InventoryManager]));
        createUser.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");

        var response = await client.GetAsync($"/api/v1/patients/{patient.Id}/documents");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<HttpResponseMessage> UploadPdfAsync(
        HttpClient client,
        Guid patientId,
        string fileName,
        string content,
        PatientDocumentCategory category,
        PatientDocumentType type,
        bool allowPossibleDuplicate = false) =>
        await UploadBytesAsync(
            client,
            patientId,
            fileName,
            Encoding.UTF8.GetBytes(content),
            "application/pdf",
            category,
            type,
            allowPossibleDuplicate);

    private static async Task<HttpResponseMessage> UploadBytesAsync(
        HttpClient client,
        Guid patientId,
        string fileName,
        byte[] bytes,
        string contentType,
        PatientDocumentCategory category,
        PatientDocumentType type,
        bool allowPossibleDuplicate = false)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(((int)category).ToString()), "category");
        form.Add(new StringContent(((int)type).ToString()), "documentType");
        form.Add(new StringContent(allowPossibleDuplicate ? "true" : "false"), "allowPossibleDuplicate");
        return await client.PostAsync($"/api/v1/patients/{patientId}/documents", form);
    }

    private async Task<PatientDto> RegisterPatientAsync(HttpClient client)
    {
        var phone = $"+9665{Random.Shared.NextInt64(10000000, 99999999)}";
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterPatientRequest(
            $"Doc{suffix}",
            null,
            $"Patient{suffix}",
            new DateOnly(1992, 3, 10),
            Gender.Male,
            null,
            phone,
            $"{Guid.NewGuid():N}@example.com",
            null,
            null,
            null,
            null,
            null,
            false);

        var response = await client.PostAsJsonAsync("/api/v1/patients", request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        var result = JsonSerializer.Deserialize<RegisterPatientResult>(body, JsonOptions)!;
        return result.Patient;
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
