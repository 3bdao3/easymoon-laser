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
using ErpClink.Modules.Finance.Application.GeneralLedger.Models;
using ErpClink.Modules.Finance.Application.Journals.Models;
using ErpClink.Modules.Finance.Application.TrialBalance.Models;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class FinanceApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FinanceApiTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Account_crud_and_isolation()
    {
        var client = await AdminClientAsync();
        var created = await CreateAccountAsync(client, $"CASH-{Guid.NewGuid():N}"[..8], "Cash", "Asset", true);
        created.Code.Should().Be(created.Code);

        var loaded = await client.GetFromJsonAsync<AccountDto>($"/api/v1/finance/accounts/{created.Id}", JsonOptions);
        loaded!.Name.Should().Be("Cash");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [finance].[Accounts] SET [OrganizationId] = {Guid.Parse("99999999-9999-9999-9999-999999999999")} WHERE [Id] = {created.Id}");
        }

        (await client.GetAsync($"/api/v1/finance/accounts/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Fiscal_year_and_period_create_close()
    {
        var client = await AdminClientAsync();
        var year = await CreateFiscalYearAsync(client, "FY2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var period = await CreateFiscalPeriodAsync(client, year.Id, "Jan 2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        (await client.PostAsync($"/api/v1/finance/fiscal-periods/{period.Id}/close", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/v1/finance/fiscal-years/{year.Id}/close", null)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Journal_create_post_reverse_and_gl_trial_balance()
    {
        var client = await AdminClientAsync();
        var cash = await CreateAccountAsync(client, $"1000-{Guid.NewGuid():N}"[..8], "Cash GL", "Asset", true);
        var revenue = await CreateAccountAsync(client, $"4000-{Guid.NewGuid():N}"[..8], "Revenue GL", "Revenue", true);
        var (journalDate, _) = await SetupOpenPeriodAsync(client);

        var draft = await CreateJournalAsync(client, journalDate, [
            new JournalLineInput(cash.Id, null, 150m, 0m, 1),
            new JournalLineInput(revenue.Id, null, 0m, 150m, 2)
        ]);
        draft.Status.Should().Be("Draft");

        var posted = await PostJournalAsync(client, draft.Id, draft.RowVersion);
        posted.Status.Should().Be("Posted");

        var rangeFrom = new DateOnly(journalDate.Year, 1, 1);
        var rangeTo = new DateOnly(journalDate.Year, 12, 31);
        var glBeforeReverse = await client.GetFromJsonAsync<PagedGeneralLedgerResult>(
            $"/api/v1/finance/general-ledger?fromDate={rangeFrom:yyyy-MM-dd}&toDate={rangeTo:yyyy-MM-dd}", JsonOptions);
        glBeforeReverse!.Items.Should().HaveCount(2);

        var tb = await client.GetFromJsonAsync<TrialBalanceResult>(
            $"/api/v1/finance/trial-balance?fromDate={rangeFrom:yyyy-MM-dd}&toDate={rangeTo:yyyy-MM-dd}", JsonOptions);
        tb!.GrandTotalDebit.Should().Be(150m);
        tb.GrandTotalCredit.Should().Be(150m);

        var draftOnly = await CreateJournalAsync(client, journalDate.AddDays(1), [
            new JournalLineInput(cash.Id, null, 10m, 0m, 1),
            new JournalLineInput(revenue.Id, null, 0m, 10m, 2)
        ]);
        var glWithDraft = await client.GetFromJsonAsync<PagedGeneralLedgerResult>(
            $"/api/v1/finance/general-ledger?fromDate={rangeFrom:yyyy-MM-dd}&toDate={rangeTo:yyyy-MM-dd}", JsonOptions);
        glWithDraft!.Items.Should().HaveCount(2);

        var reversal = await ReverseJournalAsync(client, posted.Id, posted.RowVersion);
        reversal.ReversalOfJournalId.Should().Be(posted.Id);

        var glAfter = await client.GetFromJsonAsync<PagedGeneralLedgerResult>(
            $"/api/v1/finance/general-ledger?fromDate={rangeFrom:yyyy-MM-dd}&toDate={rangeTo:yyyy-MM-dd}", JsonOptions);
        glAfter!.Items.Should().HaveCount(4);
    }

    [Fact]
    public async Task Unbalanced_journal_post_returns_400()
    {
        var client = await AdminClientAsync();
        var a = await CreateAccountAsync(client, $"A-{Guid.NewGuid():N}"[..6], "A", "Asset", true);
        var b = await CreateAccountAsync(client, $"B-{Guid.NewGuid():N}"[..6], "B", "Revenue", true);
        var (journalDate, _) = await SetupOpenPeriodAsync(client);

        var draft = await CreateJournalAsync(client, journalDate, [
            new JournalLineInput(a.Id, null, 100m, 0m, 1),
            new JournalLineInput(b.Id, null, 0m, 50m, 2)
        ]);

        var response = await client.PostAsJsonAsync($"/api/v1/finance/journals/{draft.Id}/post", new PostJournalRequest(draft.RowVersion));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Concurrent_journal_numbers_unique()
    {
        var client = await AdminClientAsync();
        var a = await CreateAccountAsync(client, $"CA-{Guid.NewGuid():N}"[..6], "CA", "Asset", true);
        var b = await CreateAccountAsync(client, $"CB-{Guid.NewGuid():N}"[..6], "CB", "Expense", true);
        var (journalDate, _) = await SetupOpenPeriodAsync(client);
        var auth = client.DefaultRequestHeaders.Authorization!;

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = auth;
            return await c.PostAsJsonAsync("/api/v1/finance/journals", new CreateJournalDraftRequest(journalDate, null, null));
        });
        var responses = await Task.WhenAll(tasks);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created);

        var numbers = new List<string>();
        foreach (var response in responses)
        {
            var dto = JsonSerializer.Deserialize<JournalEntryDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
            numbers.Add(dto.JournalNumber);
        }

        numbers.Distinct().Should().HaveCount(20);
    }

    [Fact]
    public async Task RowVersion_conflict_on_account_update()
    {
        var client = await AdminClientAsync();
        var account = await CreateAccountAsync(client, $"RV-{Guid.NewGuid():N}"[..6], "RV", "Asset", true);
        var stale = account.RowVersion;

        await client.PutAsJsonAsync($"/api/v1/finance/accounts/{account.Id}",
            new UpdateAccountRequest("Updated", null, "Asset", true, null, account.RowVersion));

        var conflict = await client.PutAsJsonAsync($"/api/v1/finance/accounts/{account.Id}",
            new UpdateAccountRequest("Stale", null, "Asset", true, null, stale));
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Doctor_role_forbidden_for_finance_gl()
    {
        var admin = await AdminClientAsync();
        var email = $"doctor-fin-{Guid.NewGuid():N}@test.local";
        (await admin.PostAsJsonAsync("/api/admin/users",
            new CreateUserRequest(email, "TempUser!123", "Doctor", null, [AppRoles.Doctor]))).EnsureSuccessStatusCode();

        var client = _factory.CreateClient();
        await LoginAsync(client, email, "TempUser!123");
        (await client.GetAsync("/api/v1/finance/accounts")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static DateOnly UniqueJournalDate()
    {
        var year = 2040 + Random.Shared.Next(0, 50);
        return new DateOnly(year, Random.Shared.Next(1, 12), Random.Shared.Next(1, 28));
    }

    private async Task<(DateOnly JournalDate, FiscalYearDto Year)> SetupOpenPeriodAsync(HttpClient client)
    {
        var journalDate = UniqueJournalDate();
        var yearStart = new DateOnly(journalDate.Year, 1, 1);
        var yearEnd = new DateOnly(journalDate.Year, 12, 31);
        var year = await CreateFiscalYearAsync(client, $"FY{journalDate.Year}-{Guid.NewGuid():N}"[..12], yearStart, yearEnd);
        await CreateFiscalPeriodAsync(client, year.Id, "Period", yearStart, yearEnd);
        return (journalDate, year);
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

    private static async Task<FiscalPeriodDto> CreateFiscalPeriodAsync(HttpClient client, Guid yearId, string name, DateOnly start, DateOnly end)
    {
        var response = await client.PostAsJsonAsync("/api/v1/finance/fiscal-periods",
            new CreateFiscalPeriodRequest(yearId, name, start, end));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<FiscalPeriodDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
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

    private static async Task<JournalEntryDto> ReverseJournalAsync(HttpClient client, Guid id, byte[] rowVersion)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/finance/journals/{id}/reverse", new ReverseJournalRequest(rowVersion));
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JournalEntryDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }
}
