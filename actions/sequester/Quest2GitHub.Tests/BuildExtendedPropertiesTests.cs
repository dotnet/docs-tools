using System.Text.Json;
using DotNet.DocsTools.GitHubObjects;
using Quest2GitHub.Models;

namespace Quest2GitHub.Tests;

public class BuildExtendedPropertiesTests
{
    private static QuestIteration[] _allIterations =
    [
        new() { Id =  1, Identifier = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Future", Path = """Content\Future""" },
        new() { Id =  2, Identifier = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "04 Apr", Path = """Content\Dilithium\FY24Q4\04 Apr""" },
        new() { Id =  3, Identifier = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "05 May", Path = """Content\Dilithium\FY24Q4\05 May""" },
        new() { Id =  4, Identifier = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "06 Jun", Path = """Content\Dilithium\FY24Q4\06 Jun""" },
        new() { Id =  5, Identifier = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "07 Jul", Path = """Content\Dilithium\FY25Q1\07 Jul""" },
        new() { Id =  6, Identifier = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "08 Aug", Path = """Content\Dilithium\FY25Q1\08 Aug""" },
        new() { Id =  7, Identifier = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "09 Sep", Path = """Content\Dilithium\FY25Q1\09 Sep""" },
        new() { Id =  8, Identifier = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "10 Oct", Path = """Content\Selenium\FY25Q2\10 Oct""" },
        new() { Id =  9, Identifier = Guid.Parse("99999999-9999-9999-9999-999999999999"), Name = "11 Nov", Path = """Content\Selenium\FY25Q2\11 Nov""" },
        new() { Id = 10, Identifier = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "12 Dev", Path = """Content\Selenium\FY25Q2\12 Dec""" },
        new() { Id = 11, Identifier = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "01 Jan", Path = """Content\Selenium\FY25Q3\01 Jan""" },
        new() { Id = 12, Identifier = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Name = "02 Feb", Path = """Content\Selenium\FY25Q3\02 Feb""" },
        new() { Id = 13, Identifier = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Name = "03 Mar", Path = """Content\Selenium\FY25Q3\03 Mar""" }
    ];
    private static LabelToTagMap[] _tagMap =
    [
        new () { Label = ":checkered_flag: Release: .NET 9", Tag = "new-feature" },
        new () { Label = labelForTag, Tag = "content-curation" },
    ];

    private static ParentForLabel[] _parentMap =
    [
        new () { Label = labelWithParent, Semester = "Selenium", ParentNodeId = 1 },
        new () { Label = null, Semester = "Selenium", ParentNodeId = 2 },
        new () { Label = labelWithParent, Semester = "Dilithium", ParentNodeId = 11 },
        new () { Label = null, Semester = "Dilithium", ParentNodeId = 22 }
    ];
    private const int _defaultParentId = 33;

    private const string labelForTag = "okr-curation";
    private const string labelWithoutTag = "enhancement";
    private const string labelWithParent = "user-feedback";
    private const string labelWithoutParent = "bug";


    private const string PastProject = """
      {
        "fieldValues": {
          "nodes": [
            {
                "field": {
                "name": "Status"
                },
                "name": "Slipped"
            },
            {
                "field": {
                "name": "Priority"
                },
                "name": "🏔 Pri1"
            },
            {
                "field": {
                "name": "Size"
                },
                "name": "🐂 Medium (3-5d)"
            }
            ]
        },
        "project": {
            "title": "dotnet/docs July 2024 Sprint"
        }
      }
      """;

    private const string AnotherPastProject = """
      {
        "fieldValues": {
          "nodes": [
            {
                "field": {
                "name": "Status"
                },
                "name": "Slipped"
            },
            {
                "field": {
                "name": "Priority"
                },
                "name": "🏔 Pri1"
            },
            {
                "field": {
                "name": "Size"
                },
                "name": "🐂 Medium (3-5d)"
            }
            ]
        },
        "project": {
            "title": "dotnet/docs October 2024 Sprint"
        }
      }
      """;
    private const string FutureProject = """
      {
        "fieldValues": {
          "nodes": [
            {
                "field": {
                "name": "Status"
                },
                "name": "Ready"
            },
            {
                "field": {
                "name": "Priority"
                },
                "name": "🏔 Pri1"
            },
            {
                "field": {
                "name": "Size"
                },
                "name": "🐂 Medium (3-5d)"
            }
            ]
        },
        "project": {
            "title": "dotnet/docs March 2025 Sprint"
        }
      }
      """;

    private const string SingleIssueFutureProject = $$"""
    {
      "id": "I_kwDOFn2dfM6YkX0J",
      "number": 1111,
      "title": "This is an issue",
      "state": "OPEN",
      "author": {
        "login": "BillWagner",
        "name": "Bill Wagner"
      },
      "timelineItems": {
        "nodes": []
      },
      "projectItems": {
        "nodes": [
          {{FutureProject}}
        ]
      },
      "bodyHTML": "<p dir=\"auto\">This is a bad, bad, thing.</p>",
      "body": "This is a bad, bad, thing.",
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
            "name": "{{labelWithoutParent}}",
            "id": "MDU6TGFiZWwzMDkwMTEzMzI1"
          },
          {
            "name": ":pushpin: seQUESTered",
            "id": "LA_kwDOFn2dfM8AAAABK0cMjA"
          }
        ]
      },
      "comments": {
        "nodes": []
      }
    }        
    """;

    private const string SingleIssueFutureProjectWithTags = $$"""
    {
      "id": "I_kwDOFn2dfM6YkX0J",
      "number": 1111,
      "title": "This is an issue",
      "state": "OPEN",
      "author": {
        "login": "BillWagner",
        "name": "Bill Wagner"
      },
      "timelineItems": {
        "nodes": []
      },
      "projectItems": {
        "nodes": [
          {{FutureProject}}
        ]
      },
      "bodyHTML": "<p dir=\"auto\">This is a bad, bad, thing.</p>",
      "body": "This is a bad, bad, thing.",
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
            "name": "{{labelForTag}}",
            "id": "MDU6TGFiZWwzMDkwMTEzMzI1"
          },
          {
            "name": "{{labelWithoutTag}}",
            "id": "LA_kwDOFn2dfM8AAAABK0cMjA"
          }
        ]
      },
      "comments": {
        "nodes": []
      }
    }        
    """;

    private const string SingleIssueFutureProjectWithParentLabel = $$"""
    {
      "id": "I_kwDOFn2dfM6YkX0J",
      "number": 1111,
      "title": "This is an issue",
      "state": "OPEN",
      "author": {
        "login": "BillWagner",
        "name": "Bill Wagner"
      },
      "timelineItems": {
        "nodes": []
      },
      "projectItems": {
        "nodes": [
          {{FutureProject}}
        ]
      },
      "bodyHTML": "<p dir=\"auto\">This is a bad, bad, thing.</p>",
      "body": "This is a bad, bad, thing.",
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
            "name": "{{labelWithParent}}",
            "id": "MDU6TGFiZWwzMDkwMTEzMzI1"
          },
          {
            "name": "{{labelWithoutTag}}",
            "id": "LA_kwDOFn2dfM8AAAABK0cMjA"
          }
        ]
      },
      "comments": {
        "nodes": []
      }
    }        
    """;

    private const string MultipleFutureProjects = $$"""
    {
      "id": "I_kwDOFn2dfM6YkX0J",
      "number": 1111,
      "title": "This is an issue",
      "state": "OPEN",
      "author": {
        "login": "BillWagner",
        "name": "Bill Wagner"
      },
      "timelineItems": {
        "nodes": []
      },
      "projectItems": {
        "nodes": [
          {{PastProject}},
          {{FutureProject}}
        ]
      },
      "bodyHTML": "<p dir=\"auto\">This is a bad, bad, thing.</p>",
      "body": "This is a bad, bad, thing.",
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
            "name": "{{labelWithoutParent}}",
            "id": "MDU6TGFiZWwzMDkwMTEzMzI1"
          },
          {
            "name": ":pushpin: seQUESTered",
            "id": "LA_kwDOFn2dfM8AAAABK0cMjA"
          }
        ]
      },
      "comments": {
        "nodes": []
      }
    }        
    """;

    private const string SingleIssuePastProject = $$"""
    {
      "id": "I_kwDOFn2dfM6YkX0J",
      "number": 1111,
      "title": "This is an issue",
      "state": "OPEN",
      "author": {
        "login": "BillWagner",
        "name": "Bill Wagner"
      },
      "timelineItems": {
        "nodes": []
      },
      "projectItems": {
        "nodes": [
          {{PastProject}}
        ]
      },
      "bodyHTML": "<p dir=\"auto\">This is a bad, bad, thing.</p>",
      "body": "This is a bad, bad, thing.",
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
            "name": "{{labelWithParent}}",
            "id": "MDU6TGFiZWwzMDkwMTEzMzI1"
          },
          {
            "name": ":pushpin: seQUESTered",
            "id": "LA_kwDOFn2dfM8AAAABK0cMjA"
          }
        ]
      },
      "comments": {
        "nodes": []
      }
    }        
    """;

    private const string MultipleIssuePastProject = $$"""
    {
      "id": "I_kwDOFn2dfM6YkX0J",
      "number": 1111,
      "title": "This is an issue",
      "state": "OPEN",
      "author": {
        "login": "BillWagner",
        "name": "Bill Wagner"
      },
      "timelineItems": {
        "nodes": []
      },
      "projectItems": {
        "nodes": [
          {{AnotherPastProject}},
          {{PastProject}}
        ]
      },
      "bodyHTML": "<p dir=\"auto\">This is a bad, bad, thing.</p>",
      "body": "This is a bad, bad, thing.",
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
            "name": "{{labelForTag}}",
            "id": "MDU6TGFiZWwzMDkwMTEzMzI1"
          },
          {
            "name": ":pushpin: seQUESTered",
            "id": "LA_kwDOFn2dfM8AAAABK0cMjA"
          }
        ]
      },
      "comments": {
        "nodes": []
      }
    }        
    """;

    private const string NoProject = $$"""
    {
      "id": "I_kwDOFn2dfM6YkX0J",
      "number": 1111,
      "title": "This is an issue",
      "state": "OPEN",
      "author": {
        "login": "BillWagner",
        "name": "Bill Wagner"
      },
      "timelineItems": {
        "nodes": []
      },
      "projectItems": {
        "nodes": []
      },
      "bodyHTML": "<p dir=\"auto\">This is a bad, bad, thing.</p>",
      "body": "This is a bad, bad, thing.",
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
            "name": "{{labelWithParent}}",
            "id": "MDU6TGFiZWwzMDkwMTEzMzI1"
          },
          {
            "name": ":pushpin: seQUESTered",
            "id": "LA_kwDOFn2dfM8AAAABK0cMjA"
          }
        ]
      },
      "comments": {
        "nodes": []
      }
    }        
    """;


    [Fact]
    public static void BuildExtensionForFutureProject()
    {
        var extendedProperties = CreateIssueObject(SingleIssueFutureProject);

        // Check each property:
        Assert.Equal("🐂 Medium (3-5d)", extendedProperties.GitHubSize);
        Assert.Equal(5, extendedProperties.StoryPoints);
        Assert.Equal(2, extendedProperties.Priority);
        Assert.Equal("03 Mar", extendedProperties.LatestIteration?.Name);
        Assert.False(extendedProperties.IsPastIteration);
        Assert.Equal("Mar", extendedProperties.Month);
        Assert.Equal(2025, extendedProperties.CalendarYear);
        Assert.Empty(extendedProperties.Tags);
        Assert.Equal(2, extendedProperties.ParentNodeId);
    }

    [Fact]
    public static void BuildExtensionForFutureProjectWithTag()
    {
        var extendedProperties = CreateIssueObject(SingleIssueFutureProjectWithTags);

        // Check each property:
        Assert.Equal("🐂 Medium (3-5d)", extendedProperties.GitHubSize);
        Assert.Equal(5, extendedProperties.StoryPoints);
        Assert.Equal(2, extendedProperties.Priority);
        Assert.Equal("03 Mar", extendedProperties.LatestIteration?.Name);
        Assert.False(extendedProperties.IsPastIteration);
        Assert.Equal("Mar", extendedProperties.Month);
        Assert.Equal(2025, extendedProperties.CalendarYear);
        Assert.Equal(["content-curation"], extendedProperties.Tags);
        Assert.Equal(2, extendedProperties.ParentNodeId);
    }

    [Fact]
    public static void BuildExtensionForFutureProjectWithParent()
    {
        var extendedProperties = CreateIssueObject(SingleIssueFutureProjectWithParentLabel);

        // Check each property:
        Assert.Equal("🐂 Medium (3-5d)", extendedProperties.GitHubSize);
        Assert.Equal(5, extendedProperties.StoryPoints);
        Assert.Equal(2, extendedProperties.Priority);
        Assert.Equal("03 Mar", extendedProperties.LatestIteration?.Name);
        Assert.False(extendedProperties.IsPastIteration);
        Assert.Equal("Mar", extendedProperties.Month);
        Assert.Equal(2025, extendedProperties.CalendarYear);
        Assert.Empty(extendedProperties.Tags);
        Assert.Equal(1, extendedProperties.ParentNodeId);
    }

    [Fact]
    public static void BuildExtensionForMultipleFutureProject()
    {
        var extendedProperties = CreateIssueObject(MultipleFutureProjects);

        // Check each property:
        Assert.Equal("🐂 Medium (3-5d)", extendedProperties.GitHubSize);
        Assert.Equal(5, extendedProperties.StoryPoints);
        Assert.Equal(2, extendedProperties.Priority);
        Assert.Equal("03 Mar", extendedProperties.LatestIteration?.Name);
        Assert.False(extendedProperties.IsPastIteration);
        Assert.Equal("Mar", extendedProperties.Month);
        Assert.Equal(2025, extendedProperties.CalendarYear);
        Assert.Empty(extendedProperties.Tags);
        Assert.Equal(2, extendedProperties.ParentNodeId);
    }

    [Fact]
    public static void BuildExtensionForSinglePastProject()
    {
        var extendedProperties = CreateIssueObject(SingleIssuePastProject);

        // Check each property:
        Assert.Equal("🐂 Medium (3-5d)", extendedProperties.GitHubSize);
        Assert.Equal(5, extendedProperties.StoryPoints);
        Assert.Equal(2, extendedProperties.Priority);
        Assert.Equal("07 Jul", extendedProperties.LatestIteration?.Name);
        Assert.True(extendedProperties.IsPastIteration);
        Assert.Equal("Jul", extendedProperties.Month);
        Assert.Equal(2024, extendedProperties.CalendarYear);
        Assert.Empty(extendedProperties.Tags);
        Assert.Equal(0, extendedProperties.ParentNodeId);
    }

    [Fact]
    public static void BuildExtensionForMultiplePastProject()
    {
        var extendedProperties = CreateIssueObject(MultipleIssuePastProject);

        // Check each property:
        Assert.Equal("🐂 Medium (3-5d)", extendedProperties.GitHubSize);
        Assert.Equal(5, extendedProperties.StoryPoints);
        Assert.Equal(2, extendedProperties.Priority);
        Assert.Equal("10 Oct", extendedProperties.LatestIteration?.Name);
        Assert.True(extendedProperties.IsPastIteration);
        Assert.Equal("Oct", extendedProperties.Month);
        Assert.Equal(2024, extendedProperties.CalendarYear);
        Assert.Equal(["content-curation"], extendedProperties.Tags);
        Assert.Equal(0, extendedProperties.ParentNodeId);
    }

    [Fact]
    public static void BuildExtensionForNoProject()
    {
        var extendedProperties = CreateIssueObject(NoProject);

        // Check each property:
        Assert.Equal("Unknown", extendedProperties.GitHubSize);
        Assert.Equal(null, extendedProperties.StoryPoints);
        Assert.Equal(null, extendedProperties.Priority);
        Assert.Equal(null, extendedProperties.LatestIteration?.Name);
        Assert.False(extendedProperties.IsPastIteration);
        Assert.Equal("Unknown", extendedProperties.Month);
        Assert.Equal(0, extendedProperties.CalendarYear);
        Assert.Empty(extendedProperties.Tags);
        Assert.Equal(0, extendedProperties.ParentNodeId);
    }


    private static ExtendedIssueProperties CreateIssueObject(string jsonDocument)
    {
        var variables = new QuestIssueOrPullRequestVariables
        {
            Organization = "dotnet",
            Repository = "docs",
            issueNumber = 1111
        };
        JsonElement element = JsonDocument.Parse(jsonDocument).RootElement;
        return QuestIssue.FromJsonElement(element, variables).ExtendedProperties(_allIterations, _tagMap, _parentMap);
    }

}
