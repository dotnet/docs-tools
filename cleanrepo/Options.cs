namespace CleanRepo;

// Define a class to represent config options.
class Options
{
    public string? DocFxDirectory { get; set; }
    public string? SnippetsDirectory { get; set; }
    public string? MediaDirectory { get; set; }
    public string? IncludesDirectory { get; set; }
    public string? ArticlesDirectory { get; set; }
    public string? UrlBasePath { get; set; }
    public bool? Delete { get; set; }
    public bool XmlSource { get; set; }
    public bool FindOrphanedArticles { get; set; }
    public bool FindOrphanedImages { get; set; }
    public bool CatalogImages { get; set; }
    public bool FindOrphanedSnippets { get; set; }
    public bool FindOrphanedIncludes { get; set; }
    public bool ReplaceRedirectTargets { get; set; }
    public bool ReplaceWithRelativeLinks { get; set; }
    public bool RemoveRedirectHops { get; set; }
    public bool CatalogImagesWithText { get; set; }
    public bool FilterImagesForText { get; set; }
    public string? OcrModelDirectory { get; set; }
    public string? FilterTextJsonFile { get; set; }
}
