using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Domain.Users;
using FluentAssertions;

namespace ErpClink.Api.Tests;

public sealed class ReceptionistLaserClinicApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly AuthWebApplicationFactory _factory;

    public ReceptionistLaserClinicApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Seeded_receptionist_can_login_with_username()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsReceptionistAsync(client, useUsername: true);

        login.User.FullName.Should().Be("موظفة السوشيال ميديا");
        login.User.Roles.Should().Contain(AppRoles.Receptionist);
        login.User.Roles.Should().NotContain(AppRoles.SuperAdmin);
        login.User.Permissions.Should().Contain(PermissionCodes.LaserCustomersView);
        login.User.Permissions.Should().Contain(PermissionCodes.LaserAppointmentsCreate);
        login.User.Permissions.Should().Contain(PermissionCodes.LaserOffersView);
        login.User.Permissions.Should().NotContain(PermissionCodes.AdministrationUsersView);
        login.User.Permissions.Should().NotContain(PermissionCodes.LaserSettingsManage);
        login.User.Permissions.Should().NotContain(PermissionCodes.LaserReportsView);
        login.User.Permissions.Should().NotContain(PermissionCodes.LaserServicesManage);
        login.User.Permissions.Should().NotContain(PermissionCodes.LaserOffersManage);
    }

    [Fact]
    public async Task Receptionist_can_manage_customers_and_bookings()
    {
        var client = await AuthedReceptionistClientAsync();

        var createCustomer = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "عميلة سوشيال",
            phoneNumber = $"015{Random.Shared.Next(10000000, 99999999)}",
            age = 27,
            notes = "من إنستغرام"
        });
        createCustomer.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await createCustomer.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var customerId = customer.GetProperty("id").GetGuid();

        var list = await client.GetAsync("/api/v1/customers");
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        var update = await client.PutAsJsonAsync($"/api/v1/customers/{customerId}", new
        {
            fullName = "عميلة سوشيال محدّثة",
            phoneNumber = customer.GetProperty("phoneNumber").GetString(),
            age = 28,
            notes = "تم التحديث"
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var services = await client.GetFromJsonAsync<JsonElement>("/api/v1/laser-services", JsonOptions);
        var serviceId = services.EnumerateArray().First().GetProperty("id").GetGuid();
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        var availability = await client.GetAsync(
            $"/api/v1/laser-appointments/availability?date={date:yyyy-MM-dd}&serviceIds={serviceId}");
        availability.StatusCode.Should().Be(HttpStatusCode.OK);

        var book = await client.PostAsJsonAsync("/api/v1/laser-appointments", new
        {
            customerId,
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = "17:00:00",
            laserServiceIds = new[] { serviceId },
            notes = "حجز موظفة السوشيال"
        });
        book.StatusCode.Should().Be(HttpStatusCode.Created);
        var appt = await book.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var apptId = appt.GetProperty("id").GetGuid();

        var listAppts = await client.GetAsync($"/api/v1/laser-appointments?date={date:yyyy-MM-dd}");
        listAppts.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancel = await client.DeleteAsync($"/api/v1/laser-appointments/{apptId}");
        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Receptionist_forbidden_apis_return_403()
    {
        var client = await AuthedReceptionistClientAsync();

        (await client.GetAsync("/api/admin/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/clinic-settings")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.PutAsJsonAsync("/api/v1/clinic-settings", new
        {
            openingTime = "10:00:00",
            closingTime = "22:00:00",
            appointmentSlotIntervalMinutes = 5,
            defaultBufferMinutes = 0
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/laser-offers")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/v1/laser-offers", new
        {
            title = "Forbidden Offer",
            description = "x",
            price = 100,
            validFrom = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            validTo = (string?)null,
            displayOrder = 1
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/api/v1/laser-services", new
        {
            name = "Forbidden Area",
            minDurationMinutes = 10,
            maxDurationMinutes = 60,
            recommendedDurationMinutes = 30,
            displayOrder = 99,
            isActive = true
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuperAdmin_still_has_full_access()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@erpclink.local", "ChangeMe!Admin123"));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        login.User.Roles.Should().Contain(AppRoles.SuperAdmin);
        (await client.GetAsync("/api/admin/users")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/v1/clinic-settings")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> AuthedReceptionistClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsReceptionistAsync(client, useUsername: true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<AuthResponse> LoginAsReceptionistAsync(HttpClient client, bool useUsername)
    {
        var identifier = useUsername ? "socialmedia" : "socialmedia@easymoon.local";
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(identifier, "EasyMoon@2026"));
        var bodyText = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: bodyText);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        body.Should().NotBeNull();
        return body!;
    }
}
