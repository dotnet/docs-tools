namespace Quest2GitHub.Models;

public class QuestIteration
{
    public required int Id { get; init; }
    public required Guid Identifier { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }

    public bool IsInSemester(string semesterName) => Path.Contains(semesterName);

    public static QuestIteration? CurrentIteration(IEnumerable<QuestIteration> iterations)
    {
        var currentYear = int.Parse(DateTime.Now.ToString("yyyy"));
        var currentMonth = DateTime.Now.ToString("MMM");
        return IssueExtensions.ProjectIteration(currentMonth, currentYear, iterations);
    }

    public static QuestIteration? FutureIteration(IEnumerable<QuestIteration> iterations)
        => iterations.SingleOrDefault(sprint => sprint.Name is "Future");

    override public string ToString() => $"{Identifier} {Id} {Name} ({Path})";

}
