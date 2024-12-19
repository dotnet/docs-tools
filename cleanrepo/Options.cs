namespace CleanRepo;

class Options
{
    public string? DocFxDirectory { get; set; }
    public string? TargetDirectory { get; set; }
    public string? UrlBasePath { get; set; }
    public bool Delete { get; set; } = true;
    public bool XmlSource { get; set; } = false;
    public string? Function { get; set; }
    public string? OcrModelDirectory { get; set; }
    public string? FilterTextJsonFile { get; set; }
    public List<string>? LimitReferencingDirectories { get; set; }
}
