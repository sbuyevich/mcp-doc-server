namespace McpDocServer.Application.Retrieval;

public readonly record struct LibraryId(string PackageId)
{
    private const string Prefix = "nuget:";

    public static bool TryParse(string value, out LibraryId libraryId)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            && value.Length > Prefix.Length)
        {
            libraryId = new(value[Prefix.Length..].Trim());
            return libraryId.PackageId.Length > 0;
        }

        libraryId = default;
        return false;
    }

    public override string ToString() => $"{Prefix}{PackageId}";
}
