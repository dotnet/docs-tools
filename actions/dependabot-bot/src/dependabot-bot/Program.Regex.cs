static partial class Program
{
    [GeneratedRegex("PackageReference.*Version=\"[0-9]")]
    private static partial Regex PackageReferenceVersionRegex();

    [GeneratedRegex("TargetFramework(.*)>(?<tfm>.+?)</")]
    private static partial Regex TargetFrameworkRegex();

    [GeneratedRegex("<PackageReference(?:.+?)Include=\"\"(?<nuget>.+?)\"\"")]
    private static partial Regex PackageReferenceIncludeRegex();

    static bool TryGetTargetFramework(
    string content,
    [NotNullWhen(true)] out string? targetFramework) =>
    TryGetRegexGroupValue(
        TargetFrameworkRegex(),
        content, "tfm", out targetFramework);

    static bool TryGetPackageName(
        string content,
        [NotNullWhen(true)] out string? packageName) =>
        TryGetRegexGroupValue(
            PackageReferenceIncludeRegex(),
            content, "nuget", out packageName);

    static bool TryGetRegexGroupValue(
        Regex regex, 
        string content, 
        string groupKey, 
        [NotNullWhen(true)] out string? groupValue)
    {
        var match = regex.Match(content);
        if (match is { Success: true } and { Groups.Count: > 0 })
        {
            groupValue = match.Groups[groupKey].Value;
            return true;
        }
        else
        {
            groupValue = null;
            return false;
        }
    }
}