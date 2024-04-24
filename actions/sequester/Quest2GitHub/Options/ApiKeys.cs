namespace Quest2GitHub.Options;

public sealed record class ApiKeys
{
    /// <summary>
    /// The GitHub token (or <c>${{ secrets.GITHUB_TOKEN }}</c>) used to read issues and update labels with.
    /// Consuming workflows should specify:
    /// <code>
    /// jobs:
    ///   build:
    ///   runs-on: ubuntu-latest
    ///   
    ///   permissions:    # Define permissions
    ///     issues: write # Enable reading issues, and applying labels
    /// </code>
    /// For more information, see <a href="https://docs.github.com/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idpermissions"></a>.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__ApiKeys__GitHubToken</c>:
    /// <code>
    /// env:
    ///   ImportOptions__ApiKeys__GitHubToken: ${{ secrets.GITHUB_TOKEN }}
    /// </code>
    /// </remarks>
    public required string GitHubToken { get; init; }

    /// <summary>
    /// The Microsoft Open Source Programs Office API key.
    /// </summary>
    /// <remarks>
    /// This key is deprecated. The PAT based authentication has been removed. Read it from
    /// the config to write a warning message to update to OIDC based authentication.
    /// </remarks>
    public string? OSPOKey { get; init; }

    /// <summary>
    /// The client ID for identifying this app with OSPO.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__ApiKeys__ClientId</c>:
    /// <code>
    /// env:
    ///   ImportOptions__ApiKeys__OSPOClientID: ${{ secrets.CLIENT_ID }}
    /// </code>
    /// </remarks>
    public string? OSPOClientID { get; init; }

    /// <summary>
    /// The Tenant ID for identifying this app with OSPO.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__ApiKeys__TenantId</c>:
    /// <code>
    /// env:
    ///   ImportOptions__ApiKeys__OSPOTenantID: ${{ secrets.TENANT_ID }}
    /// </code>
    /// </remarks>
    public string? OSPOTenantID{ get; init; }

    /// <summary>
    /// The resource for this app with OSPO.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__ApiKeys__ResourceAudience</c>:
    /// <code>
    /// env:
    ///   ImportOptions__ApiKeys__OSPOResourceAudience: ${{ secrets.OSMP_API_AUDIENCE }}
    /// </code>
    /// </remarks>
    public string? OSPOResourceAudience { get; init; }

    /// <summary>
    /// The Azure DevOps API key.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__ApiKeys__QuestKey</c>:
    /// <code>
    /// env:
    ///   ImportOptions__ApiKeys__QuestKey: ${{ secrets.QUEST_API_KEY }}
    /// </code>
    /// </remarks>
    public required string QuestKey { get; init; }

    /// <summary>
    /// The OAuth Private key to authenticate this app
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__ApiKeys__SequesterPrivateKey</c>:
    /// <code>
    /// env:
    ///   ImportOptions__ApiKeys__SequesterPrivateKey: ${{ secrets.SEQUESTER_PRIVATEKEY }}
    /// </code>
    /// </remarks>
    public required string SequesterPrivateKey { get; init; }

    /// <summary>
    /// The Sequester GitHub App ID
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__ApiKeys__SequesterAppID</c>:
    /// <code>
    /// env:
    ///   ImportOptions__ApiKeys__SequesterAppID: ${{ secrets.SEQUESTER_APPID }}
    /// </code>
    /// </remarks>
    public required int SequesterAppID { get; init; }
}
