using Microsoft.DotnetOrg.Ospo;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// The Actor node of a PR
/// </summary>
/// <remarks>
/// This node contains the login, and name for this account.
/// If the user has deleted their acount (where the GitHub UI shows "Ghost")
/// then both properties are null.
/// </remarks>
public sealed record Actor
{
    public static Actor? FromJsonElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Null => null,
            _ => new Actor(element),
        };

    private Actor(JsonElement authorNode)
    {
        Login = GetLogin(authorNode)!;
        Name = GetOptionalName(authorNode)!;
    }

    /// <summary>
    /// Access the GitHub login for this actor
    /// </summary>
    /// <remarks>
    /// This is null if the actor has deleted their account.
    /// </remarks>
    public string Login { get; }

    /// <summary>
    /// Access the name for this actor.
    /// </summary>
    /// <remarks>
    /// Not all GitHub accounts make the owner's name public.
    /// In that case, the GraphQL node is a null node. The login is
    /// non-null in those cases.
    /// </remarks>
    public string Name { get; }

    private static string? GetLogin(JsonElement authorNode) => authorNode.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.Object => ResponseExtractors.StringProperty(authorNode, "login"),
        _ => null,
    };

    private static string? GetOptionalName(JsonElement authorNode) => authorNode.ValueKind switch
    {
        JsonValueKind.Null => string.Empty,
        JsonValueKind.Object => ResponseExtractors.OptionalStringProperty(authorNode, "name"),
        _ => string.Empty,
    };

}
