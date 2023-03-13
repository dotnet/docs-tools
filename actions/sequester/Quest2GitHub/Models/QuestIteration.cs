namespace Quest2GitHub.Models;

public class QuestIteration
{
    private const string PathTeam = "Content";
    private const string YearPrefix = "CY_";
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }

    // Path format is "Content\\CY_YYYY\\MM MMM" (CY is calendar year)
    // For example: "Content\\CY_2023\\03 Mar"
    public static string CurrentIterationPath()
    {
        var currentYear = DateTime.Now.ToString("yyyy");
        var sprintName = DateTime.Now.ToString("MM MMM");
        return $"Content\\CY_{currentYear}\\{sprintName}";
    }

}
