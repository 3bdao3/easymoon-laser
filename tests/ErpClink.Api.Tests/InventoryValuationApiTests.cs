using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Inventory.Application.Costing.Models;
using ErpClink.Modules.Inventory.Application.GoodsReceipts.Models;
using ErpClink.Modules.Inventory.Application.Items.Models;
using ErpClink.Modules.Inventory.Application.Stock.Models;
using ErpClink.Modules.Inventory.Application.Warehouses.Models;
using ErpClink.Modules.Inventory.Domain.Costing;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using ErpClink.Modules.Procurement.Application.PurchaseOrders.Models;
using ErpClink.Modules.Procurement.Application.Suppliers.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class InventoryValuationApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InventoryValuationApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Goods_receipt_creates_cost_layer_and_valuation()
    {
        var client = await AdminClientAsync();
        var wh = await CreateWarehouseAsync(client);
        var item = await CreateItemAsync(client);
        var po = await ApprovePoAsync(client, (await CreateSupplierAsync(client)).Id, 10, 100m);
        var receipt = await ReceiveAsync(client, po, wh.Id, item.Id, 10);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var layer = await db.InventoryCostLayers.SingleAsync(l => l.SourceId == receipt.Id);
        layer.RemainingQuantity.Should().Be(10m);
        layer.UnitCost.Should().Be(100m);

        var balance = await db.StockBalances.SingleAsync(b => b.InventoryItemId == item.Id && b.WarehouseId == wh.Id);
        balance.InventoryValue.Should().Be(1000m);

        var valuation = await client.GetFromJsonAsync<ValuationResult>(
            $"/api/v1/inventory/valuation?warehouseId={wh.Id}&inventoryItemId={item.Id}", JsonOptions);
        valuation!.TotalValue.Should().Be(1000m);
    }

    [Fact]
    public async Task Fifo_issue_consumes_layers_in_order()
    {
        var client = await AdminClientAsync();
        var wh = await CreateWarehouseAsync(client);
        var item = await CreateItemAsync(client);
        var supplier = await CreateSupplierAsync(client);

        var po1 = await ApprovePoAsync(client, supplier.Id, 10, 100m);
        await ReceiveAsync(client, po1, wh.Id, item.Id, 10);
        var po2 = await ApprovePoAsync(client, supplier.Id, 10, 120m);
        await ReceiveAsync(client, po2, wh.Id, item.Id, 10);

        (await client.PostAsJsonAsync("/api/v1/inventory/stock/adjust",
            new AdjustStockRequest(wh.Id, item.Id, "Out", 15m, "FIFO issue", null, null, null)))
            .EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var open = await db.InventoryCostLayers
            .Where(l => l.InventoryItemId == item.Id && l.Status == InventoryCostLayerStatus.Open)
            .SingleAsync();
        open.RemainingQuantity.Should().Be(5m);
        open.UnitCost.Should().Be(120m);

        var balance = await db.StockBalances.SingleAsync(b => b.InventoryItemId == item.Id && b.WarehouseId == wh.Id);
        balance.Quantity.Should().Be(5m);
        balance.InventoryValue.Should().Be(600m);

        var issueCosts = await client.GetFromJsonAsync<PagedIssueCostResult>(
            $"/api/v1/inventory/valuation/issue-costs?warehouseId={wh.Id}&inventoryItemId={item.Id}", JsonOptions);
        issueCosts!.Items.Should().Contain(i => i.TotalCost == 1600m);
    }

    [Fact]
    public async Task Adjust_in_requires_unit_cost()
    {
        var client = await AdminClientAsync();
        var wh = await CreateWarehouseAsync(client);
        var item = await CreateItemAsync(client);
        var response = await client.PostAsJsonAsync("/api/v1/inventory/stock/adjust",
            new AdjustStockRequest(wh.Id, item.Id, "In", 2m, "missing cost", null, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthorized_valuation_denied()
    {
        var client = _factory.CreateClient();
        (await client.GetAsync("/api/v1/inventory/valuation")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@erpclink.local", "ChangeMe!Admin123"));
        response.EnsureSuccessStatusCode();
        var token = JsonSerializer.Deserialize<AuthResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    private static async Task<SupplierDto> CreateSupplierAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/procurement/suppliers",
            new CreateSupplierRequest($"Sup {Guid.NewGuid():N}", null, null, null, null, null));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<SupplierDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<PurchaseOrderDto> ApprovePoAsync(HttpClient client, Guid supplierId, decimal qty, decimal unitCost)
    {
        var create = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders",
            new CreateDraftPurchaseOrderRequest(supplierId, DateOnly.FromDateTime(DateTime.UtcNow), "EGP", null, null,
                [new PurchaseOrderLineInputDto(null, "Line", qty, unitCost, null, 1)]));
        create.EnsureSuccessStatusCode();
        var draft = JsonSerializer.Deserialize<PurchaseOrderDto>(await create.Content.ReadAsStringAsync(), JsonOptions)!;
        var submitted = await client.PostAsync($"/api/v1/procurement/purchase-orders/{draft.Id}/submit?rowVersion={Row(draft.RowVersion)}", null);
        submitted.EnsureSuccessStatusCode();
        var submittedPo = JsonSerializer.Deserialize<PurchaseOrderDto>(await submitted.Content.ReadAsStringAsync(), JsonOptions)!;
        var approve = await client.PostAsync($"/api/v1/procurement/purchase-orders/{submittedPo.Id}/approve?rowVersion={Row(submittedPo.RowVersion)}", null);
        approve.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<PurchaseOrderDto>(await approve.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<WarehouseDto> CreateWarehouseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventory/warehouses", new CreateWarehouseRequest($"WH {Guid.NewGuid():N}"[..12], null));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<WarehouseDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<InventoryItemDto> CreateItemAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventory/items",
            new CreateInventoryItemRequest($"Item {Guid.NewGuid():N}"[..12], null, null, "EA", null, false, 0));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<InventoryItemDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<GoodsReceiptDto> ReceiveAsync(
        HttpClient client, PurchaseOrderDto po, Guid warehouseId, Guid itemId, decimal qty)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventory/goods-receipts/from-purchase-order",
            new CreateGoodsReceiptFromPoRequest(
                po.Id, warehouseId, DateOnly.FromDateTime(DateTime.UtcNow), null, po.RowVersion,
                [new CreateGoodsReceiptLineRequest(po.Lines.Single().Id, itemId, qty, null, null)]));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<GoodsReceiptDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static string Row(byte[] rowVersion) => Uri.EscapeDataString(Convert.ToBase64String(rowVersion));
}
