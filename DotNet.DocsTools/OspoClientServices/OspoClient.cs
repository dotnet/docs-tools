// Taken from https://github.com/dotnet/org-policy/tree/main/src/Microsoft.DotnetOrg.Ospo

using DotNetDocs.Tools.Serialization;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Microsoft.DotnetOrg.Ospo;

public sealed class OspoClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private OspoLinkSet? _allLinks = default;
    private readonly Dictionary<string, OspoLink?> _allEmployeeQueries = new();
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly bool _useAllCache;

    public OspoClient(string token, bool useAllCache)
    {
        ArgumentNullException.ThrowIfNull(token);
        _useAllCache = useAllCache;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://repos.opensource.microsoft.com/api/")
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("api-version", "2019-10-01");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{token}")));

        var delay = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromMinutes(3), retryCount: 2);

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
        if ((_useAllCache) || (_allLinks is not null))
        {
            _allLinks = _allLinks ?? await GetAllAsync();
            return _allLinks.LinkByLogin.GetValueOrDefault(gitHubLogin); 
        }

        if (_allEmployeeQueries.TryGetValue(gitHubLogin, out var query))
            return query;

        var result = await _retryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            var link = await _httpClient.GetFromJsonAsync<OspoLink>(
            $"people/links/github/{gitHubLogin}", JsonSerializerOptionsDefaults.Shared);
            return link;
        });
        if (result.Outcome == OutcomeType.Failure)
        {
            throw new InvalidOperationException("OSPO REST API failure. Check access token rights");
        }

        return result.Result;
    }
    
    public async Task<OspoLinkSet> GetAllAsync()
    {
        if (_allLinks is not null)
            return _allLinks;
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

        if (result.Outcome == OutcomeType.Failure)
        {
            throw new InvalidOperationException("OSPO REST API failure. Check access token rights");
        }

        _allLinks = result.Result;
        return result.Result;
    }
}
