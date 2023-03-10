namespace GitHub.RepositoryExplorer.Client.Services
{
    public static class DailyRecordFactory
    {
        internal static DailyRecord CreateMissingRecord(DateOnly date, IssueClassificationModel model, string? org, string? repo)
        {

            var productIssueCounts = new List<ProductIssueCount>();
            foreach (var prod in model.Products.
                Append(new Product { Label = "*", DisplayLabel = "Unnassigned", Technologies = Array.Empty<Technology>() }).
                Append(new Product { Label = null!, DisplayLabel = "Total", Technologies = Array.Empty<Technology>() }))
            {
                var technologyIssueCounts = new List<TechnologyIssueCount>();
                foreach (var tech in prod.Technologies
                    .Append(new Technology { Label = null!, DisplayLabel = "Total" }))
                {
                    var priorityIssueCounts = new List<PriorityIssueCount>();
                    foreach (var priority in model.Priorities
                        .Append(new Priority { Label = "*", DisplayLabel = "Unassigned" })
                        .Append(new Priority { Label = null!, DisplayLabel = "Total" }))
                    {
                        var classificationIssueCounts = new List<ClassificationIssueCount>();
                        foreach (var classification in model.Classification
                        .Append(new Classification { Label = "*", DisplayLabel = "Unassigned" })
                        .Append(new Classification { Label = null!, DisplayLabel = "Total" }))
                        {
                            var issueCount = -1;
                            classificationIssueCounts.Add(new ClassificationIssueCount(classification.Label, issueCount));
                        }
                        priorityIssueCounts.Add(new PriorityIssueCount(priority.Label, classificationIssueCounts.ToArray()));
                    }
                    technologyIssueCounts.Add(new TechnologyIssueCount(tech.Label, priorityIssueCounts.ToArray()));
                }
                productIssueCounts.Add(new ProductIssueCount(prod.Label, technologyIssueCounts.ToArray()));
            }

            var document = new DailyRecord
            {
                OrgAndRepo = $"{org}/{repo}",
                Date = date,
                Issues = productIssueCounts.ToArray()
            };
            return document;
        }
    }
}
