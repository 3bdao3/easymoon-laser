namespace ErpClink.Modules.Assets.Application.Categories;

public interface IAssetCategoryCodeGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
