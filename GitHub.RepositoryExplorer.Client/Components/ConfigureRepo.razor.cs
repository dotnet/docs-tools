using GitHub.RepositoryExplorer.Client.Extensions;

namespace GitHub.RepositoryExplorer.Client.Components;

public sealed partial class ConfigureRepo
{
    private string? _organizationName;
    private string? _repositoryName;
    private bool _isEditing = false;
    private ErrorBoundary? _errorBoundary;

    [Inject]
    public AppInMemoryStateService AppState { get; set; } = null!;

    [Inject]
    public ILocalStorageService LocalStorage { get; set; } = null!;

    [Parameter, EditorRequired]
    public RenderFragment PostConfigurationContent { get; set; } = null!;

    [Parameter]
    public RenderFragment? OptionalMessageContent { get; set; }

    public bool IsConfigured => AppState is { RepoState.IsAssigned: true };

    public string FullyQualifiedOrgAndRepo =>
        $"https://github.com/{_organizationName ?? "org"}/{_repositoryName ?? "repo"}";

    public void EditConfiguration() => _isEditing = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _organizationName = LocalStorage.TryGetItem<string>("github.org");
        _repositoryName = LocalStorage.TryGetItem<string>("github.repo");

        if (RepoConfigurationDispatched(false))
        {
            StateHasChanged();
        }
    }

    protected override void OnParametersSet() => _errorBoundary?.Recover();

    private void OnAssignClick()
    {
        _ = RepoConfigurationDispatched(true);
        _isEditing = false;
    }

    private bool RepoConfigurationDispatched(bool persistToLocalStorage)
    {
        if (_organizationName is null || _repositoryName is null)
        {
            return false;
        }

        if (persistToLocalStorage)
        {
            LocalStorage.TrySetItem("github.org", _organizationName);
            LocalStorage.TrySetItem("github.repo", _repositoryName);
        }

        AppState.RepoState = new Repository(
            _organizationName,
            _repositoryName);

        return true;
    }
}
