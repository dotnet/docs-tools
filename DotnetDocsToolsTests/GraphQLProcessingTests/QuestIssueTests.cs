using DotNet.DocsTools.GitHubObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private const string bodyHTMLResult = """<p dir="auto">Now that C# 12 has shipped, remove the <code class="notranslate">&lt;LangVersion&gt;preview&lt;/Langversion&gt;</code> from all C# sample files.</p>""";
        private static readonly string[] labelNames = ["Pri1", "in-pr", "okr-health", ":world_map: reQUEST"];
        private static readonly string[] labelIds = ["MDU6TGFiZWwyNDYyNTI4ODY2", "LA_kwDOAiOjoc8AAAABB4U2LA", "LA_kwDOAiOjoc8AAAABDHlPfQ", "LA_kwDOAiOjoc8AAAABGxZeDw"];

        private const string closeBodyHTMLResult = """<p dir="auto">Now that C# 12 has shipped, remove the <code class="notranslate">&lt;LangVersion&gt;preview&lt;/Langversion&gt;</code> from all C# sample files.</p><hr><p dir="auto"><a href="https://dev.azure.com/msft-skilling/Content/_workitems/edit/187722" rel="nofollow">Associated WorkItem - 187722</a></p>""";
        private static readonly string[] closedLabelNames = ["resolved-by-customer", "Pri1", "okr-health", ":pushpin: seQUESTered"];
        private static readonly string[] closedLabelIds = ["MDU6TGFiZWw5MTg4NDAxNDg=", "MDU6TGFiZWwyNDYyNTI4ODY2", "LA_kwDOAiOjoc8AAAABDHlPfQ", "LA_kwDOAiOjoc8AAAABGxZkJw"];

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
            "bodyHTML": "<p dir=\"auto\">Now that C# 12 has shipped, remove the <code class=\"notranslate\">&lt;LangVersion&gt;preview&lt;/Langversion&gt;</code> from all C# sample files.</p>",
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

        private const string ValidClosedIssueResult = """
        {
            "id": "I_kwDOAiOjoc54eAkz",
            "number": 38544,
            "title": "Remove preview LangVer",
            "state": "CLOSED",
            "author": {
                "login": "BillWagner",
                "name": "Bill Wagner"
        },
        "timelineItems": {
            "nodes": [
                {},
                {},
                {
                    "closer": {
                        "url": "https://github.com/dotnet/docs/pull/38584"
                    }
                },
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
                                "name": "✅ Done"
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
        "bodyHTML": "<p dir=\"auto\">Now that C# 12 has shipped, remove the <code class=\"notranslate\">&lt;LangVersion&gt;preview&lt;/Langversion&gt;</code> from all C# sample files.</p><hr><p dir=\"auto\"><a href=\"https://dev.azure.com/msft-skilling/Content/_workitems/edit/187722\" rel=\"nofollow\">Associated WorkItem - 187722</a></p>",
        "body": "Now that C# 12 has shipped, remove the `<LangVersion>preview</Langversion>` from all C# sample files.\n\n\n---\n[Associated WorkItem - 187722](https://dev.azure.com/msft-skilling/Content/_workitems/edit/187722)",
        "assignees": {
            "nodes": [
                {
                "login": "BillWagner",
                "name": "Bill Wagner"
                }
            ]
        },
        "labels": {
            "nodes": [
                {
                "name": "resolved-by-customer",
                "id": "MDU6TGFiZWw5MTg4NDAxNDg="
                },
                {
                "name": "Pri1",
                "id": "MDU6TGFiZWwyNDYyNTI4ODY2"
                },
                {
                "name": "okr-health",
                "id": "LA_kwDOAiOjoc8AAAABDHlPfQ"
                },
                {
                "name": ":pushpin: seQUESTered",
                "id": "LA_kwDOAiOjoc8AAAABGxZkJw"
                }
            ]
        },
        "comments": {
            "nodes": []
        }
    }
    """;

        private const string IssueMissingFields = """
        {
            "id": "I_kwDOAiOjoc54eAkz",
            "number": 38544,
            "title": "UnUsed",
            "state": "OPEN",
            "author": {
                "login": "BillWagner",
                "name": "Bill Wagner"
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
            "nodes": []
        },
        "bodyHTML": "<p dir=\"auto\">Just a short description</p>",
        "body": "Just a short description",
        "assignees": {
            "nodes": []
        },
        "labels": {
            "nodes": []
        },
        "comments": {
            "nodes": []
        }
    }
    """;

        private const string IssueWithComments = """
        {
            "id": "I_kwDOEj6Ilc5rmSP4",
            "number": 857,
            "title": "C# 7.x §15.15.1 meaning of \"returns a value\" in async function",
            "state": "OPEN",
            "author": {
                "login": "KalleOlaviNiemitalo",
                "name": "Kalle Olavi Niemitalo"
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
            "bodyHTML": "<p dir=\"auto\">Just a short description</p>",
            "body": "**Describe the bug**\r\n\r\nIn the C# 7.x draft, the phrase \"returns a value\" is used in §15.6.1 (Methods / General) and §15.15.1 (Async functions / General), but not with the same meaning.\r\n\r\nIn §15.6.1:\r\n\r\n> If `ref` is present, the method is ***returns-by-ref***; otherwise, if *return_type* is `void`, the method is ***returns-no-value***; otherwise, the method is ***returns-by-value***.\r\n>\r\n> The *return_type* of a method declaration specifies the type of the result, if any, returned by the method. A returns-no-value method does not return a value. A returns-by-ref method returns a *variable_reference* ([§9.5](variables.md#95-variable-references)), that is optionally read-only. A returns-by-value method returns a value. If the declaration includes the `partial` modifier, then *return_type* shall be `void` ([§15.6.9](classes.md#1569-partial-methods)). If the declaration includes the `async` modifier then *return_type* shall be `void` or the method returns-by-value and the return type is a *task type* ([§15.15.1](classes.md#15151-general)).\r\n\r\ni.e. if the *return_type* of an async method is a *task type*, then it is a returns-by-value method and thus \"returns a value\".\r\n\r\nIn §15.15.1:\r\n\r\n> The *return_type* of an async method shall be either `void` or a ***task type***. For an async method that returns a value, a task type shall be generic. For an async method that does not return a value, a task type shall not be generic.\r\n\r\n**Example**\r\n\r\nThe following should be allowed, but is C.M() a method that \"returns a value\"?\r\n\r\n```csharp\r\nusing System.Threading.Tasks;\r\n\r\nclass C {\r\n    async Task M() {}\r\n}\r\n```\r\n\r\nAccording to §15.6.1, this method is *returns-by-value*, and thus \"returns a value\".\r\n\r\nHowever, if this is \"an async method that returns a value\", then §15.15.1 requires that \"a task type shall be generic\", which System.Threading.Tasks.Task is not.\r\n\r\n**Expected behavior**\r\n\r\nIn §15.15.1, use some other phrase than \"returns a value\".\r\n\r\n**Additional context**\r\n\r\nNone.\n\n\n---\n[Associated WorkItem - 187393](https://dev.azure.com/msft-skilling/Content/_workitems/edit/187393)",
            "assignees": {
                "nodes": [
                {
                    "login": "BillWagner",
                    "name": "Bill Wagner"
                }
                ]
            },
            "labels": {
                "nodes": [
                {
                    "name": "seQUESTered",
                    "id": "LA_kwDOEj6Ilc8AAAABIyr8bQ"
                }
                ]
            },
            "comments": {
                "nodes": [
                {
                    "author": {
                        "login": "KalleOlaviNiemitalo",
                        "name": "Kalle Olavi Niemitalo"
                    },
                    "bodyHTML": "<p dir=\"auto\">Could perhaps say \"has an <em>effective return type</em> (15.6.11) other than <code class=\"notranslate\">void</code>\", but I did not check whether this would result in a circular definition.</p>"
                },
                {
                    "author": {
                        "login": "jskeet",
                        "name": "Jon Skeet"
                    },
                    "bodyHTML": "<p dir=\"auto\">Again, punting - and this one has probably been broken since C# 5. (I suspect the eventual fix will be a matter of just a few words, but choosing those words will take time...)</p>"
                },
                {
                    "author": {
                        "login": "KalleOlaviNiemitalo",
                        "name": "Kalle Olavi Niemitalo"
                    },
                    "bodyHTML": "This was a big comment. And, tests have already verified that the JSON parser handles HTML."
                }
                ]
            }
        }
        """;

        private static readonly string MissingTimeLine = $$"""
        {
            "id": "I_kwDOAiOjoc54eAkz",
            "number": 38544,
            "title": "Remove preview LangVer",
            "state": "OPEN",
            "author": {
                "login": "{{authorLogin}}",
                "name": "{{authorName}}"
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
            "bodyHTML": "<p dir=\"auto\">Now that C# 12 has shipped, remove the <code class=\"notranslate\">&lt;LangVersion&gt;preview&lt;/Langversion&gt;</code> from all C# sample files.</p>",
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
            var variables = new QuestIssueOrPullRequestVariables
            {
                Organization = "dotnet",
                Repository = "docs",
                issueNumber = 38544
            };

            JsonElement element = JsonDocument.Parse(ValidOpenIssueResult).RootElement;
            var issue = QuestIssue.FromJsonElement(element, variables);
            Assert.NotNull(issue);
            Assert.True(issue.IsOpen);
            Assert.Equal($"{authorLogin} - {authorName}", issue.FormattedAuthorLoginName);
            Assert.Equal(bodyHTMLResult, issue.BodyHtml);
            Assert.Single(issue.Assignees);
            Assert.Equal(authorLogin, issue.Assignees.First().Login);
            Assert.Equal(4, issue.Labels.Length);
            for (int i = 0; i < issue.Labels.Length; i++)
            {
                Assert.Equal(labelNames[i], issue.Labels[i].Name);
                Assert.Equal(labelIds[i], issue.Labels[i].Id);
            }
            Assert.Empty(issue.Comments);

            // This test uses "contains" instead of "equal" because it contains newlines, and must run on both Windwos and Linux.
            Assert.Contains("<a href = \"https://github.com/dotnet/docs/issues/38544\">", issue.LinkText);
            Assert.Contains("dotnet/docs#38544", issue.LinkText);
            Assert.Contains("</a>", issue.LinkText);
            // Skip date time. For a single issue, we don't retrieve it.
            // Assert.Equal(DateTime.Now, issue.UpdatedAt);
            Assert.Single(issue.ProjectStoryPoints);
            StoryPointSize points = issue.ProjectStoryPoints.First();
            Assert.Equal(2023, points.CalendarYear);
            Assert.Equal("Dec", points.Month);
            Assert.Equal("🦔 Tiny (4h)", points.Size);
            Assert.Null(issue.ClosingPRUrl);
        }

        [Fact]
        public void ClosedQuestIssueFromJsonNode()
        {
            var variables = new QuestIssueOrPullRequestVariables
            {
                Organization = "dotnet",
                Repository = "docs",
                issueNumber = 38544
            };

            JsonElement element = JsonDocument.Parse(ValidClosedIssueResult).RootElement;
            var issue = QuestPullRequest.FromJsonElement(element, variables);
            Assert.NotNull(issue);
            Assert.False(issue.IsOpen);
            Assert.Equal($"{authorLogin} - {authorName}", issue.FormattedAuthorLoginName);
            Assert.Equal(closeBodyHTMLResult, issue.BodyHtml);
            Assert.Single(issue.Assignees);
            Assert.Equal(authorLogin, issue.Assignees.First().Login);
            Assert.Equal(4, issue.Labels.Length);
            for (int i = 0; i < issue.Labels.Length; i++)
            {
                Assert.Equal(closedLabelNames[i], issue.Labels[i].Name);
                Assert.Equal(closedLabelIds[i], issue.Labels[i].Id);
            }
            Assert.Empty(issue.Comments);
            // This test uses "contains" instead of "equal" because it contains newlines, and must run on both Windwos and Linux.
            Assert.Contains("<a href = \"https://github.com/dotnet/docs/issues/38544\">", issue.LinkText);
            Assert.Contains("dotnet/docs#38544", issue.LinkText);
            Assert.Contains("</a>", issue.LinkText);
            // Skip date time. For a single issue, we don't retrieve it.
            // Assert.Equal(DateTime.Now, issue.UpdatedAt);
            Assert.Single(issue.ProjectStoryPoints);
            StoryPointSize points = issue.ProjectStoryPoints.First();
            Assert.Equal(2023, points.CalendarYear);
            Assert.Equal("Dec", points.Month);
            Assert.Equal("🦔 Tiny (4h)", points.Size);
            Assert.Equal("https://github.com/dotnet/docs/pull/38584", issue.ClosingPRUrl);
        }

        [Fact]
        public void ParseIssueMissingOptionalFields()
        {
            var variables = new QuestIssueOrPullRequestVariables
            {
                Organization = "dotnet",
                Repository = "docs",
                issueNumber = 38544
            };

            JsonElement element = JsonDocument.Parse(IssueMissingFields).RootElement;
            var issue = QuestIssue.FromJsonElement(element, variables);
            Assert.NotNull(issue);
            Assert.True(issue.IsOpen);
            Assert.Equal($"{authorLogin} - {authorName}", issue.FormattedAuthorLoginName);
            Assert.Equal("<p dir=\"auto\">Just a short description</p>", issue.BodyHtml);
            Assert.Empty(issue.Assignees);
            Assert.Empty(issue.Labels);
            Assert.Empty(issue.Comments);
            // This test uses "contains" instead of "equal" because it contains newlines, and must run on both Windwos and Linux.
            Assert.Contains("<a href = \"https://github.com/dotnet/docs/issues/38544\">", issue.LinkText);
            Assert.Contains("dotnet/docs#38544", issue.LinkText);
            Assert.Contains("</a>", issue.LinkText);
            // Skip date time. For a single issue, we don't retrieve it.
            // Assert.Equal(DateTime.Now, issue.UpdatedAt);
            Assert.Empty(issue.ProjectStoryPoints);
            Assert.Null(issue.ClosingPRUrl);
        }

        [Fact]
        public void ParseIssueWithComments()
        {
            (string author, string bodyHTML)[] comments = [
                ("KalleOlaviNiemitalo", """<p dir="auto">Could perhaps say "has an <em>effective return type</em> (15.6.11) other than <code class="notranslate">void</code>", but I did not check whether this would result in a circular definition.</p>"""),
                ("jskeet", """<p dir="auto">Again, punting - and this one has probably been broken since C# 5. (I suspect the eventual fix will be a matter of just a few words, but choosing those words will take time...)</p>"""),
                ("KalleOlaviNiemitalo", "This was a big comment. And, tests have already verified that the JSON parser handles HTML.")
            ];

            var variables = new QuestIssueOrPullRequestVariables
            {
                Organization = "dotnet",
                Repository = "csharpstandard",
                issueNumber = 857
            };

            JsonElement element = JsonDocument.Parse(IssueWithComments).RootElement;
            var issue = QuestPullRequest.FromJsonElement(element, variables);
            Assert.NotNull(issue);
            Assert.True(issue.IsOpen);
            Assert.Equal("KalleOlaviNiemitalo - Kalle Olavi Niemitalo", issue.FormattedAuthorLoginName);
            Assert.Equal("<p dir=\"auto\">Just a short description</p>", issue.BodyHtml);
            Assert.Single(issue.Assignees);
            Assert.Single(issue.Labels);
            Assert.Equal(3, issue.Comments.Count());
            for (int i = 0; i < issue.Comments.Count(); i++)
            {
                Assert.Equal(comments[i], issue.Comments[i]);
                //Assert.Equal(comments[i].bodyHTML, issue.Comments[i].bodyHtml);
            }
            // This test uses "contains" instead of "equal" because it contains newlines, and must run on both Windwos and Linux.
            Assert.Contains("<a href = \"https://github.com/dotnet/csharpstandard/issues/857\">", issue.LinkText);
            Assert.Contains("dotnet/csharpstandard#857", issue.LinkText);
            Assert.Contains("</a>", issue.LinkText);
            // Skip date time. For a single issue, we don't retrieve it.
            // Assert.Equal(DateTime.Now, issue.UpdatedAt);
            StoryPointSize points = issue.ProjectStoryPoints.First();
            Assert.Equal(2023, points.CalendarYear);
            Assert.Equal("Dec", points.Month);
            Assert.Equal("🦔 Tiny (4h)", points.Size);
            Assert.Null(issue.ClosingPRUrl);
        }

        // This test is interesting in that I"ve never seen an issue without a timeline
        // node in the GraphQL Exploreer. However, Sequester failed overnight because 
        // the response packet was missing the timeline node. This test is to ensure
        // that we can handle that case.
        [Fact]
        public void OpenIssueMissingTimeLine()
        {
            var variables = new QuestIssueOrPullRequestVariables
            {
                Organization = "dotnet",
                Repository = "docs",
                issueNumber = 38544
            };

            JsonElement element = JsonDocument.Parse(MissingTimeLine).RootElement;
            var issue = QuestIssue.FromJsonElement(element, variables);
            Assert.NotNull(issue);
            Assert.True(issue.IsOpen);
            Assert.Equal($"{authorLogin} - {authorName}", issue.FormattedAuthorLoginName);
            Assert.Equal(bodyHTMLResult, issue.BodyHtml);
            Assert.Single(issue.Assignees);
            Assert.Equal(authorLogin, issue.Assignees.First().Login);
            Assert.Equal(4, issue.Labels.Length);
            for (int i = 0; i < issue.Labels.Length; i++)
            {
                Assert.Equal(labelNames[i], issue.Labels[i].Name);
                Assert.Equal(labelIds[i], issue.Labels[i].Id);
            }
            Assert.Empty(issue.Comments);

            // This test uses "contains" instead of "equal" because it contains newlines, and must run on both Windwos and Linux.
            Assert.Contains("<a href = \"https://github.com/dotnet/docs/issues/38544\">", issue.LinkText);
            Assert.Contains("dotnet/docs#38544", issue.LinkText);
            Assert.Contains("</a>", issue.LinkText);
            // Skip date time. For a single issue, we don't retrieve it.
            // Assert.Equal(DateTime.Now, issue.UpdatedAt);
            Assert.Single(issue.ProjectStoryPoints);
            StoryPointSize points = issue.ProjectStoryPoints.First();
            Assert.Equal(2023, points.CalendarYear);
            Assert.Equal("Dec", points.Month);
            Assert.Equal("🦔 Tiny (4h)", points.Size);
            Assert.Null(issue.ClosingPRUrl);
        }
    }
}
