using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Procurement.Application.PurchaseOrders.Models;
using ErpClink.Modules.Procurement.Application.Suppliers.Models;
using FluentAssertions;

namespace ErpClink.Api.Tests;

public sealed class ProcurementApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProcurementApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Supplier_crud_and_search()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var created = await CreateSupplierAsync(client, "Pharma Supplies Co");
        created.SupplierCode.Should().StartWith("SUP-");
        created.IsActive.Should().BeTrue();

        var loaded = await client.GetFromJsonAsync<SupplierDto>($"/api/v1/procurement/suppliers/{created.Id}", JsonOptions);
        loaded!.Name.Should().Be("Pharma Supplies Co");

        var updated = await client.PutAsJsonAsync($"/api/v1/procurement/suppliers/{created.Id}",
            new UpdateSupplierRequest("Pharma Supplies Updated", null, null, null, null, null, created.RowVersion));
        updated.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Inventory_manager_forbidden_on_create_supplier()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        (await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email, "TempUser!123", "Inventory", null, [AppRoles.InventoryManager]))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");

        var response = await client.PostAsJsonAsync("/api/v1/procurement/suppliers",
            new CreateSupplierRequest("Blocked Supplier", null, null, null, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Purchase_order_submit_approve_cancel()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var supplier = await CreateSupplierAsync(client, "PO Supplier");
        var draft = await CreatePoAsync(client, supplier.Id, 100m);
        draft.Status.Should().Be("Draft");

        var submitted = await SubmitPoAsync(client, draft.Id, draft.RowVersion);
        submitted.Status.Should().Be("Submitted");

        var approved = await ApprovePoAsync(client, submitted.Id, submitted.RowVersion);
        approved.Status.Should().Be("Approved");

        var cancelled = await client.PostAsJsonAsync($"/api/v1/procurement/purchase-orders/{approved.Id}/cancel",
            new CancelPurchaseOrderRequest("No longer needed", approved.RowVersion));
        cancelled.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Wrong_org_supplier_not_found_on_po()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var response = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders",
            new CreateDraftPurchaseOrderRequest(
                Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), "EGP", null, null,
                [new PurchaseOrderLineInputDto(null, "Item", 1, 10, null, 1)]));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Concurrent_po_create_produces_unique_numbers()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var supplier = await CreateSupplierAsync(client, "Concurrent PO Supplier");

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
            return await CreatePoAsync(c, supplier.Id, 1m);
        });
        var orders = await Task.WhenAll(tasks);
        orders.Select(o => o.PurchaseOrderNumber).Distinct().Should().HaveCount(20);
    }

    [Fact]
    public async Task Concurrent_supplier_create_produces_unique_codes()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await CreateSupplierAsync(c, $"Supplier {i}");
        });
        var suppliers = await Task.WhenAll(tasks);
        suppliers.Select(s => s.SupplierCode).Distinct().Should().HaveCount(20);
    }

    [Fact]
    public async Task Stale_row_version_on_po_update_returns_conflict()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var supplier = await CreateSupplierAsync(client, "RV Supplier");
        var draft = await CreatePoAsync(client, supplier.Id, 50m);
        var stale = draft.RowVersion;

        await client.PutAsJsonAsync($"/api/v1/procurement/purchase-orders/{draft.Id}",
            new UpdateDraftPurchaseOrderRequest(supplier.Id, draft.OrderDate, "note", null, draft.RowVersion));

        var conflict = await client.PutAsJsonAsync($"/api/v1/procurement/purchase-orders/{draft.Id}",
            new UpdateDraftPurchaseOrderRequest(supplier.Id, draft.OrderDate, "stale", null, stale));
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Purchase_order_keeps_unit_cost_snapshot()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var supplier = await CreateSupplierAsync(client, "Snapshot Supplier");
        var catalogId = Guid.NewGuid();

        var po1 = await CreatePoAsync(client, supplier.Id, 50m, catalogId);
        po1.Lines.Single().UnitCost.Should().Be(50m);

        var po2 = await CreatePoAsync(client, supplier.Id, 99m, catalogId);
        po2.Lines.Single().UnitCost.Should().Be(99m);

        var reloaded = await client.GetFromJsonAsync<PurchaseOrderDto>($"/api/v1/procurement/purchase-orders/{po1.Id}", JsonOptions);
        reloaded!.Lines.Single().UnitCost.Should().Be(50m);
    }

    private async Task<SupplierDto> CreateSupplierAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/procurement/suppliers",
            new CreateSupplierRequest(name, null, null, null, null, null));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<SupplierDto>(body, JsonOptions)!;
    }

    private async Task<PurchaseOrderDto> CreatePoAsync(HttpClient client, Guid supplierId, decimal unitCost, Guid? catalogItemId = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders",
            new CreateDraftPurchaseOrderRequest(
                supplierId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "EGP",
                null,
                null,
                [new PurchaseOrderLineInputDto(catalogItemId, "Supply item", 1, unitCost, null, 1)]));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<PurchaseOrderDto>(body, JsonOptions)!;
    }

    private async Task<PurchaseOrderDto> SubmitPoAsync(HttpClient client, Guid id, byte[] rowVersion)
    {
        var url = $"/api/v1/procurement/purchase-orders/{id}/submit?rowVersion={Uri.EscapeDataString(Convert.ToBase64String(rowVersion))}";
        var response = await client.PostAsync(url, null);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
        return JsonSerializer.Deserialize<PurchaseOrderDto>(body, JsonOptions)!;
    }

    private async Task<PurchaseOrderDto> ApprovePoAsync(HttpClient client, Guid id, byte[] rowVersion)
    {
        var url = $"/api/v1/procurement/purchase-orders/{id}/approve?rowVersion={Uri.EscapeDataString(Convert.ToBase64String(rowVersion))}";
        var response = await client.PostAsync(url, null);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
        return JsonSerializer.Deserialize<PurchaseOrderDto>(body, JsonOptions)!;
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
