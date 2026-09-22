using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Billing.Application.Invoices.Models;
using ErpClink.Modules.Billing.Application.Payments.Models;
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using ErpClink.Modules.Services.Application.Catalog.Models;
using FluentAssertions;

namespace ErpClink.Api.Tests;

public sealed class BillingApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BillingApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Issued_invoice_keeps_price_snapshot_after_catalog_change()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var patient = await RegisterPatientAsync(client);
        var svc = await CreateServiceAsync(client, $"BIL-{Guid.NewGuid():N}"[..10], "Bill Svc", 200m);

        var draft = await CreateDraftAsync(client, patient.Patient.Id, svc.Id, 200m);
        var issued = await IssueAsync(client, draft.Id, draft.RowVersion);
        issued.Lines.Single().UnitPrice.Should().Be(200m);

        await client.PutAsJsonAsync($"/api/v1/services/{svc.Id}",
            new UpdateHealthcareServiceRequest("Bill Svc", null, null, 999m, null, null, svc.RowVersion));

        var loaded = await client.GetFromJsonAsync<InvoiceDto>($"/api/v1/billing/invoices/{issued.Id}", JsonOptions);
        loaded!.Lines.Single().UnitPrice.Should().Be(200m);
    }

    [Fact]
    public async Task Partial_payments_until_paid_and_overpayment_rejected()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var patient = await RegisterPatientAsync(client);
        var svc = await CreateServiceAsync(client, $"PAY-{Guid.NewGuid():N}"[..10], "Pay Svc", 500m);
        var draft = await CreateDraftAsync(client, patient.Patient.Id, svc.Id, 500m);
        var issued = await IssueAsync(client, draft.Id, draft.RowVersion);

        var p1 = await RecordPaymentAsync(client, issued.Id, 200m, issued.RowVersion);
        p1.StatusCode.Should().Be(HttpStatusCode.Created);
        issued = await ReloadInvoiceAsync(client, issued.Id);
        issued.Status.Should().Be("PartiallyPaid");

        var p2 = await RecordPaymentAsync(client, issued.Id, 300m, issued.RowVersion);
        p2.StatusCode.Should().Be(HttpStatusCode.Created);
        issued = await ReloadInvoiceAsync(client, issued.Id);
        issued.Status.Should().Be("Paid");

        var over = await RecordPaymentAsync(client, issued.Id, 1m, issued.RowVersion);
        over.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Concurrent_issue_produces_unique_invoice_numbers()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        var draftIds = new List<(Guid Id, byte[] RowVersion)>();
        for (var i = 0; i < 20; i++)
        {
            var patient = await RegisterPatientAsync(client);
            var svc = await CreateServiceAsync(client, $"CI-{Guid.NewGuid():N}"[..10], "CI", 10m);
            var draft = await CreateDraftAsync(client, patient.Patient.Id, svc.Id, 10m);
            draftIds.Add((draft.Id, draft.RowVersion));
        }

        var tasks = draftIds.Select(async d =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await c.PostAsync($"/api/v1/billing/invoices/{d.Id}/issue", null);
        });
        var responses = await Task.WhenAll(tasks);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        var numbers = new List<string>();
        foreach (var d in draftIds)
        {
            var inv = await client.GetFromJsonAsync<InvoiceDto>($"/api/v1/billing/invoices/{d.Id}", JsonOptions);
            numbers.Add(inv!.InvoiceNumber!);
        }

        numbers.Distinct().Should().HaveCount(20);
    }

    [Fact]
    public async Task Concurrent_payments_never_overpay_outstanding()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        var patient = await RegisterPatientAsync(client);
        var svc = await CreateServiceAsync(client, $"CP-{Guid.NewGuid():N}"[..10], "CP", 500m);
        var draft = await CreateDraftAsync(client, patient.Patient.Id, svc.Id, 500m);
        var issued = await IssueAsync(client, draft.Id, draft.RowVersion);

        var tasks = Enumerable.Range(0, 30).Select(async _ =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var current = await c.GetFromJsonAsync<InvoiceDto>($"/api/v1/billing/invoices/{issued.Id}", JsonOptions);
            return await c.PostAsJsonAsync("/api/v1/billing/payments",
                new RecordPaymentRequest(issued.Id, DateOnly.FromDateTime(DateTime.UtcNow), 400m, "Cash", null, null, current!.RowVersion));
        });
        var responses = await Task.WhenAll(tasks);
        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var rejected = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);
        created.Should().BeGreaterThanOrEqualTo(1);
        (created + rejected + responses.Count(r => r.StatusCode == HttpStatusCode.Conflict)).Should().Be(30);

        var final = await ReloadInvoiceAsync(client, issued.Id);
        final.OutstandingAmount.Should().BeGreaterThanOrEqualTo(0);
        final.PaidAmount.Should().BeLessThanOrEqualTo(500m);
    }

    [Fact]
    public async Task Inventory_manager_forbidden_on_billing()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        (await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email, "TempUser!123", "Inventory", null, [AppRoles.InventoryManager]))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");
        (await client.GetAsync("/api/v1/billing/invoices/search")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Payment_concurrency_conflict_on_stale_row_version()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var patient = await RegisterPatientAsync(client);
        var svc = await CreateServiceAsync(client, $"RV-{Guid.NewGuid():N}"[..10], "RV", 100m);
        var draft = await CreateDraftAsync(client, patient.Patient.Id, svc.Id, 100m);
        var issued = await IssueAsync(client, draft.Id, draft.RowVersion);
        var stale = issued.RowVersion;

        await RecordPaymentAsync(client, issued.Id, 50m, issued.RowVersion);
        var conflict = await RecordPaymentAsync(client, issued.Id, 10m, stale);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<HttpResponseMessage> RecordPaymentAsync(HttpClient client, Guid invoiceId, decimal amount, byte[] rowVersion) =>
        await client.PostAsJsonAsync("/api/v1/billing/payments",
            new RecordPaymentRequest(invoiceId, DateOnly.FromDateTime(DateTime.UtcNow), amount, "Cash", null, null, rowVersion));

    private async Task<InvoiceDto> ReloadInvoiceAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<InvoiceDto>($"/api/v1/billing/invoices/{id}", JsonOptions))!;

    private async Task<InvoiceDto> CreateDraftAsync(HttpClient client, Guid patientId, Guid serviceId, decimal _)
    {
        var response = await client.PostAsJsonAsync("/api/v1/billing/invoices",
            new CreateDraftInvoiceRequest(
                patientId, null, DateOnly.FromDateTime(DateTime.UtcNow), "EGP", null, null,
                [new InvoiceLineInput(serviceId, null, 1, null, 1)]));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<InvoiceDto>(body, JsonOptions)!;
    }

    private async Task<InvoiceDto> IssueAsync(HttpClient client, Guid id, byte[]? rowVersion = null)
    {
        var url = rowVersion is { Length: > 0 }
            ? $"/api/v1/billing/invoices/{id}/issue?rowVersion={Uri.EscapeDataString(Convert.ToBase64String(rowVersion))}"
            : $"/api/v1/billing/invoices/{id}/issue";
        var response = await client.PostAsync(url, null);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
        return JsonSerializer.Deserialize<InvoiceDto>(body, JsonOptions)!;
    }

    private async Task<HealthcareServiceDto> CreateServiceAsync(HttpClient client, string code, string name, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/v1/services",
            new CreateHealthcareServiceRequest(code, name, null, null, price, "EGP", null));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<HealthcareServiceDto>(body, JsonOptions)!;
    }

    private async Task<RegisterPatientResult> RegisterPatientAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var phone = $"+9665{Random.Shared.NextInt64(10000000, 99999999)}";
        var response = await client.PostAsJsonAsync("/api/v1/patients",
            new RegisterPatientRequest(
                $"Bill{suffix}", null, $"Patient{suffix}", new DateOnly(1990, 1, 1), Gender.Male,
                null, phone, null, null, null, null, null, null, true));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<RegisterPatientResult>(body, JsonOptions)!;
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
}
