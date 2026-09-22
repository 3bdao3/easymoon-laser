using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Inventory.Application.GoodsReceipts.Models;
using ErpClink.Modules.Inventory.Application.Items.Models;
using ErpClink.Modules.Inventory.Application.Stock.Models;
using ErpClink.Modules.Inventory.Application.Warehouses.Models;
using ErpClink.Modules.Procurement.Application.PurchaseOrders.Models;
using ErpClink.Modules.Procurement.Application.Suppliers.Models;
using FluentAssertions;

namespace ErpClink.Api.Tests;

public sealed class InventoryApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InventoryApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Warehouse_and_item_crud_with_unique_codes()
    {
        var client = await AdminClientAsync();
        var wh = await CreateWarehouseAsync(client, "Main Store");
        wh.WarehouseCode.Should().StartWith("WH-");

        var item = await CreateItemAsync(client, "Gloves", false);
        item.ItemCode.Should().StartWith("ITM-");

        var loaded = await client.GetFromJsonAsync<InventoryItemDto>($"/api/v1/inventory/items/{item.Id}", JsonOptions);
        loaded!.Name.Should().Be("Gloves");
    }

    [Fact]
    public async Task Receive_approved_po_increases_stock_and_movement()
    {
        var client = await AdminClientAsync();
        var supplier = await CreateSupplierAsync(client);
        var po = await ApprovePoAsync(client, supplier.Id, 10);
        var wh = await CreateWarehouseAsync(client, "Recv WH");
        var item = await CreateItemAsync(client, "Supply Item", false);

        var gr = await ReceiveAsync(client, po, wh.Id, item.Id, po.Lines.Single().Id, 10);
        gr.Status.Should().Be("Posted");

        var balances = await client.GetFromJsonAsync<PagedStockBalancesResult>(
            $"/api/v1/inventory/stock/balances?warehouseId={wh.Id}&inventoryItemId={item.Id}", JsonOptions);
        balances!.Items.Single().Quantity.Should().Be(10);
    }

    [Fact]
    public async Task Draft_po_cannot_be_received()
    {
        var client = await AdminClientAsync();
        var supplier = await CreateSupplierAsync(client);
        var draft = await CreatePoAsync(client, supplier.Id, 5);
        var wh = await CreateWarehouseAsync(client, "WH");
        var item = await CreateItemAsync(client, "X", false);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/goods-receipts/from-purchase-order",
            new CreateGoodsReceiptFromPoRequest(
                draft.Id, wh.Id, DateOnly.FromDateTime(DateTime.UtcNow), null, draft.RowVersion,
                [new CreateGoodsReceiptLineRequest(draft.Lines.Single().Id, item.Id, 1, null, null)]));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Partial_then_final_receipt_updates_po_status()
    {
        var client = await AdminClientAsync();
        var supplier = await CreateSupplierAsync(client);
        var po = await ApprovePoAsync(client, supplier.Id, 10);
        var wh = await CreateWarehouseAsync(client, "Partial WH");
        var item = await CreateItemAsync(client, "Partial Item", false);
        var lineId = po.Lines.Single().Id;

        await ReceiveAsync(client, po, wh.Id, item.Id, lineId, 4);
        po = (await client.GetFromJsonAsync<PurchaseOrderDto>($"/api/v1/procurement/purchase-orders/{po.Id}", JsonOptions))!;
        po.Status.Should().Be("PartiallyReceived");

        await ReceiveAsync(client, po, wh.Id, item.Id, lineId, 6);
        po = await client.GetFromJsonAsync<PurchaseOrderDto>($"/api/v1/procurement/purchase-orders/{po.Id}", JsonOptions);
        po!.Status.Should().Be("Received");
    }

    [Fact]
    public async Task Over_receive_rejected()
    {
        var client = await AdminClientAsync();
        var supplier = await CreateSupplierAsync(client);
        var po = await ApprovePoAsync(client, supplier.Id, 5);
        var wh = await CreateWarehouseAsync(client, "Over WH");
        var item = await CreateItemAsync(client, "Over Item", false);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/goods-receipts/from-purchase-order",
            new CreateGoodsReceiptFromPoRequest(
                po.Id, wh.Id, DateOnly.FromDateTime(DateTime.UtcNow), null, po.RowVersion,
                [new CreateGoodsReceiptLineRequest(po.Lines.Single().Id, item.Id, 6, null, null)]));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Negative_stock_adjust_rejected()
    {
        var client = await AdminClientAsync();
        var wh = await CreateWarehouseAsync(client, "Adj WH");
        var item = await CreateItemAsync(client, "Adj Item", false);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/stock/adjust",
            new AdjustStockRequest(wh.Id, item.Id, "Out", 1, "test", null, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Concurrent_gr_numbers_unique()
    {
        var client = await AdminClientAsync();
        var supplier = await CreateSupplierAsync(client);
        var wh = await CreateWarehouseAsync(client, "Conc WH");
        var item = await CreateItemAsync(client, "Conc Item", false);
        var auth = client.DefaultRequestHeaders.Authorization!;

        var pos = new List<PurchaseOrderDto>();
        for (var i = 0; i < 20; i++)
            pos.Add(await ApprovePoAsync(client, supplier.Id, 1));

        var tasks = pos.Select(async po =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = auth;
            return await ReceiveAsync(c, po, wh.Id, item.Id, po.Lines.Single().Id, 1);
        });
        var receipts = await Task.WhenAll(tasks);
        receipts.Select(r => r.ReceiptNumber).Distinct().Should().HaveCount(20);
    }

    [Fact]
    public async Task Concurrent_receive_same_po_never_over_receives()
    {
        var client = await AdminClientAsync();
        var supplier = await CreateSupplierAsync(client);
        var po = await ApprovePoAsync(client, supplier.Id, 10);
        var wh = await CreateWarehouseAsync(client, "Race WH");
        var item = await CreateItemAsync(client, "Race Item", false);
        var lineId = po.Lines.Single().Id;
        var auth = client.DefaultRequestHeaders.Authorization!;

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = auth;
            var response = await c.PostAsJsonAsync("/api/v1/inventory/goods-receipts/from-purchase-order",
                new CreateGoodsReceiptFromPoRequest(
                    po.Id,
                    wh.Id,
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    null,
                    null,
                    [new CreateGoodsReceiptLineRequest(lineId, item.Id, 3, null, null)]));
            return response.StatusCode;
        });

        var statuses = await Task.WhenAll(tasks);
        var created = statuses.Count(s => s == HttpStatusCode.Created);
        created.Should().BeInRange(1, 3);

        var balances = await client.GetFromJsonAsync<PagedStockBalancesResult>(
            $"/api/v1/inventory/stock/balances?warehouseId={wh.Id}&inventoryItemId={item.Id}", JsonOptions);
        var qty = balances!.Items.SingleOrDefault()?.Quantity ?? 0m;
        qty.Should().BeLessThanOrEqualTo(10m);
        qty.Should().Be(created * 3m);

        var finalPo = await client.GetFromJsonAsync<PurchaseOrderDto>(
            $"/api/v1/procurement/purchase-orders/{po.Id}", JsonOptions);
        finalPo!.Lines.Single().QuantityReceived.Should().Be(qty);
        finalPo.Lines.Single().QuantityReceived.Should().BeLessThanOrEqualTo(10m);
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        return client;
    }

    private static async Task LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@erpclink.local", "ChangeMe!Admin123"));
        response.EnsureSuccessStatusCode();
        var token = JsonSerializer.Deserialize<AuthResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }

    private static async Task<SupplierDto> CreateSupplierAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/procurement/suppliers",
            new CreateSupplierRequest($"Sup {Guid.NewGuid():N}", null, null, null, null, null));
        return JsonSerializer.Deserialize<SupplierDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<PurchaseOrderDto> CreatePoAsync(HttpClient client, Guid supplierId, decimal qty)
    {
        var response = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders",
            new CreateDraftPurchaseOrderRequest(supplierId, DateOnly.FromDateTime(DateTime.UtcNow), "EGP", null, null,
                [new PurchaseOrderLineInputDto(null, "Line", qty, 10, null, 1)]));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<PurchaseOrderDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<PurchaseOrderDto> ApprovePoAsync(HttpClient client, Guid supplierId, decimal qty)
    {
        var draft = await CreatePoAsync(client, supplierId, qty);
        var submitted = await client.PostAsync($"/api/v1/procurement/purchase-orders/{draft.Id}/submit?rowVersion={Row(draft.RowVersion)}", null);
        submitted.EnsureSuccessStatusCode();
        var submittedPo = JsonSerializer.Deserialize<PurchaseOrderDto>(await submitted.Content.ReadAsStringAsync(), JsonOptions)!;
        var approve = await client.PostAsync($"/api/v1/procurement/purchase-orders/{submittedPo.Id}/approve?rowVersion={Row(submittedPo.RowVersion)}", null);
        approve.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<PurchaseOrderDto>(await approve.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<WarehouseDto> CreateWarehouseAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventory/warehouses", new CreateWarehouseRequest(name, null));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<WarehouseDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<InventoryItemDto> CreateItemAsync(HttpClient client, string name, bool trackExpiry)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventory/items",
            new CreateInventoryItemRequest(name, null, null, "EA", null, trackExpiry, 0));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<InventoryItemDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<GoodsReceiptDto> ReceiveAsync(
        HttpClient client,
        PurchaseOrderDto po,
        Guid warehouseId,
        Guid itemId,
        Guid lineId,
        decimal qty,
        byte[]? rowVersion = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventory/goods-receipts/from-purchase-order",
            new CreateGoodsReceiptFromPoRequest(
                po.Id,
                warehouseId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                null,
                rowVersion ?? po.RowVersion,
                [new CreateGoodsReceiptLineRequest(lineId, itemId, qty, null, null)]));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<GoodsReceiptDto>(body, JsonOptions)!;
    }

    private static string Row(byte[] rowVersion) => Uri.EscapeDataString(Convert.ToBase64String(rowVersion));
}
