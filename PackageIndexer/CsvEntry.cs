namespace PackageIndexer;

public sealed class CsvEntry
{
    public static CsvEntry Create(string packageNumber, string packageName, string packageVersion)
    {
        return new CsvEntry(packageNumber, packageName, packageVersion);
    }

    private CsvEntry(string packageNumber, string packageName, string packageVersion)
    {
        PackageNumber = packageNumber;
        PackageName = packageName;
        PackageVersion = packageVersion;
    }

    public string PackageNumber { get; set; }
    public string PackageName { get; set; }
    public string PackageVersion { get; set; }    
}
