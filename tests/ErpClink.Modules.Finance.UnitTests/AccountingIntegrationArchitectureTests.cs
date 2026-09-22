using System.Xml.Linq;
using FluentAssertions;

namespace ErpClink.Modules.Finance.UnitTests;

/// <summary>
/// Guards modular-monolith boundaries for accounting integration (STEP 18).
/// </summary>
public sealed class AccountingIntegrationArchitectureTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Theory]
    [InlineData("src/Modules/Billing/ErpClink.Modules.Billing.Infrastructure/ErpClink.Modules.Billing.Infrastructure.csproj", "Finance.Infrastructure")]
    [InlineData("src/Modules/Procurement/ErpClink.Modules.Procurement.Infrastructure/ErpClink.Modules.Procurement.Infrastructure.csproj", "Finance.Infrastructure")]
    [InlineData("src/Modules/Inventory/ErpClink.Modules.Inventory.Infrastructure/ErpClink.Modules.Inventory.Infrastructure.csproj", "Finance.Infrastructure")]
    [InlineData("src/Modules/Assets/ErpClink.Modules.Assets.Infrastructure/ErpClink.Modules.Assets.Infrastructure.csproj", "Finance.Infrastructure")]
    [InlineData("src/Modules/Finance/ErpClink.Modules.Finance.Infrastructure/ErpClink.Modules.Finance.Infrastructure.csproj", "Billing.Infrastructure")]
    [InlineData("src/Modules/Finance/ErpClink.Modules.Finance.Infrastructure/ErpClink.Modules.Finance.Infrastructure.csproj", "Procurement.Infrastructure")]
    [InlineData("src/Modules/Finance/ErpClink.Modules.Finance.Infrastructure/ErpClink.Modules.Finance.Infrastructure.csproj", "Inventory.Infrastructure")]
    [InlineData("src/Modules/Finance/ErpClink.Modules.Finance.Infrastructure/ErpClink.Modules.Finance.Infrastructure.csproj", "Assets.Infrastructure")]
    public void Forbidden_infrastructure_references(string relativeCsproj, string forbiddenFragment)
    {
        var refs = GetProjectReferences(Path.Combine(RepoRoot, relativeCsproj));
        refs.Should().NotContain(r => r.Contains(forbiddenFragment, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> GetProjectReferences(string csprojPath)
    {
        File.Exists(csprojPath).Should().BeTrue($"expected project at {csprojPath}");
        var doc = XDocument.Load(csprojPath);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ErpClink.sln"))
                || Directory.Exists(Path.Combine(dir.FullName, "src", "Modules", "Finance")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}
