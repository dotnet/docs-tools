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
    private readonly string _questOrg;

    public string QuestProject { get; }

    /// <summary>
    /// Create the quest client services object
    /// </summary>
    /// <param name="token">The personal access token</param>
    /// <param name="org">The Azure devops organization</param>
    /// <param name="project">The Azure devops project</param>
    public QuestClient(string token, string org, string project)
    {
        _questOrg = org;
        QuestProject = project;
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($":{token}")));
    }

    /// <summary>
    /// Create a work item from an array of JsonPatch documents.
    /// </summary>
    /// <param name="document">The Json patch document that represents
    /// the new item.</param>
    /// <returns>The JSON packet representing the new item.</returns>
    public async Task<JsonElement> CreateWorkItem(List<JsonPatchDocument> document)
    {
        var json = JsonSerializer.Serialize(document, s_options);
        Console.WriteLine($"Creating work item with:\n{json}");

        using var request = new StringContent(json);
        request.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
        request.Headers.Add("Accepts", MediaTypeNames.Application.Json);

        string createWorkItemUrl =
            $"https://dev.azure.com/{_questOrg}/{QuestProject}/_apis/wit/workitems/$User%20Story?api-version=6.0&expand=Fields";
        Console.WriteLine($"Create work item URL: \"{createWorkItemUrl}\"");

        var response = await _client.PostAsync(createWorkItemUrl, request);
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
            $"https://dev.azure.com/{_questOrg}/{QuestProject}/_apis/wit/workitems/{id}?api-version=6.0&expand=Fields";
        Console.WriteLine($"Get work item URL: \"{getWorkItemUrl}\"");

        using var response = await _client.GetAsync(getWorkItemUrl);
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
        var json = JsonSerializer.Serialize(document, s_options);
        Console.WriteLine($"Patching {id} with:\n{json}");

        using var request = new StringContent(json);
        request.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
        request.Headers.Add("Accepts", MediaTypeNames.Application.Json);

        string patchWorkItemUrl = 
            $"https://dev.azure.com/{_questOrg}/{QuestProject}/_apis/wit/workitems/{id}?api-version=6.0&expand=Fields";
        Console.WriteLine($"Patch work item URL: \"{patchWorkItemUrl}\"");

        var response = await _client.PatchAsync(patchWorkItemUrl, request);
        return await HandleResponseAsync(response);
    }

    static async Task<JsonElement> HandleResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return jsonDocument.RootElement;
        }
        else
        {
            var text = $"HTTP error:\n{response}";
            var content = await response.Content.ReadAsStringAsync();
            text += $"\nContent: \"{content}\"";

            throw new InvalidOperationException(text);
        }
    }

    public async Task<AzDoIdentity?> GetIDFromEmail(string emailAddress)
    {
        string url = $"https://vssps.dev.azure.com/{_questOrg}/_apis/identities?searchFilter=General&filterValue={emailAddress}&queryMembership=None&api-version=7.1-preview.1";
        var response = await _client.GetAsync(url);
        var rootElement = await HandleResponseAsync(response);
        var count = rootElement.Descendent("count").GetInt32();
        if (count != 1)
        {
            return null;
        }
        var values = rootElement.Descendent("value");
        var user = values.EnumerateArray().First();
        bool success = user.Descendent("id").TryGetGuid(out var id);
        var uniqueName = user.Descendent("properties", "Account", "$value").GetString()!;
        // When retrieving the user identity, the property is called "subjectDescriptor".
        // But, when sending a user ID, the property name is "descriptor".
        var descriptor = user.Descendent("subjectDescriptor").GetString()!;
        var identity = new AzDoIdentity { Id = id, UniqueName = uniqueName, Descriptor = descriptor };
        return success ? identity : null;
    }

    /// <summary>
    /// Dispose of the embedded HTTP client.
    /// </summary>
    public void Dispose() => _client.Dispose();
}
