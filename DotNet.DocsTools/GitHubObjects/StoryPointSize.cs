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
            string? projectTitle = projectItem.Descendent("project", "title").GetString();

            Console.WriteLine($"Project title: {projectTitle}");

            // size may or may not have been set yet:
            string size = "🐂 Medium";
            string? sprintMonth = default;
            foreach (JsonElement field in projectItem.Descendent("fieldValues", "nodes").EnumerateArray())
            {
                if (field.TryGetProperty("name", out JsonElement fieldValue))
                {
                    string? fieldName = field.Descendent("field", "name").GetString();
                    if (fieldName == "Sprint") sprintMonth = fieldValue.GetString();
                    if (fieldName == "Size") size = fieldValue.GetString() ?? "🐂 Medium";
                }
            }
            if ((projectTitle is not null) &&
                projectTitle.Contains("sprint", StringComparison.CurrentCultureIgnoreCase))
            {
                string[] components = projectTitle.Split(' ');
                int yearIndex = (sprintMonth is null) ? 2 : 1;
                // Should be in a project variable named "Sprint", take substring 0,3
                string month = sprintMonth ?? components[1];
                if (int.TryParse(components[yearIndex], out int year))
                {
                    sz = new StoryPointSize(year, month.Substring(0, 3), size);
                }
            }
        } else
        {
            Console.WriteLine("Expect project node wasn't a JSON object");
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

