using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// The query variables for file history
/// </summary>
/// <param name="owner">The GitHub org for the repository</param>
/// <param name="repo">The repository name</param>
/// <param name="path">The path to the file</param>
public readonly record struct FileHistoryVariables(string owner, string repo, string path);

public class FileHistory : IGitHubQueryResult<FileHistory, FileHistoryVariables>
{
    private const string FileHistoryQueryText = """
        query FileHistory($owner: String!, $repo: String!, $path: String!, $cursor: String) {
          repository(owner: $owner, name: $repo) {
            defaultBranchRef {
              target {
                ... on Commit {
                  history(
                    first: 25
                    after: $cursor
                    path: $path
                  ) {
                    nodes {
                      committedDate
                      changedFilesIfAvailable
                      additions
                      deletions
                    }
                    pageInfo {
                      hasNextPage
                      endCursor
                    }
                  }
                }
              }
            }
          }
        }
        """;


    public DateTime CommittedDate { get; }

    public int? ChangedFilesIfAvailable { get; }

    public int Additions { get; }

    public int Deletions { get; }
    private FileHistory(JsonElement element)
    {
        CommittedDate = ResponseExtractors.DateTimeProperty(element, "committedDate");
        ChangedFilesIfAvailable = ResponseExtractors.IntProperty(element, "changedFilesIfAvailable");
        Additions = ResponseExtractors.IntProperty(element, "additions");
        Deletions = ResponseExtractors.IntProperty(element, "deletions");
    }

    public static GraphQLPacket GetQueryPacket(FileHistoryVariables variables, bool isScalar) =>
        (isScalar)
        ? throw new InvalidOperationException("This query is not a scalar query")
        : new()
        {
            query = FileHistoryQueryText,
            variables =
            {
                ["owner"] = variables.owner,
                ["repo"] = variables.repo,
                ["path"] = variables.path,
            }
        };

    public static IEnumerable<string> NavigationToNodes(bool isScalar) =>
        (isScalar)
        ? throw new InvalidOperationException("This query is not a scalar query")
        : ["repository", "defaultBranchRef", "target", "history"];

    public static FileHistory? FromJsonElement(JsonElement element, FileHistoryVariables variables) =>
                new FileHistory(element);
}
