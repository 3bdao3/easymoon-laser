using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Services.Application.Catalog.Models;
using ErpClink.Modules.Services.Application.Categories.Models;
using ErpClink.Modules.Services.Application.Packages.Models;
using ErpClink.Modules.Services.Application.Contracts;
using ErpClink.Modules.Services.Infrastructure.Events;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class ServicesApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ServicesApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Service_catalog_crud_activate_and_price_history()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        _factory.Services.GetRequiredService<ServicesDomainEventCollector>().Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var cat = await CreateCategoryAsync(client, $"CAT-{suffix}", "Consultation");
        var create = await client.PostAsJsonAsync("/api/v1/services",
            new CreateHealthcareServiceRequest($"SVC-{suffix}", "General Consultation", "Desc", cat.Id, 200m, "EGP", 30));
        var body = await create.Content.ReadAsStringAsync();
        create.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        var svc = JsonSerializer.Deserialize<HealthcareServiceDto>(body, JsonOptions)!;
        svc.CurrentPriceId.Should().NotBeNull();

        var dup = await client.PostAsJsonAsync("/api/v1/services",
            new CreateHealthcareServiceRequest($"SVC-{suffix}", "Dup", null, null, 1m, "EGP", null));
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var priceUpdate = await client.PutAsJsonAsync($"/api/v1/services/{svc.Id}",
            new UpdateHealthcareServiceRequest("General Consultation", "Desc2", cat.Id, 250m, null, 30, null));
        var priceBody = await priceUpdate.Content.ReadAsStringAsync();
        priceUpdate.StatusCode.Should().Be(HttpStatusCode.OK, because: priceBody);
        var svc2 = await priceUpdate.Content.ReadFromJsonAsync<HealthcareServiceDto>(JsonOptions);
        svc2!.DefaultPrice.Should().Be(250m);

        (await client.PostAsync($"/api/v1/services/{svc.Id}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/v1/services/{svc.Id}/activate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var search = await client.GetFromJsonAsync<PagedHealthcareServicesResult>(
            "/api/v1/services/search?query=Consultation&isActive=true&page=1&pageSize=10", JsonOptions);
        search!.Items.Should().Contain(i => i.Id == svc.Id);
    }

    [Fact]
    public async Task Package_items_and_authorization()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);

        var svc = await CreateServiceAsync(client, "LAB-CBC", "CBC", 50m);
        var pkgCreate = await client.PostAsJsonAsync("/api/v1/packages",
            new CreateHealthcarePackageRequest("PKG-01", "Checkup", null,
                [new PackageItemInput(svc.Id, 1, 1)]));
        pkgCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var pkg = (await pkgCreate.Content.ReadFromJsonAsync<HealthcarePackageDto>(JsonOptions))!;

        var dupItem = await client.PostAsJsonAsync($"/api/v1/packages/{pkg.Id}/items",
            new PackageItemInput(svc.Id, 2, 2));
        dupItem.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await client.PostAsync($"/api/v1/packages/{pkg.Id}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync($"/api/v1/packages/{pkg.Id}/items",
            new PackageItemInput(svc.Id, 1, 1))).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var email = $"inv-{Guid.NewGuid():N}@erpclink.local";
        (await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email, "TempUser!123", "Inventory", null, [AppRoles.InventoryManager]))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        await LoginAsync(client, email, "TempUser!123");
        (await client.GetAsync("/api/v1/services/search")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/packages/search")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Service_lookup_contract()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var svc = await CreateServiceAsync(client, "LKP-01", "Lookup Test", 99m);

        await using var scope = _factory.Services.CreateAsyncScope();
        var lookup = scope.ServiceProvider.GetRequiredService<IServiceLookup>();
        var dto = await lookup.GetServiceAsync(svc.Id);
        dto.Should().NotBeNull();
        dto!.CurrentPrice.Should().Be(99m);
        (await lookup.GetCurrentPriceAsync(svc.Id)).Should().Be(99m);

        var packageLookup = scope.ServiceProvider.GetRequiredService<IPackageLookup>();
        var pkgResp = await client.PostAsJsonAsync("/api/v1/packages",
            new CreateHealthcarePackageRequest($"PKG-{Guid.NewGuid():N}"[..8], "Lkp Pkg", null,
                [new PackageItemInput(svc.Id, 2, 1)]));
        var pkg = await pkgResp.Content.ReadFromJsonAsync<HealthcarePackageDto>(JsonOptions);
        var items = await packageLookup.GetPackageItemsAsync(pkg!.Id);
        items.Should().ContainSingle(i => i.ServiceId == svc.Id && i.Quantity == 2);
    }

    [Fact]
    public async Task Concurrency_conflict_on_service_update()
    {
        var client = _factory.CreateClient();
        await LoginAsAdminAsync(client);
        var svc = await CreateServiceAsync(client, "CONC-01", "Concurrency", 10m);
        var stale = svc.RowVersion;
        await client.PutAsJsonAsync($"/api/v1/services/{svc.Id}",
            new UpdateHealthcareServiceRequest("Concurrency", null, null, 11m, null, null, svc.RowVersion));

        var conflict = await client.PutAsJsonAsync($"/api/v1/services/{svc.Id}",
            new UpdateHealthcareServiceRequest("Concurrency X", null, null, 12m, null, null, stale));
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<ServiceCategoryDto> CreateCategoryAsync(HttpClient client, string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/service-categories",
            new CreateServiceCategoryRequest(code, name, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ServiceCategoryDto>(JsonOptions))!;
    }

    private async Task<HealthcareServiceDto> CreateServiceAsync(HttpClient client, string code, string name, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/v1/services",
            new CreateHealthcareServiceRequest(code, name, null, null, price, "EGP", null));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, because: body);
        return JsonSerializer.Deserialize<HealthcareServiceDto>(body, JsonOptions)!;
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
