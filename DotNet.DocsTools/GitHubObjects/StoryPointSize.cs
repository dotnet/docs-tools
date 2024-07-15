using DotNetDocs.Tools.GraphQLQueries;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// Record type to store a project name / story point size pair
/// </summary>
public record StoryPointSize
{
    private static readonly Dictionary<string, int> s_months = new()
    {
        ["Jan"] = 1, // 3
        ["Feb"] = 2, // 3
        ["Mar"] = 3, // 3
        ["Apr"] = 4, // 4
        ["May"] = 5, // 4
        ["Jun"] = 6, // 4
        ["Jul"] = 7, // 1
        ["Aug"] = 8, // 1
        ["Sep"] = 9, // 1
        ["Oct"] = 10, // 2
        ["Nov"] = 11, // 2
        ["Dec"] = 12  // 2
    };

    public static int MonthOrdinal(string month) => s_months[month];

    public static bool TryGetMonthOrdinal(string month, out int ordinal)
        => s_months.TryGetValue(month, out ordinal);

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

    public bool IsPastIteration
    {
        get
        {
            var currentYear = int.Parse(DateTime.Now.ToString("yyyy"));
            var currentMonth = MonthOrdinal(DateTime.Now.ToString("MMM"));

            if (CalendarYear < currentYear)
            {
                return true;
            } else if (CalendarYear == currentYear)
            {
                return MonthOrdinal(Month) < currentMonth;
            }
            return false;
        }
    }
}

