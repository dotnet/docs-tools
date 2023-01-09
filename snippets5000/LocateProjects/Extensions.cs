internal static class Extensions
{
    // TRUE if "projectPath" is a child of one of the entries in folders
    internal static bool ContainedInOneOf(this string projectPath, IEnumerable<string> folders)
    {
        foreach (var folder in folders)
        {
            if (projectPath.Contains(folder))
            {
                return true;
            }
        }
        return false;
    }
}


