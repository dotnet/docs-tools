using ChartJs.Blazor;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor.LineChart;
using ChartJs.Blazor.Util;

namespace GitHub.RepositoryExplorer.Client.Pages;

public partial class ClassificationLineChart: ComponentBase
{
    private ConfigureRepo? _config;
    private RepoLabels _repoLabelsState = new();
    private IList<IssuesSnapshot>? _issueSnapshots;
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
    private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Today);

    private IList<DateOnly> DateRange
    {
        get
        {
            List<DateOnly> dates = new();
            var date = _date;
            while (date < _endDate)
            {
                dates.Add(date);
                date = date.AddDays(1);
            }
            return dates;
        }
    }

    private Chart? _chart;

    private readonly LineConfig _chartConfig = new()
    {
        Options = new LineOptions
        {
            Responsive = true,
            Title = new OptionsTitle
            {
                Display = true,
                Text = "Open issues by classification"
            },
            Tooltips = new Tooltips
            {
                Mode = InteractionMode.Nearest,
                Intersect = true
            },
            Hover = new Hover
            {
                Mode = InteractionMode.Nearest,
                Intersect = true
            },
            Legend = new Legend
            {
                Display = true,
                Position = Position.Top
            },
            // This is a hack to get stacked line graphs.
            // We create scales, using the Stacked Bar Graph style
            // so we can set the "stacked" property in the underlying
            // Chart.JS library to "true".
            Scales = new Scales
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

    [Inject]
    public AppInMemoryStateService AppState { get; set; } = null!;

    [Inject]
    public RepositoryLabelsClient RepositoryLabelsClient { get; set; } = null!;

    [Inject]
    public IssuesByClassificationClient SnapshotsClient { get; set; } = null!;

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

                var data =
                    await SnapshotsClient.GetIssuesForDateRangeAsync(state, _date, _endDate, _repoLabelsState);
                _issueSnapshots = data?.ToList() ?? Array.Empty<IssuesSnapshot>().ToList();
                if (_issueSnapshots is { Count: > 0 })
                {
                    _chartConfig.Data.Labels.Clear();
                    _chartConfig.Data.XLabels.Clear();
                    foreach (var date in DateRange)
                    {
                        _chartConfig.Data.XLabels.Add(date.ToShortDateString());
                    }

                    _chartConfig.Data.Datasets.Clear();

                    foreach (var grouping in _issueSnapshots)
                    {
                        var color = ColorUtil.RandomColorString();
                        double[] lineSeries = grouping.DailyCount
                            .Select(i => (i == -1) ? double.NaN : (double)i)
                            .ToArray();
                        _chartConfig.Data.Datasets.Add(
                            new LineDataset<double>(
                                lineSeries)
                            {
                                Fill = FillingMode.Start,
                                Label = classifications.ClassificationWithUnassigned().First(p => p.Label == grouping.Classification).DisplayLabel,
                                BorderColor = color,
                                BackgroundColor = color
                            });
                    }
                    StateHasChanged();
                }
            }
        }
    }

    private Task OnLoadClick() => LoadSummaryDataAsync();

}