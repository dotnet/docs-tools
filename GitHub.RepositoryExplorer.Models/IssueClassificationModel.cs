using Microsoft.Azure.CosmosRepository;
using Microsoft.Azure.CosmosRepository.Attributes;
using System.Text.Json.Serialization;

namespace GitHub.RepositoryExplorer.Models;


// Null for any value means "total"
// - means "none of these labels"

// Storage record is:
// Date
// Prod *
// Technology
// Priority *
// Classification
// Count

// TODO: Refactor and organize


// Used for storage:
public record IssueCount(
    string? Product,
    string? Technology,
    string? Priority,
    string? Classification,
    int Issues);

// Query by Product, Technology, Priority, Classification
// example: Null,    null,       Pri1,     Null   => All priority 1 issues
// example: *,       null,       Pri1,     Null   => All priority 1 issues with no product assigned

public record ProductIssueCount( // 10
    string? Product,
    TechnologyIssueCount[] Technologies);

public record TechnologyIssueCount( // 5 for any given product
    string? Technology,  // null means all technologies, '*' means unassigned.
    PriorityIssueCount[] Priorities);

public record PriorityIssueCount( // 6
    string? Priority, // one of "Pri0", "Pri1", "Pri2", Pri3", 
    ClassificationIssueCount[] Classifications // one of "doc-bug", "doc-enhancement", "doc-idea"
);

public record ClassificationIssueCount( // 5
    string? Classification,
    int Issues);

[PartitionKeyPath("/orgAndRepo")]
public class DailyRecord : Item
{
    public string OrgAndRepo { get; set; } = null!;

    [JsonConverter(typeof(DateOnlyConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftDateOnlyConverter))]
    public DateOnly Date { get; set; }
    public ProductIssueCount[] Issues { get; set; } = Array.Empty<ProductIssueCount>();

    protected override string GetPartitionKeyValue() => OrgAndRepo;
}