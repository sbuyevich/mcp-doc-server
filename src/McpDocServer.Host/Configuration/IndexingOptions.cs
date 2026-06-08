namespace McpDocServer.Host.Configuration;

/// <summary>
/// Limits and scheduling settings for future indexing stages.
/// </summary>
public sealed class IndexingOptions
{
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(1);

    public long MaxPackageBytes { get; set; } = 100 * 1024 * 1024;

    public long MaxDocumentBytes { get; set; } = 20 * 1024 * 1024;

    public int DefaultMaxResults { get; set; } = 10;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
