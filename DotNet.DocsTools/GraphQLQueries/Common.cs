using System.Text.Json;

namespace DotnetDocsTools.GraphQLQueries;

internal static class Common
{
    // If this becomes public, add tests.
    internal static JsonElement Descendent(this JsonElement element, params string[] path)
    {
        foreach (var item in path)
        {
            if ((element.ValueKind == JsonValueKind.Null) || (!element.TryGetProperty(item, out element)))
                return default;
        }
        return element;
    }

    internal static (bool hasNext, string endCursor) NextPageInfo(this JsonElement pageInfoNode) =>
        (pageInfoNode.Descendent("pageInfo", "hasNextPage").GetBoolean(), 
        pageInfoNode.Descendent("pageInfo", "endCursor").GetString() ?? throw new InvalidOperationException("endCursor not present"));
}
