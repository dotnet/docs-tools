// Taken from https://github.com/dotnet/org-policy/tree/main/src/Microsoft.DotnetOrg.Ospo

using Quest2GitHub.Serialization;
using System.Net.Http.Json;

namespace Microsoft.DotnetOrg.Ospo;

public sealed class OspoClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public OspoClient(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://repos.opensource.microsoft.com/api/")
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("api-version", "2019-10-01");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{token}")));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<OspoLink?> GetAsync(string gitHubLogin)
    {
        var result = await _httpClient.GetFromJsonAsync<OspoLink>(
            $"people/links/github/{gitHubLogin}", JsonSerializerOptionsDefaults.Shared);
        return result;
    }

    public async Task<OspoLinkSet> GetAllAsync()
    {
        var links = await _httpClient.GetFromJsonAsync<IReadOnlyList<OspoLink>>(
            $"people/links", JsonSerializerOptionsDefaults.Shared);

        var linkSet = new OspoLinkSet
        {
            Links = links ?? Array.Empty<OspoLink>()
        };

        linkSet.Initialize();
        return linkSet;
    }
}
