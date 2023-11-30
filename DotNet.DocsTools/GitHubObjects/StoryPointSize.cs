using DotNetDocs.Tools.GraphQLQueries;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// Record type to store a project name / story point size pair
/// </summary>
public record StoryPointSize
{

    public static StoryPointSize? OptionalFromJsonElement(JsonElement projectItem)
    {
        StoryPointSize? sz = default;
        if (projectItem.ValueKind == JsonValueKind.Object)
        {
            // Modify the code to store the optional month in the tuple field.
            // Consider: Store YYYY, Month, Size as a threeple.
            var projectTitle = projectItem.Descendent("project", "title").GetString();
            // size may or may not have been set yet:
            string? size = default;
            string? sprintMonth = default;
            foreach (var field in projectItem.Descendent("fieldValues", "nodes").EnumerateArray())
            {
                if (field.TryGetProperty("name", out var fieldValue))
                {
                    var fieldName = field.Descendent("field", "name").GetString();
                    if (fieldName == "Sprint") sprintMonth = fieldValue.GetString();
                    if (fieldName == "Size") size = fieldValue.GetString();
                }
            }
            if ((projectTitle is not null) &&
                (size is not null) &&
                projectTitle.ToLower().Contains("sprint"))
            {
                string[] components = projectTitle.Split(' ');
                int yearIndex = (sprintMonth is null) ? 2 : 1;
                // Should be in a project variable named "Sprint", take substring 0,3
                var Month = sprintMonth ?? components[1].Substring(0, 3);
                int.TryParse(components[yearIndex], out var year);
                sz = new StoryPointSize(year, Month.Substring(0, 3), size);
            }
        }
        return sz;
    }

    private StoryPointSize(int CalendarYear, string Month, string Size)
    {
        this.CalendarYear = CalendarYear;
        this.Month = Month;
        this.Size = Size;
    }

    public int CalendarYear { get; }
    public string Month { get; }
    public string Size { get; }
}


