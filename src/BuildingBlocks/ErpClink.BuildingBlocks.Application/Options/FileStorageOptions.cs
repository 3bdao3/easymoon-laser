namespace ErpClink.BuildingBlocks.Application.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Root folder for local filesystem storage (dev/default).</summary>
    public string LocalRootPath { get; set; } = "App_Data/patient-files";

    /// <summary>Maximum upload size in bytes. Default 10 MB.</summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Allowed extensions without leading dots (lowercase).</summary>
    public string[] AllowedExtensions { get; set; } =
    [
        "pdf",
        "doc",
        "docx",
        "jpg",
        "jpeg",
        "png",
        "webp"
    ];
}
