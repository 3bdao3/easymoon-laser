using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace ErpClink.Api.Tests;

public sealed class LaserClinicApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly AuthWebApplicationFactory _factory;

    public LaserClinicApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Book_with_new_customer_bikini_combo_is_15_minutes()
    {
        var client = await AuthedClientAsync();

        var services = await client.GetFromJsonAsync<JsonElement>("/api/v1/laser-services", JsonOptions);
        var bikiniCombo = FindService(services, "بكيني + لاين");
        bikiniCombo.Should().NotBeNull();

        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
        var bookRes = await client.PostAsJsonAsync("/api/v1/laser-appointments/book-with-customer", new
        {
            existingCustomerId = (Guid?)null,
            customer = new
            {
                fullName = "سارة محمد",
                phoneNumber = $"010{Random.Shared.Next(10000000, 99999999)}",
                age = 25,
                notes = (string?)null
            },
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = "17:00:00",
            laserServiceIds = new[] { bikiniCombo!.Value.GetProperty("id").GetGuid() },
            appointmentNotes = "أول جلسة"
        });

        bookRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var appt = await bookRes.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        appt.GetProperty("durationMinutes").GetInt32().Should().Be(15);
        appt.GetProperty("startTime").GetString().Should().StartWith("17:00");
        appt.GetProperty("endTime").GetString().Should().StartWith("17:15");
        appt.GetProperty("status").GetInt32().Should().Be(0); // Pending
    }

    [Fact]
    public async Task Multi_service_duration_sums_and_conflict_is_rejected()
    {
        var client = await AuthedClientAsync();
        var services = await client.GetFromJsonAsync<JsonElement>("/api/v1/laser-services", JsonOptions);

        var face = FindService(services, "فيس")!.Value.GetProperty("id").GetGuid();
        var arm = FindService(services, "ذراع كامل")!.Value.GetProperty("id").GetGuid();
        var leg = FindService(services, "نصف رجل")!.Value.GetProperty("id").GetGuid();
        // Prefer exact half leg not upper
        foreach (var s in services.EnumerateArray())
        {
            if (s.GetProperty("name").GetString() == "نصف رجل")
                leg = s.GetProperty("id").GetGuid();
            if (s.GetProperty("name").GetString() == "ذراع كامل")
                arm = s.GetProperty("id").GetGuid();
            if (s.GetProperty("name").GetString() == "فيس ورقبة")
                face = s.GetProperty("id").GetGuid();
        }

        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var phone = $"011{Random.Shared.Next(10000000, 99999999)}";

        var first = await client.PostAsJsonAsync("/api/v1/laser-appointments/book-with-customer", new
        {
            customer = new { fullName = "نور أحمد", phoneNumber = phone, age = 22, notes = (string?)null },
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = "17:00:00",
            laserServiceIds = new[] { face, arm, leg },
            appointmentNotes = (string?)null
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var appt = await first.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        appt.GetProperty("durationMinutes").GetInt32().Should().Be(55);
        appt.GetProperty("endTime").GetString().Should().StartWith("17:55");

        var conflict = await client.PostAsJsonAsync("/api/v1/laser-appointments/book-with-customer", new
        {
            customer = new
            {
                fullName = "عميلة تعارض",
                phoneNumber = $"012{Random.Shared.Next(10000000, 99999999)}",
                age = (int?)null,
                notes = (string?)null
            },
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = "17:30:00",
            laserServiceIds = new[] { face },
            appointmentNotes = (string?)null
        });
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var err = await conflict.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var detail = err.TryGetProperty("detail", out var d) ? d.GetString() : "";
        detail.Should().Match(s =>
            s!.Contains("متعارض", StringComparison.Ordinal)
            || s.Contains("تم حجزه", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Check_slot_reports_availability()
    {
        var client = await AuthedClientAsync();
        var services = await client.GetFromJsonAsync<JsonElement>("/api/v1/laser-services", JsonOptions);
        var serviceId = services[0].GetProperty("id").GetGuid();
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(4));

        var check = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/laser-appointments/check-slot?date={date:yyyy-MM-dd}&startTime=18:00:00&serviceIds={serviceId}",
            JsonOptions);
        check.GetProperty("isAvailable").GetBoolean().Should().BeTrue();
        check.GetProperty("messageAr").GetString().Should().Be("الموعد متاح");
    }

    [Fact]
    public async Task Customer_list_includes_next_appointment_with_duration()
    {
        var client = await AuthedClientAsync();
        var services = await client.GetFromJsonAsync<JsonElement>("/api/v1/laser-services", JsonOptions);
        var fullLeg = FindService(services, "رجل كامل") ?? FindService(services, "Full Leg");
        fullLeg.Should().NotBeNull();
        var serviceId = fullLeg!.Value.GetProperty("id").GetGuid();

        var phone = $"015{Random.Shared.Next(10000000, 99999999)}";
        var nextDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        var book = await client.PostAsJsonAsync("/api/v1/laser-appointments/book-with-customer", new
        {
            customer = new { fullName = "Eman Mohamed", phoneNumber = phone, age = 30, notes = (string?)null },
            appointmentDate = nextDate.ToString("yyyy-MM-dd"),
            startTime = "17:00:00",
            laserServiceIds = new[] { serviceId },
            appointmentNotes = (string?)null
        });
        book.EnsureSuccessStatusCode();
        var appt = await book.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var apptId = appt.GetProperty("id").GetGuid();

        // Confirm status
        var statusRes = await client.PutAsJsonAsync($"/api/v1/laser-appointments/{apptId}/status", new { status = 1 });
        statusRes.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<JsonElement>($"/api/v1/customers?q={phone}", JsonOptions);
        var row = list.EnumerateArray().First(r => r.GetProperty("phoneNumber").GetString() == phone);
        var next = row.GetProperty("nextAppointment");
        next.ValueKind.Should().NotBe(JsonValueKind.Null);
        next.GetProperty("date").GetString().Should().StartWith(nextDate.ToString("yyyy-MM-dd"));
        next.GetProperty("startTime").GetString().Should().StartWith("17:00");
        next.GetProperty("durationMinutes").GetInt32().Should().Be(45);
        next.GetProperty("status").GetInt32().Should().Be(1);
    }

    private async Task<HttpClient> AuthedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static JsonElement? FindService(JsonElement services, string nameContains)
    {
        foreach (var s in services.EnumerateArray())
        {
            var name = s.GetProperty("name").GetString() ?? "";
            if (name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                return s;
        }
        return null;
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@erpclink.local",
            password = "ChangeMe!Admin123"
        });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        if (body.TryGetProperty("accessToken", out var t1))
            return t1.GetString()!;
        return body.GetProperty("AccessToken").GetString()!;
    }
}
