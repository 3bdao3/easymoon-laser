using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Assets.Application.Accounting.Models;
using ErpClink.Modules.Assets.Application.Assets.Models;
using ErpClink.Modules.Assets.Application.Categories.Models;
using ErpClink.Modules.Assets.Application.Locations.Models;
using ErpClink.Modules.Assets.Domain.Accounting;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class AssetAccountingApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AssetAccountingApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Capitalize_depreciate_idempotent_and_no_journal()
    {
        var client = await AdminClientAsync();
        var asset = await CreateAssetWithCostAsync(client, 1200m);

        var cap = await client.PostAsJsonAsync("/api/v1/assets/accounting/capitalize",
            new CapitalizeAssetRequest(asset.Id, 1200m, 1200m, 200m,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), 10, null));
        cap.EnsureSuccessStatusCode();
        var profile = JsonSerializer.Deserialize<AssetFinancialProfileDto>(await cap.Content.ReadAsStringAsync(), JsonOptions)!;
        profile.NetBookValue.Should().Be(1200m);
        profile.DepreciableBase.Should().Be(1000m);

        var dep1 = await client.PostAsJsonAsync("/api/v1/assets/depreciation/post",
            new PostDepreciationRequest(asset.Id, null, null, profile.RowVersion));
        dep1.EnsureSuccessStatusCode();
        var tx1 = JsonSerializer.Deserialize<DepreciationTransactionDto>(await dep1.Content.ReadAsStringAsync(), JsonOptions)!;
        tx1.DepreciationAmount.Should().Be(100m);

        // Idempotent re-post of same period
        var again = await client.PostAsJsonAsync("/api/v1/assets/depreciation/post",
            new PostDepreciationRequest(asset.Id, tx1.PeriodKey, null, null));
        again.EnsureSuccessStatusCode();
        var txAgain = JsonSerializer.Deserialize<DepreciationTransactionDto>(await again.Content.ReadAsStringAsync(), JsonOptions)!;
        txAgain.Id.Should().Be(tx1.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var assetsDb = scope.ServiceProvider.GetRequiredService<AssetsDbContext>();
        (await assetsDb.AssetDepreciationTransactions.CountAsync(t => t.AssetId == asset.Id)).Should().Be(1);

        var financeDb = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        (await financeDb.JournalEntries.CountAsync()).Should().Be(0);
        (await financeDb.AccountingIntegrationRequests
            .CountAsync(r => r.SourceModule == "Assets" && r.EventType == "AssetDepreciationPosted"))
            .Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Legacy_asset_without_capitalize_cannot_depreciate()
    {
        var client = await AdminClientAsync();
        var asset = await CreateAssetWithCostAsync(client, null);
        var response = await client.PostAsJsonAsync("/api/v1/assets/depreciation/post",
            new PostDepreciationRequest(asset.Id, null, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Concurrent_same_period_posts_once()
    {
        var client = await AdminClientAsync();
        var asset = await CreateAssetWithCostAsync(client, 1200m);
        (await client.PostAsJsonAsync("/api/v1/assets/accounting/capitalize",
            new CapitalizeAssetRequest(asset.Id, 1200m, 1200m, 200m,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), 10, null))).EnsureSuccessStatusCode();

        var auth = client.DefaultRequestHeaders.Authorization!;
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = auth;
            return await c.PostAsJsonAsync("/api/v1/assets/depreciation/post",
                new PostDepreciationRequest(asset.Id, "2026-01", null, null));
        });
        var results = await Task.WhenAll(tasks);
        var ok = results.Count(r => r.IsSuccessStatusCode);
        ok.Should().BeGreaterThanOrEqualTo(1);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AssetsDbContext>();
        var txs = await db.AssetDepreciationTransactions
            .Where(t => t.AssetId == asset.Id && t.PeriodKey == "2026-01")
            .ToListAsync();
        txs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Unauthorized_accounting_denied()
    {
        var client = _factory.CreateClient();
        (await client.GetAsync("/api/v1/assets/accounting")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    private static async Task<AssetDto> CreateAssetWithCostAsync(HttpClient client, decimal? cost)
    {
        var cat = await client.PostAsJsonAsync("/api/v1/assets/categories", new CreateAssetCategoryRequest($"Cat {Guid.NewGuid():N}"[..12], null));
        cat.EnsureSuccessStatusCode();
        var category = JsonSerializer.Deserialize<AssetCategoryDto>(await cat.Content.ReadAsStringAsync(), JsonOptions)!;
        var loc = await client.PostAsJsonAsync("/api/v1/assets/locations", new CreateAssetLocationRequest($"Loc {Guid.NewGuid():N}"[..12], null));
        loc.EnsureSuccessStatusCode();
        var location = JsonSerializer.Deserialize<AssetLocationDto>(await loc.Content.ReadAsStringAsync(), JsonOptions)!;
        var create = await client.PostAsJsonAsync("/api/v1/assets",
            new CreateAssetRequest($"Asset {Guid.NewGuid():N}"[..20], null, category.Id, location.Id, null,
                DateOnly.FromDateTime(DateTime.UtcNow), cost, null, null, null, null));
        create.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<AssetDto>(await create.Content.ReadAsStringAsync(), JsonOptions)!;
    }
}
