using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Finance.Application.Accounts.Models;
using ErpClink.Modules.Finance.Application.FiscalPeriods.Models;
using ErpClink.Modules.Finance.Application.FiscalYears.Models;
using ErpClink.Modules.Finance.Application.Journals.Models;
using ErpClink.Modules.Reports.Application.Models;
using FluentAssertions;

namespace ErpClink.Api.Tests;

public sealed class ReportsApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ReportsApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Unauthorized_reports_denied()
    {
        var client = _factory.CreateClient();
        (await client.GetAsync("/api/v1/reports/finance/general-ledger?fromDate=2026-01-01&toDate=2026-12-31"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/v1/reports/management/summary"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Doctor_role_forbidden_for_reports()
    {
        var admin = await AdminClientAsync();
        var email = $"doctor-rpt-{Guid.NewGuid():N}@test.local";
        (await admin.PostAsJsonAsync("/api/admin/users",
            new CreateUserRequest(email, "TempUser!123", "Doctor", null, [AppRoles.Doctor]))).EnsureSuccessStatusCode();

        var client = _factory.CreateClient();
        await LoginAsync(client, email, "TempUser!123");
        (await client.GetAsync("/api/v1/reports/management/summary")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/reports/finance/general-ledger?fromDate=2026-01-01&toDate=2026-12-31"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Gl_excludes_draft_and_trial_balance_balances()
    {
        var client = await AdminClientAsync();
        var cash = await CreateAccountAsync(client, $"RGL-{Guid.NewGuid():N}"[..8], "R Cash", "Asset", true);
        var revenue = await CreateAccountAsync(client, $"RRV-{Guid.NewGuid():N}"[..8], "R Rev", "Revenue", true);
        var (journalDate, _) = await SetupOpenPeriodAsync(client);
        var rangeFrom = new DateOnly(journalDate.Year, 1, 1);
        var rangeTo = new DateOnly(journalDate.Year, 12, 31);

        var draft = await CreateJournalAsync(client, journalDate, [
            new JournalLineInput(cash.Id, "draft line", 25m, 0m, 1),
            new JournalLineInput(revenue.Id, null, 0m, 25m, 2)
        ]);
        draft.Status.Should().Be("Draft");

        var posted = await CreateJournalAsync(client, journalDate, [
            new JournalLineInput(cash.Id, "posted line", 40m, 0m, 1),
            new JournalLineInput(revenue.Id, null, 0m, 40m, 2)
        ]);
        posted = await PostJournalAsync(client, posted.Id, posted.RowVersion);

        var gl = await client.GetFromJsonAsync<ReportPage<GlReportLine>>(
            $"/api/v1/reports/finance/general-ledger?fromDate={rangeFrom:yyyy-MM-dd}&toDate={rangeTo:yyyy-MM-dd}&page=1&pageSize=50",
            JsonOptions);
        gl.Should().NotBeNull();
        gl!.Items.Should().NotContain(x => x.JournalEntryId == draft.Id);
        gl.Items.Should().Contain(x => x.JournalEntryId == posted.Id);
        gl.Page.Should().Be(1);
        gl.SourceModule.Should().Be("Finance");

        var tb = await client.GetFromJsonAsync<TrialBalanceReportResult>(
            $"/api/v1/reports/finance/trial-balance?fromDate={rangeFrom:yyyy-MM-dd}&toDate={rangeTo:yyyy-MM-dd}",
            JsonOptions);
        tb!.GrandTotalDebit.Should().Be(tb.GrandTotalCredit);
        tb.GrandTotalDebit.Should().BeGreaterThanOrEqualTo(40m);
    }

    [Fact]
    public async Task Management_summary_marks_profit_unavailable()
    {
        var client = await AdminClientAsync();
        var summary = await client.GetFromJsonAsync<ManagementSummaryResult>(
            "/api/v1/reports/management/summary?fromDate=2026-01-01&toDate=2026-12-31", JsonOptions);
        summary.Should().NotBeNull();
        summary!.Metrics.Should().Contain(m => m.Metric == "ArOutstanding" && m.Available);
        summary.Metrics.Should().Contain(m => m.Metric == "NetProfit" && !m.Available);
        summary.Metrics.Should().Contain(m => m.Metric == "Ebitda" && !m.Available);
        summary.Metrics.Should().Contain(m => m.Metric == "GrossMargin" && !m.Available);
        summary.Notes.Should().Contain("not GL revenue");
    }

    [Fact]
    public async Task Purchase_order_report_is_labeled_not_ap()
    {
        var client = await AdminClientAsync();
        var page = await client.GetFromJsonAsync<ReportPage<PurchaseOrderRegisterLine>>(
            "/api/v1/reports/procurement/purchase-orders?page=1&pageSize=10", JsonOptions);
        page.Should().NotBeNull();
        page!.SourceModule.Should().Be("Procurement");
        foreach (var item in page.Items)
        {
            item.SemanticsNote.Should().Contain("not an AP");
        }
    }

    [Fact]
    public async Task Inventory_cogs_report_labeled_not_gl()
    {
        var client = await AdminClientAsync();
        var page = await client.GetFromJsonAsync<ReportPage<InventoryCogsReportLine>>(
            "/api/v1/reports/inventory/cogs?fromDate=2026-01-01&toDate=2026-12-31&page=1&pageSize=10",
            JsonOptions);
        page.Should().NotBeNull();
        page!.DateFilterSemantics.Should().Contain("not GL COGS");
    }

    [Fact]
    public async Task Reports_infrastructure_does_not_reference_other_module_infrastructure()
    {
        var csproj = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Modules", "Reports", "ErpClink.Modules.Reports.Infrastructure",
            "ErpClink.Modules.Reports.Infrastructure.csproj"));
        File.Exists(csproj).Should().BeTrue(csproj);
        var text = await File.ReadAllTextAsync(csproj);
        text.Should().NotContain("Finance.Infrastructure");
        text.Should().NotContain("Billing.Infrastructure");
        text.Should().NotContain("Inventory.Infrastructure");
        text.Should().NotContain("Assets.Infrastructure");
        text.Should().NotContain("Procurement.Infrastructure");
        text.Should().Contain("Finance.Application");
        text.Should().Contain("Billing.Application");
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "admin@erpclink.local", "ChangeMe!Admin123");
        return client;
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var token = JsonSerializer.Deserialize<AuthResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }

    private static DateOnly UniqueJournalDate()
    {
        var year = 2040 + Random.Shared.Next(0, 50);
        return new DateOnly(year, Random.Shared.Next(1, 12), Random.Shared.Next(1, 28));
    }

    private static async Task<(DateOnly JournalDate, FiscalYearDto Year)> SetupOpenPeriodAsync(HttpClient client)
    {
        var journalDate = UniqueJournalDate();
        var yearStart = new DateOnly(journalDate.Year, 1, 1);
        var yearEnd = new DateOnly(journalDate.Year, 12, 31);
        var year = await CreateFiscalYearAsync(client, $"FY{journalDate.Year}-{Guid.NewGuid():N}"[..12], yearStart, yearEnd);
        await CreateFiscalPeriodAsync(client, year.Id, "Period", yearStart, yearEnd);
        return (journalDate, year);
    }

    private static async Task<AccountDto> CreateAccountAsync(HttpClient client, string code, string name, string type, bool postable)
    {
        var response = await client.PostAsJsonAsync("/api/v1/finance/accounts",
            new CreateAccountRequest(code, name, null, type, postable, null));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<AccountDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<FiscalYearDto> CreateFiscalYearAsync(HttpClient client, string name, DateOnly start, DateOnly end)
    {
        var response = await client.PostAsJsonAsync("/api/v1/finance/fiscal-years", new CreateFiscalYearRequest(name, start, end));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<FiscalYearDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task CreateFiscalPeriodAsync(HttpClient client, Guid yearId, string name, DateOnly start, DateOnly end)
    {
        var response = await client.PostAsJsonAsync("/api/v1/finance/fiscal-periods",
            new CreateFiscalPeriodRequest(yearId, name, start, end));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<JournalEntryDto> CreateJournalAsync(HttpClient client, DateOnly date, JournalLineInput[] lines)
    {
        var response = await client.PostAsJsonAsync("/api/v1/finance/journals", new CreateJournalDraftRequest(date, null, lines));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JournalEntryDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private static async Task<JournalEntryDto> PostJournalAsync(HttpClient client, Guid id, byte[] rowVersion)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/finance/journals/{id}/post", new PostJournalRequest(rowVersion));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JournalEntryDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }
}
