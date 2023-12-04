using DotNet.DocsTools.GitHubObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DotnetDocsTools.Tests.GraphQLProcessingTests
{
    public class QuestIssueTests
    {
        // To make it easier to write tests, define
        // constants for each property, and then compose
        // the full packet from those contents:
        private const string authorLogin = "BillWagner";
        private const string authorName = "Bill Wagner";
        // JSON with text requires two constants. The `\` must be in the packet source, but they are removed when 
        // the element is pulled from the JSON.
        private const string bodyHTMLSource = """<p dir=\"auto\">Now that C# 12 has shipped, remove the <code class=\"notranslate\">&lt;LangVersion&gt;preview&lt;/Langversion&gt;</code> from all C# sample files.</p>""";
        private const string bodyHTMLResult = """<p dir="auto">Now that C# 12 has shipped, remove the <code class="notranslate">&lt;LangVersion&gt;preview&lt;/Langversion&gt;</code> from all C# sample files.</p>""";

        private static readonly string[] labelNames = ["Pri1", "in-pr", "okr-health", ":world_map: reQUEST"];
        private static readonly string[] labelIds = ["MDU6TGFiZWwyNDYyNTI4ODY2", "LA_kwDOAiOjoc8AAAABB4U2LA", "LA_kwDOAiOjoc8AAAABDHlPfQ", "LA_kwDOAiOjoc8AAAABGxZeDw"];

        private static readonly string ValidOpenIssueResult = $$"""
        {
            "id": "I_kwDOAiOjoc54eAkz",
            "number": 38544,
            "title": "Remove preview LangVer",
            "state": "OPEN",
            "author": {
                "login": "{{authorLogin}}",
                "name": "{{authorName}}"
            },
            "timelineItems": {
                "nodes": [
                    {},
                    {},
                    {},
                    {},
                    {}
                ]
            },
            "projectItems": {
                "nodes": [
                    {
                        "fieldValues": {
                            "nodes": [
                                {},
                                {},
                                {},
                                {},
                                {},
                                {
                                    "field": {
                                        "name": "Status"
                                    },
                                    "name": "🏗 In progress"
                                },
                                {
                                    "field": {
                                        "name": "Priority"
                                    },
                                    "name": "🏔 High"
                                },
                                {
                                    "field": {
                                        "name": "Size"
                                    },
                                    "name": "🦔 Tiny (4h)"
                                }
                            ]
                        },
                        "project": {
                            "title": "dotnet/docs December 2023 sprint"
                        }
                    }
                ]
            },
            "bodyHTML": "{{bodyHTMLSource}}",
            "body": "Now that C# 12 has shipped, remove the `<LangVersion>preview</Langversion>` from all C# sample files.",
            "assignees": {
                "nodes": [
                    {
                        "login": "{{authorLogin}}",
                        "name": "{{authorName}}"
                    }
                ]
            },
            "labels": {
                "nodes": [
                    {
                        "name": "{{labelNames[0]}}",
                        "id": "{{labelIds[0]}}"
                    },
                    {
                        "name": "{{labelNames[1]}}",
                        "id": "{{labelIds[1]}}"
                    },
                    {
                        "name": "{{labelNames[2]}}",
                        "id": "{{labelIds[2]}}"
                    },
                    {
                        "name": "{{labelNames[3]}}",
                        "id": "{{labelIds[3]}}"
                    }
                ]
            },
            "comments": {
                "nodes": []
            }
        }
        """;

        [Fact]
        public void OpenQuestIssueFromJsonNode()
        {
            var variables = new QuestIssueVariables
            {
                Organization = "dotnet",
                Repository = "docs",
                issueNumber = 38544
            };

            JsonElement element = JsonDocument.Parse(ValidOpenIssueResult).RootElement;
            var issue = QuestIssue.FromJsonElement(element, variables);
            Assert.NotNull(issue);
            Assert.True(issue.IsOpen);
            Assert.Equal($"{authorLogin} - {authorName}", issue.Author);
            Assert.Equal(bodyHTMLResult, issue.BodyHtml);
            Assert.Single(issue.Assignees);
            Assert.Equal(authorLogin, issue.Assignees.First());
            Assert.Equal(4, issue.Labels.Length);
            for (int i = 0; i < issue.Labels.Length; i++)
            {
                Assert.Equal(labelNames[i], issue.Labels[i].Name);
                Assert.Equal(labelIds[i], issue.Labels[i].Id);
            }
            Assert.Empty(issue.Comments);
            Assert.Equal("<a href = \"https://github.com/dotnet/docs/issues/38544\">\r\n    dotnet/docs#38544\r\n</a>", issue.LinkText);
            // Skip date time. For a single issue, we don't retrieve it.
            // Assert.Equal(DateTime.Now, issue.UpdatedAt);
            Assert.Single(issue.ProjectStoryPoints);
            StoryPointSize points = issue.ProjectStoryPoints.First();
            Assert.Equal(2023, points.CalendarYear);
            Assert.Equal("Dec", points.Month);
            Assert.Equal("🦔 Tiny (4h)", points.Size);
            Assert.Null(issue.ClosingPRUrl);
        }

        // 2. Test with closed issue
        // 3. Test with no project data
        // 4. Start making invalid responses.
    }
}
