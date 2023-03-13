namespace GitHub.RepositoryExplorer.Client.Extensions;

internal static class LocalStorageServiceExtensions
{
    internal static T? TryGetItem<T>(this ILocalStorageService localStorage, string key)
    {
        try
        {
            return localStorage.GetItem<T>(key);
        }
        catch
        {
            return default;
        }
    }

    internal static void TrySetItem<T>(this ILocalStorageService localStorage, string key, T value)
    {
        try
        {
            localStorage.SetItem<T>(key, value);
        }
        catch { }
    }

}
