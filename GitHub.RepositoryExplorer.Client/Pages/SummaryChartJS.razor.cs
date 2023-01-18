using ChartJs.Blazor;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Enums;

namespace GitHub.RepositoryExplorer.Client.Pages;

// Using https://github.com/mariusmuntean/ChartJs.Blazor
public sealed partial class SummaryChartJS
{
    private ConfigureRepo? _config;
    private RepoLabels _repoLabelsState = new();
    private IssueSummary _summaryState = new();
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today);

    private readonly BarConfig _chartConfig = new()
    {
        Options = new BarOptions
        {
            Responsive = true,
            Title = new OptionsTitle
            {
                Display = true,
                Text = "Current open issus"
            },
            Tooltips = new Tooltips
            {
                Mode = InteractionMode.Index,
                Intersect = false
            },
            Scales = new BarScales
            {
                XAxes = new List<CartesianAxis>
                {
                    new BarCategoryAxis
                    {
                        Stacked = true
                    }
                },
                YAxes = new List<CartesianAxis>
                {
                    new BarLinearCartesianAxis
                    {
                        Stacked = true
                    }
                }
            }
        }
    };

    private Chart? _chart;

    [Inject]
    public AppInMemoryStateService AppState { get; set; } = null!;

    [Inject]
    public RepositoryLabelsClient RepositoryLabelsClient { get; set; } = null!;

    [Inject]
    public IssuesClient IssuesClient { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await LoadSummaryDataAsync();
        }
    }

    private async Task LoadSummaryDataAsync()
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

                var data = await IssuesClient.GetIssuesForDateAsync(state, _date);
                if (classifications is not null && data is { Issues.Length: > 0 })
                {
                    _summaryState = _summaryState with
                    {
                        IsLoading = false,
                        Date = _date,
                        Data = data.Issues
                    };

                    // Configure the chart here:
                    // classifications is the labels.
                    _chartConfig.Data.Labels.Clear();
                    foreach(var product in classifications.ProductWithUnassigned())
                    {
                        // Might be XLabels, or YLabels.
                        _chartConfig.Data.Labels.Add(product.DisplayLabel);
                    }
                    // _date is the date (to display)
                    _chartConfig.Data.Datasets.Clear();
                    // data is the DailyRecord.
                    foreach (var priority in _repoLabelsState.IssueClassification.PriorityWithUnassigned())
                    {
                        IDataset<int> dataset = new BarDataset<int>(classifications
                            .Products
                            .Select(p => data.Issues.IssueCount(p.Label, null, priority.Label, null)))
                        {
                            Label = priority.DisplayLabel,
                            BackgroundColor = PriorityColor(priority.Label)
                        };
                        _chartConfig.Data.Datasets.Add(dataset);

                    }
                    StateHasChanged();
                }
            }
        }
    }

    private Task OnLoadClick() => LoadSummaryDataAsync();

    private static string PriorityColor(string priorityLabel) =>
        priorityLabel switch
        {
            "Pri0" => "#b60205",
            "Pri1" => "#D93F0B",
            "Pri2" => "#FBCA04",
            "Pri3" => "#0E8A16",
            _ => "#666600",
        };
}
