using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Assets.Application.Assets.Models;
using ErpClink.Modules.Assets.Application.Categories.Models;
using ErpClink.Modules.Assets.Application.Locations.Models;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class AssetsApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AssetsApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Category_location_and_asset_crud()
    {
        var client = await AdminClientAsync();
        var category = await CreateCategoryAsync(client, "Medical Devices");
        category.Code.Should().StartWith("ASC-");

        var location = await CreateLocationAsync(client, "Radiology");
        location.Code.Should().StartWith("LOC-");

        var asset = await CreateAssetAsync(client, category.Id, location.Id, "Asset One");
        asset.AssetNumber.Should().StartWith("AST-");
        asset.Status.Should().Be("Active");

        var loaded = await client.GetFromJsonAsync<AssetDto>($"/api/v1/assets/{asset.Id}", JsonOptions);
        loaded!.Name.Should().Be("Asset One");
    }

    [Fact]
    public async Task Concurrent_asset_numbers_unique()
    {
        var client = await AdminClientAsync();
        var category = await CreateCategoryAsync(client, "Bulk Cat");
        var location = await CreateLocationAsync(client, "Bulk Loc");
        var auth = client.DefaultRequestHeaders.Authorization!;
        var purchase = DateOnly.FromDateTime(DateTime.UtcNow);

        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = auth;
            return await c.PostAsJsonAsync("/api/v1/assets",
                new CreateAssetRequest($"Asset {i}", null, category.Id, location.Id, null,
                    purchase, null, null, null, null, null));
        });

        var responses = await Task.WhenAll(tasks);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created);
        var numbers = new List<string>();
        foreach (var response in responses)
        {
            var dto = JsonSerializer.Deserialize<AssetDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
            numbers.Add(dto.AssetNumber);
        }

        numbers.Distinct().Should().HaveCount(20);
    }

    [Fact]
    public async Task Lifecycle_and_location_change()
    {
        var client = await AdminClientAsync();
        var category = await CreateCategoryAsync(client, "Lifecycle Cat");
        var location = await CreateLocationAsync(client, "Lifecycle Loc");
        var locationB = await CreateLocationAsync(client, "Lifecycle Loc B");
        var asset = await CreateAssetAsync(client, category.Id, location.Id, "Lifecycle Asset");

        asset = await PostAssetActionAsync(client, asset.Id, "change-location",
            new ChangeAssetLocationRequest(locationB.Id, asset.RowVersion));
        asset.AssetLocationId.Should().Be(locationB.Id);

        asset = await PostAssetActionAsync(client, asset.Id, "start-maintenance",
            new StartAssetMaintenanceRequest("Check filters", asset.RowVersion));
        asset.Status.Should().Be("UnderMaintenance");

        asset = await PostAssetActionAsync(client, asset.Id, "complete-maintenance",
            new CompleteAssetMaintenanceRequest(asset.RowVersion));
        asset.Status.Should().Be("Active");

        asset = await PostAssetActionAsync(client, asset.Id, "retire",
            new RetireAssetRequest("Obsolete", asset.RowVersion));
        asset.Status.Should().Be("Retired");
    }

    [Fact]
    public async Task Retired_asset_cannot_change_location()
    {
        var client = await AdminClientAsync();
        var category = await CreateCategoryAsync(client, "Retire Cat");
        var location = await CreateLocationAsync(client, "Loc A");
        var locationB = await CreateLocationAsync(client, "Loc B");
        var asset = await CreateAssetAsync(client, category.Id, location.Id, "Retire Me");
        asset = await PostAssetActionAsync(client, asset.Id, "retire", new RetireAssetRequest("Done", asset.RowVersion));

        var response = await client.PostAsJsonAsync($"/api/v1/assets/{asset.Id}/change-location",
            new ChangeAssetLocationRequest(locationB.Id, asset.RowVersion));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Warranty_invalid_range_rejected()
    {
        var client = await AdminClientAsync();
        var category = await CreateCategoryAsync(client, "Warranty Cat");
        var location = await CreateLocationAsync(client, "Warranty Loc");
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await client.PostAsJsonAsync("/api/v1/assets",
            new CreateAssetRequest("Bad Warranty", null, category.Id, location.Id, null,
                start, null, null, start, start.AddDays(-1), null));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RowVersion_conflict_on_update()
    {
        var client = await AdminClientAsync();
        var category = await CreateCategoryAsync(client, "RV Cat");
        var location = await CreateLocationAsync(client, "RV Loc");
        var asset = await CreateAssetAsync(client, category.Id, location.Id, "RV Asset");
        var stale = asset.RowVersion;

        await client.PutAsJsonAsync($"/api/v1/assets/{asset.Id}",
            new UpdateAssetRequest("Updated", null, category.Id, null, asset.PurchaseDate, null, null, null, null, null, asset.RowVersion));

        var conflict = await client.PutAsJsonAsync($"/api/v1/assets/{asset.Id}",
            new UpdateAssetRequest("Stale", null, category.Id, null, asset.PurchaseDate, null, null, null, null, null, stale));
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Doctor_role_forbidden_for_assets()
    {
        var client = _factory.CreateClient();
        var email = $"doctor-assets-{Guid.NewGuid():N}@test.local";
        var admin = await AdminClientAsync();
        (await admin.PostAsJsonAsync("/api/admin/users",
            new CreateUserRequest(email, "TempUser!123", "Doctor User", null, [AppRoles.Doctor]))).EnsureSuccessStatusCode();
        await LoginAsync(client, email, "TempUser!123");

        var response = await client.GetAsync("/api/v1/assets");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cross_organization_asset_access_returns_not_found()
    {
        var client = await AdminClientAsync();
        var category = await CreateCategoryAsync(client, "Iso Cat");
        var location = await CreateLocationAsync(client, "Iso Loc");
        var asset = await CreateAssetAsync(client, category.Id, location.Id, "Iso Asset");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AssetsDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [assets].[Assets] SET [OrganizationId] = {Guid.Parse("99999999-9999-9999-9999-999999999999")} WHERE [Id] = {asset.Id}");
        }

        var response = await client.GetAsync($"/api/v1/assets/{asset.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Asset_history_recorded_on_lifecycle()
    {
        var client = await AdminClientAsync();
        var category = await CreateCategoryAsync(client, "Hist Cat");
        var location = await CreateLocationAsync(client, "Hist Loc");
        var asset = await CreateAssetAsync(client, category.Id, location.Id, "Hist Asset");

        var history = await client.GetFromJsonAsync<List<AssetHistoryEntryDto>>($"/api/v1/assets/{asset.Id}/history", JsonOptions);
        history!.Should().Contain(h => h.EventType == "Created");
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

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var token = JsonSerializer.Deserialize<AuthResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }

    private static async Task<AssetCategoryDto> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/assets/categories", new CreateAssetCategoryRequest(name, null));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<AssetCategoryDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<AssetLocationDto> CreateLocationAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/assets/locations", new CreateAssetLocationRequest(name, null));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<AssetLocationDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<AssetDto> CreateAssetAsync(HttpClient client, Guid categoryId, Guid locationId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/assets",
            new CreateAssetRequest(name, null, categoryId, locationId, null,
                DateOnly.FromDateTime(DateTime.UtcNow), 1000m, "Manual entry", null, null, null));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<AssetDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<AssetDto> PostAssetActionAsync<T>(HttpClient client, Guid id, string action, T body)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/assets/{id}/{action}", body);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<AssetDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }
}
