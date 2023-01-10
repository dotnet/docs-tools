namespace IssueCloser;

/// <summary>
/// Bot assigned priority
/// </summary>
public enum Priority
{
    /// <summary>
    /// none assigned
    /// </summary>
    Missing,
    /// <summary>
    /// Priority 1
    /// </summary>
    Pri1,
    /// <summary>
    /// Priority 2
    /// </summary>
    Pri2,
    /// <summary>
    /// Priority 3
    /// </summary>
    Pri3
}

/// <summary>
/// Methods to manage priorities from text labels on GraphQL objects
/// </summary>
public static class Priorities
{

    /// <summary>
    /// Retrieve the bot assigned priority label
    /// </summary>
    /// <param name="labels">the sequence of labels</param>
    /// <returns>The priority enum value for the issue</returns>
    public static Priority PriLabel(IEnumerable<string> labels)
    {
        foreach (var label in labels)
        {
            switch (label)
            {
                case "Pri1":
                    return Priority.Pri1;
                case "Pri2":
                    return Priority.Pri2;
                case "Pri3":
                    return Priority.Pri3;
            }
        }
        return Priority.Missing;
    }
}
