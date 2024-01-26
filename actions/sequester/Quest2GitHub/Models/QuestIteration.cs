namespace Quest2GitHub.Models;

public class QuestIteration
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }

    public static QuestIteration? CurrentIteration(IEnumerable<QuestIteration> iterations)
    {
        var currentYear = int.Parse(DateTime.Now.ToString("yyyy"));
        var currentMonth = DateTime.Now.ToString("MMM");
        return IssueExtensions.ProjectIteration(currentMonth, currentYear, iterations);
    }

    override public string ToString() => $"{Id} {Name} ({Path})";

}
