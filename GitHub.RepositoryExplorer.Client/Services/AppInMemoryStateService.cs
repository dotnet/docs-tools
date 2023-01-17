public sealed class AppInMemoryStateService
{
    private Repository? _repoState;
    public Repository? RepoState
    {
        get => _repoState;
        set
        {
            _repoState = value;
            NotifyStateChanged();
        }
    }


    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();
}