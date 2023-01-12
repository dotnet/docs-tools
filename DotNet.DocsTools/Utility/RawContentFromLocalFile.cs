namespace DotNet.DocsTools.Utility;

/// <summary>
/// Static class contains methods to parse article files.
/// </summary>
/// <remarks>
/// At this time, we retrieve the title from markdown and YML files.
/// </remarks>
public static class RawContentFromLocalFile
{
    /// <summary>
    /// Retrieve the title from the raw content.
    /// </summary>
    /// <returns>The string of the markdown or YAML file.</returns>
    public static string RetrieveTitleFromFile(string filePath)
    {
        if (filePath.EndsWith("yml")) // Look for "title:" metadata in YML files.
        {
            int found = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                if (line.Trim().StartsWith("title:"))
                {
                    if (++found == 2)
                        return line.Replace("title:", "").Trim();
                }
            }
        }
        else // Look for H1 "# " in MD files.
        {
            int metadataDelimiterCount = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                // Ensure that the H1 used is the first H1 outside of the metadata block. Some
                // docs include an H1 with a customer intent statement in the metadata block.
                if (line.Trim() == "---")
                    metadataDelimiterCount++;
                else if (metadataDelimiterCount == 2 && line.StartsWith("# "))
                    return line[2..^0].Trim();
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Retrieve the "uid" metadata value from the raw content.
    /// </summary>
    /// <returns>The "uid" value (without the "uid:" prefix).</returns>
    public static string RetrieveUidFromFile(string filePath)
    {
        foreach (var line in File.ReadLines(filePath))
        {
            if (line.StartsWith("uid: "))
                return line[5..^0].Trim();
        }

        return string.Empty;
    }
}
