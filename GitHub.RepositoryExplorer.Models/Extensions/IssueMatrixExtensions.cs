namespace GitHub.RepositoryExplorer.Models;

public static class IssueMatrixExtensions
{
    // Return the issue count for a given tuple of values.
    // null means the total
    // * means unassigned.

    public static int IssueCount(this IEnumerable<ProductIssueCount> daily,
        string? product,
        string? technology,
        string? priority,
        string? classification) => daily.SingleOrDefault(p => p.Product == product)
            ?.Technologies?.SingleOrDefault(t => t.Technology == technology)
            ?.Priorities?.SingleOrDefault(p => p.Priority == priority)
            ?.Classifications?.SingleOrDefault(c => c.Classification == classification)
            ?.Issues ?? -1;

    public static IEnumerable<(string? prod, string? tech, string? pri, string? cl, int count)> AllItems(this DailyRecord daily)
    {
        foreach (var product in daily.Issues)
        {
            foreach (var technology in product.Technologies)
            {
                foreach (var priority in technology.Priorities)
                {
                    foreach (var item in priority.Classifications)
                    {
                        yield return (product.Product, technology.Technology, priority.Priority, item.Classification, item.Issues);
                    }
                }
            }
        }
    }

    // There should be a way to make these into 2 generic methods, but I haven't figured out how yet.
    public static IEnumerable<Product> ProductWithUnassigned(this IssueClassificationModel model) => model.Products
            .Append(new Product { Label = "*", DisplayLabel = "Unassigned" });
    public static IEnumerable<Priority> PriorityWithUnassigned(this IssueClassificationModel model) => model.Priorities
            .Append(new Priority { Label = "*", DisplayLabel = "Unassigned" });

    public static IEnumerable<Classification> ClassificationWithUnassigned(this IssueClassificationModel model) => model.Classification
            .Append(new Classification { Label = "*", DisplayLabel = "Unassigned" });
    public static IEnumerable<Product> ProductWithUnassignedAndTotal(this IssueClassificationModel model) => model.Products
            .Append(new Product { Label = "*", DisplayLabel = "Unassigned" })
            .Append(new Product { Label = null!, DisplayLabel = "Total" });

    public static IEnumerable<Priority> PriorityWithUnassignedAndTotal(this IssueClassificationModel model) => model.Priorities
            .Append(new Priority { Label = "*", DisplayLabel = "Unassigned" })
            .Append(new Priority { Label = null!, DisplayLabel = "Total" });

    public static IEnumerable<Classification> ClassificationWithUnassignedAndTotal(this IssueClassificationModel model) => model.Classification
            .Append(new Classification { Label = "*", DisplayLabel = "Unassigned" })
            .Append(new Classification { Label = null!, DisplayLabel = "Total" });

}
