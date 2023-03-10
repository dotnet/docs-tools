using System.Web;

namespace GitHub.RepositoryExplorer.Client.Pages;

public sealed partial class Areas
{
    private static Func<Product, bool, string> _encode = (Product product, bool isDisplay) =>
    {
        string? value = isDisplay
            ? product.DisplayLabel ?? $"product-{product.GetHashCode()}"
            : product.Label ?? $"product-{product.GetHashCode()}";

        if (value.Contains(':'))
        {
            value = value[(value.LastIndexOf(":") + 1)..];
        }

        value = value.ToLower()
            .Replace(".", "")
            .Replace("/", "-")
            .Replace("'s", "-is")
            .Replace("#", "sharp")
            .Replace("*", $"all-{product.GetHashCode()}")
            .Replace(" ", "");

        return HttpUtility.UrlEncode(value);
    };

    private ConfigureRepo? _config;
    private readonly List<CellDetails> _modalData = new();
    private string _modalTitle = "Details";
    private RepoLabels _repoLabelsState = new();
    private global::IssueSummary _summaryState = new();

    [Inject]
    public AppInMemoryStateService AppState { get; set; } = null!;

    [Inject]
    public RepositoryLabelsClient RepositoryLabelsClient { get; set; } = null!;

    [Inject]
    public IssuesClient IssuesClient { get; set; } = null!;

    private readonly record struct CellDetails(string Title, int Count);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await LoadSummaryDataAsync(
                DateOnly.FromDateTime(DateTime.Now));
        }
    }

    private async Task LoadSummaryDataAsync(DateOnly date)
    {
        if (AppState is { RepoState.IsAssigned: true })
        {
            var state = AppState.RepoState;
            var classifications = await RepositoryLabelsClient.GetRepositoryLabelsAsync(state);
            if (classifications is not null)
            {
                _repoLabelsState = _repoLabelsState with
                {
                    IsLoading = false,
                    IssueClassification = classifications
                };
                StateHasChanged();

                var data = await IssuesClient.GetIssuesForDateAsync(state, date, classifications);
                if (data is { Issues.Length: > 0 })
                {
                    _summaryState = _summaryState with
                    {
                        IsLoading = false,
                        Date = date,
                        Data = data.Issues
                    };
                    StateHasChanged();
                }
            }
        }
    }

    private Task OnPreviousDayClick() =>
        LoadSummaryDataAsync(_summaryState.Date.AddDays(-1));

    private void PrepareModalData(Product product, Technology? tech, Priority priority)
    {
        _modalData.Clear();
        _modalTitle = tech is not null
            ? $"{product.DisplayLabel}/{tech.DisplayLabel}/{priority.DisplayLabel}"
            : $"{product.DisplayLabel}/{priority.DisplayLabel}";

        foreach (var issueType in _repoLabelsState.IssueClassification.ClassificationWithUnassignedAndTotal())
        {
            _modalData.Add(new CellDetails
            {
                Title = issueType.DisplayLabel,
                Count = _summaryState.Data.IssueCount(product.Label, tech?.Label, priority.Label, issueType.Label)
            });
        }
    }

    private Task OnNextDayClick()
    {
        if (_summaryState.Date < DateOnly.FromDateTime(DateTime.Today))
        {
            return LoadSummaryDataAsync(_summaryState.Date.AddDays(+1));
        }

        return Task.CompletedTask;
    }

    public string GetIssueCountForProductAndPriority(
        string productLabel, string priorityLabel)
    {
        var count = _summaryState.Data.IssueCount(productLabel, null, priorityLabel, null);
        return count == -1 ? "?" : $"{count:#,0}";
    }

    public string GetIssueCountForProductTechAndPriority(
        string productLabel, string techLabel, string priorityLabel)
    {
        int count = _summaryState.Data.IssueCount(productLabel, techLabel, priorityLabel, null);
        return count == -1 ? "?" : $"{count}";
    }
}
