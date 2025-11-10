using System.Net.Http;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

namespace Quest2GitHub.AzureDevOpsCommunications;

/// <summary>
/// The client services to work with Azure Dev ops.
/// </summary>
/// <remarks>
/// Azure DevOps tokens are scoped to the org / project so pass them in at construction time.
/// You'd need to have a second client with a different token if you were passing objects
/// between orgs.
/// </remarks>
public sealed class QuestClient : IDisposable
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly HttpClient _client;
    private readonly AsyncRetryPolicy _retryPolicy;

    public string QuestOrg { get; }
    public string QuestProject { get; }

    /// <summary>
    /// Create the quest client services object
    /// </summary>
    /// <param name="token">The personal access token</param>
    /// <param name="org">The Azure DevOps organization</param>
    /// <param name="project">The Azure DevOps project</param>
    /// <param name="useBearerToken">True to use a just in time bearer token, false assumes PAT</param>
    public QuestClient(string token, string org, string project, bool useBearerToken)
    {
        QuestOrg = org;
        QuestProject = project;
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _client.DefaultRequestHeaders.Authorization = useBearerToken ?
            new AuthenticationHeaderValue("Bearer", token) :
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($":{token}")));

        IEnumerable<TimeSpan> delay = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromSeconds(15), retryCount: 5);
        _retryPolicy = Policy
            .Handle<HttpRequestException>(ex =>
            {
                Console.WriteLine($"::warning::{ex}");
                return true;
            })
            .WaitAndRetryAsync(delay);
    }

    /// <summary>
    /// Retrieve the JsonElement for all iterations in the project
    /// </summary>
    /// <returns>The JSON packet containing all iterations</returns>
    public async Task<JsonElement> RetrieveAllIterations()
    {
        string getIterationsUrl =
            $"https://dev.azure.com/{QuestOrg}/{QuestProject}/_apis/wit/classificationnodes?$depth=3&api-version=7.1";

        using HttpResponseMessage response = await InitiateRequestAsync(
            client => client.GetAsync(getIterationsUrl));

        return await HandleResponseAsync(response);

    }

    /// <summary>
    /// Create a work item from an array of JsonPatch documents.
    /// </summary>
    /// <param name="document">The Json patch document that represents
    /// the new item.</param>
    /// <returns>The JSON packet representing the new item.</returns>
    public async Task<JsonElement> CreateWorkItem(List<JsonPatchDocument> document)
    {
        string? json = JsonSerializer.Serialize(document, s_options);
        // Console.WriteLine($"Creating work item with:\n{json}");

        using var request = new StringContent(json);
        request.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
        request.Headers.Add("Accepts", MediaTypeNames.Application.Json);

        string createWorkItemUrl =
            $"https://dev.azure.com/{QuestOrg}/{QuestProject}/_apis/wit/workitems/$User%20Story?api-version=7.1&$expand=relations";
        // Console.WriteLine($"Create work item URL: \"{createWorkItemUrl}\"");

        using HttpResponseMessage response = await InitiateRequestAsync(client =>
            client.PostAsync(createWorkItemUrl, request));
        
        return await HandleResponseAsync(response);
    }

    /// <summary>
    /// Retrieve a work item from ID
    /// </summary>
    /// <param name="id">The ID</param>
    /// <returns>The JSON element for the returned item.</returns>
    public async Task<JsonElement> GetWorkItem(int id)
    {
        string getWorkItemUrl = 
            $"https://dev.azure.com/{QuestOrg}/{QuestProject}/_apis/wit/workitems/{id}?$expand=relations";
        Console.WriteLine($"Get work item URL: \"{getWorkItemUrl}\"");

        using HttpResponseMessage response = await InitiateRequestAsync(
            client => client.GetAsync(getWorkItemUrl));
        
        return await HandleResponseAsync(response);
    }

    /// <summary>
    /// Update a Quest work item.
    /// </summary>
    /// <param name="id">The work item ID</param>
    /// <param name="document">The Patch document that enumerates the updates.</param>
    /// <returns>The JSON element that represents the updated work item.</returns>
    public async Task<JsonElement> PatchWorkItem(int id, List<JsonPatchDocument> document)
    {
        string? json = JsonSerializer.Serialize(document, s_options);
        // Console.WriteLine($"Patching {id} with:\n{json}");

        using var request = new StringContent(json);
        request.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
        request.Headers.Add("Accepts", MediaTypeNames.Application.Json);

        string patchWorkItemUrl = 
            $"https://dev.azure.com/{QuestOrg}/{QuestProject}/_apis/wit/workitems/{id}?api-version=7.1&$expand=relations";
        // Console.WriteLine($"Patch work item URL: \"{patchWorkItemUrl}\"");

        using HttpResponseMessage response = await InitiateRequestAsync(
            client => client.PatchAsync(patchWorkItemUrl, request));

        return await HandleResponseAsync(response);
    }
    
    async Task<HttpResponseMessage> InitiateRequestAsync(
        Func<HttpClient, Task<HttpResponseMessage>> httpFunc)
    {
        PolicyResult<HttpResponseMessage> result = 
            await _retryPolicy.ExecuteAndCaptureAsync(() => httpFunc(_client));
        
        return result.Result;
    }

    static async Task<JsonElement> HandleResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            // Temporary debugging code:

            string packet = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response: {packet}");
            JsonDocument jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return jsonDocument.RootElement;
        }
        else
        {
            string? text = $"HTTP error:\n{response}";
            string? content = await response.Content.ReadAsStringAsync();
            text += $"\nContent: \"{content}\"";

            throw new InvalidOperationException(text);
        }
    }

    public async Task<AzDoIdentity?> GetIDFromEmail(string emailAddress)
    {
        string url = $"https://vssps.dev.azure.com/{QuestOrg}/_apis/identities?searchFilter=General&filterValue={emailAddress}&queryMembership=None&api-version=7.1-preview.1";
        using HttpResponseMessage response = await InitiateRequestAsync(
            client => client.GetAsync(url));

        JsonElement rootElement = await HandleResponseAsync(response);
        int count = rootElement.Descendent("count").GetInt32();
        if (count != 1)
        {
            return null;
        }
        JsonElement values = rootElement.Descendent("value");
        JsonElement user = values.EnumerateArray().First();
        bool success = user.Descendent("id").TryGetGuid(out Guid id);
        string? uniqueName = user.Descendent("properties", "Account", "$value").GetString()!;
        // When retrieving the user identity, the property is called "subjectDescriptor".
        // But, when sending a user ID, the property name is "descriptor".
        string? descriptor = user.Descendent("subjectDescriptor").GetString()!;
        var identity = new AzDoIdentity { Id = id, UniqueName = uniqueName, Descriptor = descriptor };
        return success ? identity : null;
    }

    /// <summary>
    /// Dispose of the embedded HTTP client.
    /// </summary>
    public void Dispose() => _client.Dispose();
}
