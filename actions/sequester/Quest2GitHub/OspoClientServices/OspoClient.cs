// Taken from https://github.com/dotnet/org-policy/tree/main/src/Microsoft.DotnetOrg.Ospo

using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Quest2GitHub.Serialization;
using System.Net.Http.Json;

namespace Microsoft.DotnetOrg.Ospo;

public sealed class OspoClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, OspoLink?> _allEmployeeQueries = new();
    private readonly AsyncRetryPolicy _retryPolicy;

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

        var delay = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromSeconds(15), retryCount: 5);

        _retryPolicy = Policy
            .Handle<HttpRequestException>(ex =>
            {
                Console.WriteLine($"::warning::{ex}");
                return true;
            })
            .WaitAndRetryAsync(delay);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<OspoLink?> GetAsync(string gitHubLogin)
    {
        if (_allEmployeeQueries.TryGetValue(gitHubLogin, out var query))
            return query;

        var result = await _retryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            var link = await _httpClient.GetFromJsonAsync<OspoLink>(
            $"people/links/github/{gitHubLogin}", JsonSerializerOptionsDefaults.Shared);
            return link;
        });

        return result.Result;
    }
    
    public async Task<OspoLinkSet> GetAllAsync()
    {
        var result = await _retryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            var links = await _httpClient.GetFromJsonAsync<IReadOnlyList<OspoLink>>(
            $"people/links", JsonSerializerOptionsDefaults.Shared);

            var linkSet = new OspoLinkSet
            {
                Links = links ?? Array.Empty<OspoLink>()
            };

            linkSet.Initialize();
            return linkSet;
        });

        return result.Result;
    }
}
