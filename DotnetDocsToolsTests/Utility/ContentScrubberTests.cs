using DotNet.DocsTools.Utility;
using Xunit;

namespace DotnetDocsTools.Tests.Utility;

public class ContentScrubberTests
{
    private const string _contentWithImageLink = """
        <p>Imported from: <a href = "https://github.com/dotnet/docs/issues/39503">
            dotnet/docs#39503
        </a></p><p>Author: mairaw - Maira Wenzel</p>
        <h3 dir="auto">Type of issue</h3>
        <p dir="auto">Missing information</p>
        <h3 dir="auto">Description</h3>
        <p dir="auto">I noticed the article doesn't mention the new blazor template (I wanted to link to the article from <a class="issue-link js-issue-link" data-error-text="Failed to load title" data-id="2133599123" data-permission-text="Title is private" data-url="https://github.com/dotnet/website-feedback/issues/32" data-hovercard-type="issue" data-hovercard-url="/dotnet/website-feedback/issues/32/hovercard" href="https://github.com/dotnet/website-feedback/issues/32">dotnet/website-feedback#32</a>)</p>
        <p dir="auto">I think there are other templates that might be missing like aspire, etc.</p>
        <h3 dir="auto">Page URL</h3>
        <p dir="auto"><a href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new" rel="nofollow">https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new</a></p>
        <h3 dir="auto">Content source URL</h3>
        <p dir="auto"><a href="https://github.com/dotnet/docs/blob/main/docs/core/tools/dotnet-new.md">https://github.com/dotnet/docs/blob/main/docs/core/tools/dotnet-new.md</a></p>
        <h3 dir="auto">Document Version Independent Id</h3>
        <p dir="auto">d80dfb96-bc86-a960-abec-255dd907d04d</p>
        <h3 dir="auto">Article author</h3>
        <p dir="auto"><a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/tdykstra/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/tdykstra">@tdykstra</a></p>
        <h3 dir="auto">Metadata</h3>
        <ul dir="auto">
        <li>ID: 0b952f8a-18e1-4843-0741-6ca877fb303a</li>
        <li>Service: <strong>dotnet-fundamentals</strong></li>
        </ul>
        <p><b>Labels:</b></p>
        <ul>
        <li>#doc-enhancement</li>
        <li>#Pri1</li>
        <li>#dotnet-fundamentals/svc</li>
        <li>#okr-discovery</li>
        <li>#in-pr</li>
        </ul>
        <p><b>Comments:</b></p>
        <dl>
        <dt>mairaw</dt>
        <dd><p dir="auto">/cc <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/danroth27/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/danroth27">@danroth27</a></p></dd>
        <dt>tdykstra</dt>
        <dd><p dir="auto">The built-in templates are in <a href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates" rel="nofollow">https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates</a>, and the blazor template is there.</p>
        <p dir="auto">I tried <code class="notranslate">dotnet new aspire -h</code> and aspire doesn't appear to be a built-in template.  cc <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/IEvangelist/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/IEvangelist">@IEvangelist</a></p></dd>
        <dt>IEvangelist</dt>
        <dd><blockquote>
        <p dir="auto">The built-in templates are in <a href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates" rel="nofollow">https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates</a>, and the blazor template is there.</p>
        <p dir="auto">I tried <code class="notranslate">dotnet new aspire -h</code> and aspire doesn't appear to be a built-in template. cc <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/IEvangelist/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/IEvangelist">@IEvangelist</a></p>
        </blockquote>
        <p dir="auto">What version of .NET do you have installed, and did you update to the latest VS 2022 preview version? I see it:<br>
        <a target="_blank" rel="noopener noreferrer" href="https://private-user-images.githubusercontent.com/7679720/304875838-d97e0bae-d1db-4f8f-9095-d587d499a8af.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDg1MjkyNDksIm5iZiI6MTcwODUyODk0OSwicGF0aCI6Ii83Njc5NzIwLzMwNDg3NTgzOC1kOTdlMGJhZS1kMWRiLTRmOGYtOTA5NS1kNTg3ZDQ5OWE4YWYucG5nP1gtQW16LUFsZ29yaXRobT1BV1M0LUhNQUMtU0hBMjU2JlgtQW16LUNyZWRlbnRpYWw9QUtJQVZDT0RZTFNBNTNQUUs0WkElMkYyMDI0MDIyMSUyRnVzLWVhc3QtMSUyRnMzJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNDAyMjFUMTUyMjI5WiZYLUFtei1FeHBpcmVzPTMwMCZYLUFtei1TaWduYXR1cmU9ZDU2MTVkM2UwZmUxNWFiYTc4Mzg3OWQ0YWUzZmJmYmViMWQyMTJlYmU5NWFjMmVhZjMxOGIwMDY4MTVhMWNkMyZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmYWN0b3JfaWQ9MCZrZXlfaWQ9MCZyZXBvX2lkPTAifQ.q0ah9STw2IrOSQb0nicPmsrTXl98D0nnMgcvR4CY2uA"><img src="https://private-user-images.githubusercontent.com/7679720/304875838-d97e0bae-d1db-4f8f-9095-d587d499a8af.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDg1MjkyNDksIm5iZiI6MTcwODUyODk0OSwicGF0aCI6Ii83Njc5NzIwLzMwNDg3NTgzOC1kOTdlMGJhZS1kMWRiLTRmOGYtOTA5NS1kNTg3ZDQ5OWE4YWYucG5nP1gtQW16LUFsZ29yaXRobT1BV1M0LUhNQUMtU0hBMjU2JlgtQW16LUNyZWRlbnRpYWw9QUtJQVZDT0RZTFNBNTNQUUs0WkElMkYyMDI0MDIyMSUyRnVzLWVhc3QtMSUyRnMzJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNDAyMjFUMTUyMjI5WiZYLUFtei1FeHBpcmVzPTMwMCZYLUFtei1TaWduYXR1cmU9ZDU2MTVkM2UwZmUxNWFiYTc4Mzg3OWQ0YWUzZmJmYmViMWQyMTJlYmU5NWFjMmVhZjMxOGIwMDY4MTVhMWNkMyZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmYWN0b3JfaWQ9MCZrZXlfaWQ9MCZyZXBvX2lkPTAifQ.q0ah9STw2IrOSQb0nicPmsrTXl98D0nnMgcvR4CY2uA" alt="image" style="max-width: 100%;"></a></p></dd>
        <dt>mairaw</dt>
        <dd><blockquote>
        <p dir="auto">The built-in templates are in <a href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates" rel="nofollow">https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates</a>, and the blazor template is there.</p>
        <p dir="auto">I tried <code class="notranslate">dotnet new aspire -h</code> and aspire doesn't appear to be a built-in template. cc <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/IEvangelist/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/IEvangelist">@IEvangelist</a></p>
        </blockquote>
        <p dir="auto">why do we have two different lists in two different pages?</p></dd>
        <dt>IEvangelist</dt>
        <dd><p dir="auto">Hi <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/mairaw/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/mairaw">@mairaw</a> and <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/tdykstra/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/tdykstra">@tdykstra</a>,</p>
        <blockquote>
        <p dir="auto">why do we have two different lists in two different pages?</p>
        </blockquote>
        <p dir="auto">I see value in having these lists in both places, but I'd encourage the use of an include to achieve this, as to avoid updating/maintaining multiple separate docs. The .NET Aspire workload is part of Visual Studio 2022 17.10.0 Preview 1.0, that's where I believe the templates are coming from. I wouldn't expect to see it listed in any of these articles yet, as it's not yet GA, nor part of any stable release, all still in preview.</p></dd>
        <dt>tdykstra</dt>
        <dd><p dir="auto">I didn't realize we duplicated the template list in the <code class="notranslate">dotnet new &lt;TEMPLATE&gt;</code> doc.  I agree, an include file makes sense here. I'll implement one to address this issue.</p></dd>
        <dt>mairaw</dt>
        <dd><p dir="auto">Makes sense! Yes, my machine might not be the best place to validate default templates 🙂</p>
        <p dir="auto">Perfect! I was thinking of the same strategy to avoid having to update the content twice. Thanks!</p></dd>
        </dl>
        """;

    private const string _contentWithImageLinkRemoved = """
        <p>Imported from: <a href = "https://github.com/dotnet/docs/issues/39503">
            dotnet/docs#39503
        </a></p><p>Author: mairaw - Maira Wenzel</p>
        <h3 dir="auto">Type of issue</h3>
        <p dir="auto">Missing information</p>
        <h3 dir="auto">Description</h3>
        <p dir="auto">I noticed the article doesn't mention the new blazor template (I wanted to link to the article from <a class="issue-link js-issue-link" data-error-text="Failed to load title" data-id="2133599123" data-permission-text="Title is private" data-url="https://github.com/dotnet/website-feedback/issues/32" data-hovercard-type="issue" data-hovercard-url="/dotnet/website-feedback/issues/32/hovercard" href="https://github.com/dotnet/website-feedback/issues/32">dotnet/website-feedback#32</a>)</p>
        <p dir="auto">I think there are other templates that might be missing like aspire, etc.</p>
        <h3 dir="auto">Page URL</h3>
        <p dir="auto"><a href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new" rel="nofollow">https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new</a></p>
        <h3 dir="auto">Content source URL</h3>
        <p dir="auto"><a href="https://github.com/dotnet/docs/blob/main/docs/core/tools/dotnet-new.md">https://github.com/dotnet/docs/blob/main/docs/core/tools/dotnet-new.md</a></p>
        <h3 dir="auto">Document Version Independent Id</h3>
        <p dir="auto">d80dfb96-bc86-a960-abec-255dd907d04d</p>
        <h3 dir="auto">Article author</h3>
        <p dir="auto"><a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/tdykstra/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/tdykstra">@tdykstra</a></p>
        <h3 dir="auto">Metadata</h3>
        <ul dir="auto">
        <li>ID: 0b952f8a-18e1-4843-0741-6ca877fb303a</li>
        <li>Service: <strong>dotnet-fundamentals</strong></li>
        </ul>
        <p><b>Labels:</b></p>
        <ul>
        <li>#doc-enhancement</li>
        <li>#Pri1</li>
        <li>#dotnet-fundamentals/svc</li>
        <li>#okr-discovery</li>
        <li>#in-pr</li>
        </ul>
        <p><b>Comments:</b></p>
        <dl>
        <dt>mairaw</dt>
        <dd><p dir="auto">/cc <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/danroth27/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/danroth27">@danroth27</a></p></dd>
        <dt>tdykstra</dt>
        <dd><p dir="auto">The built-in templates are in <a href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates" rel="nofollow">https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates</a>, and the blazor template is there.</p>
        <p dir="auto">I tried <code class="notranslate">dotnet new aspire -h</code> and aspire doesn't appear to be a built-in template.  cc <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/IEvangelist/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/IEvangelist">@IEvangelist</a></p></dd>
        <dt>IEvangelist</dt>
        <dd><blockquote>
        <p dir="auto">The built-in templates are in <a href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates" rel="nofollow">https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates</a>, and the blazor template is there.</p>
        <p dir="auto">I tried <code class="notranslate">dotnet new aspire -h</code> and aspire doesn't appear to be a built-in template. cc <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/IEvangelist/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/IEvangelist">@IEvangelist</a></p>
        </blockquote>
        <p dir="auto">What version of .NET do you have installed, and did you update to the latest VS 2022 preview version? I see it:<br>
        <i>Image link removed to protect against security vulnerability.</i></p></dd>
        <dt>mairaw</dt>
        <dd><blockquote>
        <p dir="auto">The built-in templates are in <a href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates" rel="nofollow">https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates</a>, and the blazor template is there.</p>
        <p dir="auto">I tried <code class="notranslate">dotnet new aspire -h</code> and aspire doesn't appear to be a built-in template. cc <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/IEvangelist/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/IEvangelist">@IEvangelist</a></p>
        </blockquote>
        <p dir="auto">why do we have two different lists in two different pages?</p></dd>
        <dt>IEvangelist</dt>
        <dd><p dir="auto">Hi <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/mairaw/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/mairaw">@mairaw</a> and <a class="user-mention notranslate" data-hovercard-type="user" data-hovercard-url="/users/tdykstra/hovercard" data-octo-click="hovercard-link-click" data-octo-dimensions="link_type:self" href="https://github.com/tdykstra">@tdykstra</a>,</p>
        <blockquote>
        <p dir="auto">why do we have two different lists in two different pages?</p>
        </blockquote>
        <p dir="auto">I see value in having these lists in both places, but I'd encourage the use of an include to achieve this, as to avoid updating/maintaining multiple separate docs. The .NET Aspire workload is part of Visual Studio 2022 17.10.0 Preview 1.0, that's where I believe the templates are coming from. I wouldn't expect to see it listed in any of these articles yet, as it's not yet GA, nor part of any stable release, all still in preview.</p></dd>
        <dt>tdykstra</dt>
        <dd><p dir="auto">I didn't realize we duplicated the template list in the <code class="notranslate">dotnet new &lt;TEMPLATE&gt;</code> doc.  I agree, an include file makes sense here. I'll implement one to address this issue.</p></dd>
        <dt>mairaw</dt>
        <dd><p dir="auto">Makes sense! Yes, my machine might not be the best place to validate default templates 🙂</p>
        <p dir="auto">Perfect! I was thinking of the same strategy to avoid having to update the content twice. Thanks!</p></dd>
        </dl>
        """;

    [Fact]
    public void TestScrubContent()
    {
        var result = _contentWithImageLink.ScrubContent();

        Assert.Equal(_contentWithImageLinkRemoved, result);
    }

    [Theory]
    [InlineData
        ("""
        <p>Imported from: <a href = "https://github.com/dotnet/docs/issues/39503">
            dotnet/docs#39503
        </a></p>
        """,
        """
        <p>Imported from: <a href = "https://github.com/dotnet/docs/issues/39503">
            dotnet/docs#39503
        </a></p>
        """
        )]
    [InlineData
       ("""</p><p>Author: mairaw - Maira Wenzel</p>""",
        """</p><p>Author: mairaw - Maira Wenzel</p>""")]
    [InlineData
        ("""
        <a target="_blank" rel="noopener noreferrer" href="https://private-user-images.githubusercontent.com/7679720/304875838-d97e0bae-d1db-4f8f-9095-d587d499a8af.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDg1MjkyNDksIm5iZiI6MTcwODUyODk0OSwicGF0aCI6Ii83Njc5NzIwLzMwNDg3NTgzOC1kOTdlMGJhZS1kMWRiLTRmOGYtOTA5NS1kNTg3ZDQ5OWE4YWYucG5nP1gtQW16LUFsZ29yaXRobT1BV1M0LUhNQUMtU0hBMjU2JlgtQW16LUNyZWRlbnRpYWw9QUtJQVZDT0RZTFNBNTNQUUs0WkElMkYyMDI0MDIyMSUyRnVzLWVhc3QtMSUyRnMzJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNDAyMjFUMTUyMjI5WiZYLUFtei1FeHBpcmVzPTMwMCZYLUFtei1TaWduYXR1cmU9ZDU2MTVkM2UwZmUxNWFiYTc4Mzg3OWQ0YWUzZmJmYmViMWQyMTJlYmU5NWFjMmVhZjMxOGIwMDY4MTVhMWNkMyZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmYWN0b3JfaWQ9MCZrZXlfaWQ9MCZyZXBvX2lkPTAifQ.q0ah9STw2IrOSQb0nicPmsrTXl98D0nnMgcvR4CY2uA"><img src="https://private-user-images.githubusercontent.com/7679720/304875838-d97e0bae-d1db-4f8f-9095-d587d499a8af.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDg1MjkyNDksIm5iZiI6MTcwODUyODk0OSwicGF0aCI6Ii83Njc5NzIwLzMwNDg3NTgzOC1kOTdlMGJhZS1kMWRiLTRmOGYtOTA5NS1kNTg3ZDQ5OWE4YWYucG5nP1gtQW16LUFsZ29yaXRobT1BV1M0LUhNQUMtU0hBMjU2JlgtQW16LUNyZWRlbnRpYWw9QUtJQVZDT0RZTFNBNTNQUUs0WkElMkYyMDI0MDIyMSUyRnVzLWVhc3QtMSUyRnMzJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNDAyMjFUMTUyMjI5WiZYLUFtei1FeHBpcmVzPTMwMCZYLUFtei1TaWduYXR1cmU9ZDU2MTVkM2UwZmUxNWFiYTc4Mzg3OWQ0YWUzZmJmYmViMWQyMTJlYmU5NWFjMmVhZjMxOGIwMDY4MTVhMWNkMyZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmYWN0b3JfaWQ9MCZrZXlfaWQ9MCZyZXBvX2lkPTAifQ.q0ah9STw2IrOSQb0nicPmsrTXl98D0nnMgcvR4CY2uA" alt="image" style="max-width: 100%;"></a></p></dd>
        """,
        """<i>Image link removed to protect against security vulnerability.</i></p></dd>""")]
    [InlineData
        ("""
        <a target="_blank" rel="noopener noreferrer" href="https://private-user-images.githubusercontent.com/7679720/304875838-d97e0bae-d1db-4f8f-9095-d587d499a8af.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDg1MjkyNDksIm5iZiI6MTcwODUyODk0OSwicGF0aCI6Ii83Njc5NzIwLzMwNDg3NTgzOC1kOTdlMGJhZS1kMWRiLTRmOGYtOTA5NS1kNTg3ZDQ5OWE4YWYucG5nP1gtQW16LUFsZ29yaXRobT1BV1M0LUhNQUMtU0hBMjU2JlgtQW16LUNyZWRlbnRpYWw9QUtJQVZDT0RZTFNBNTNQUUs0WkElMkYyMDI0MDIyMSUyRnVzLWVhc3QtMSUyRnMzJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNDAyMjFUMTUyMjI5WiZYLUFtei1FeHBpcmVzPTMwMCZYLUFtei1TaWduYXR1cmU9ZDU2MTVkM2UwZmUxNWFiYTc4Mzg3OWQ0YWUzZmJmYmViMWQyMTJlYmU5NWFjMmVhZjMxOGIwMDY4MTVhMWNkMyZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmYWN0b3JfaWQ9MCZrZXlfaWQ9MCZyZXBvX2lkPTAifQ.q0ah9STw2IrOSQb0nicPmsrTXl98D0nnMgcvR4CY2uA"><img src="https://private-user-images.githubusercontent.com/7679720/304875838-d97e0bae-d1db-4f8f-9095-d587d499a8af.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDg1MjkyNDksIm5iZiI6MTcwODUyODk0OSwicGF0aCI6Ii83Njc5NzIwLzMwNDg3NTgzOC1kOTdlMGJhZS1kMWRiLTRmOGYtOTA5NS1kNTg3ZDQ5OWE4YWYucG5nP1gtQW16LUFsZ29yaXRobT1BV1M0LUhNQUMtU0hBMjU2JlgtQW16LUNyZWRlbnRpYWw9QUtJQVZDT0RZTFNBNTNQUUs0WkElMkYyMDI0MDIyMSUyRnVzLWVhc3QtMSUyRnMzJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNDAyMjFUMTUyMjI5WiZYLUFtei1FeHBpcmVzPTMwMCZYLUFtei1TaWduYXR1cmU9ZDU2MTVkM2UwZmUxNWFiYTc4Mzg3OWQ0YWUzZmJmYmViMWQyMTJlYmU5NWFjMmVhZjMxOGIwMDY4MTVhMWNkMyZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmYWN0b3JfaWQ9MCZrZXlfaWQ9MCZyZXBvX2lkPTAifQ.q0ah9STw2IrOSQb0nicPmsrTXl98D0nnMgcvR4CY2uA" alt="image" style="max-width: 100%;"></a></p></dd>
        <p>Imported from: <a href = "https://github.com/dotnet/docs/issues/39503">
            dotnet/docs#39503
        </a></p>
        <a target="_blank" rel="noopener noreferrer" href="https://private-user-images.githubusercontent.com/7679720/304875838-d97e0bae-d1db-4f8f-9095-d587d499a8af.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDg1MjkyNDksIm5iZiI6MTcwODUyODk0OSwicGF0aCI6Ii83Njc5NzIwLzMwNDg3NTgzOC1kOTdlMGJhZS1kMWRiLTRmOGYtOTA5NS1kNTg3ZDQ5OWE4YWYucG5nP1gtQW16LUFsZ29yaXRobT1BV1M0LUhNQUMtU0hBMjU2JlgtQW16LUNyZWRlbnRpYWw9QUtJQVZDT0RZTFNBNTNQUUs0WkElMkYyMDI0MDIyMSUyRnVzLWVhc3QtMSUyRnMzJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNDAyMjFUMTUyMjI5WiZYLUFtei1FeHBpcmVzPTMwMCZYLUFtei1TaWduYXR1cmU9ZDU2MTVkM2UwZmUxNWFiYTc4Mzg3OWQ0YWUzZmJmYmViMWQyMTJlYmU5NWFjMmVhZjMxOGIwMDY4MTVhMWNkMyZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmYWN0b3JfaWQ9MCZrZXlfaWQ9MCZyZXBvX2lkPTAifQ.q0ah9STw2IrOSQb0nicPmsrTXl98D0nnMgcvR4CY2uA"><img src="https://private-user-images.githubusercontent.com/7679720/304875838-d97e0bae-d1db-4f8f-9095-d587d499a8af.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MDg1MjkyNDksIm5iZiI6MTcwODUyODk0OSwicGF0aCI6Ii83Njc5NzIwLzMwNDg3NTgzOC1kOTdlMGJhZS1kMWRiLTRmOGYtOTA5NS1kNTg3ZDQ5OWE4YWYucG5nP1gtQW16LUFsZ29yaXRobT1BV1M0LUhNQUMtU0hBMjU2JlgtQW16LUNyZWRlbnRpYWw9QUtJQVZDT0RZTFNBNTNQUUs0WkElMkYyMDI0MDIyMSUyRnVzLWVhc3QtMSUyRnMzJTJGYXdzNF9yZXF1ZXN0JlgtQW16LURhdGU9MjAyNDAyMjFUMTUyMjI5WiZYLUFtei1FeHBpcmVzPTMwMCZYLUFtei1TaWduYXR1cmU9ZDU2MTVkM2UwZmUxNWFiYTc4Mzg3OWQ0YWUzZmJmYmViMWQyMTJlYmU5NWFjMmVhZjMxOGIwMDY4MTVhMWNkMyZYLUFtei1TaWduZWRIZWFkZXJzPWhvc3QmYWN0b3JfaWQ9MCZrZXlfaWQ9MCZyZXBvX2lkPTAifQ.q0ah9STw2IrOSQb0nicPmsrTXl98D0nnMgcvR4CY2uA" alt="image" style="max-width: 100%;"></a></p></dd>
        """,
        """
        <i>Image link removed to protect against security vulnerability.</i></p></dd>
        <p>Imported from: <a href = "https://github.com/dotnet/docs/issues/39503">
            dotnet/docs#39503
        </a></p>
        <i>Image link removed to protect against security vulnerability.</i></p></dd>
        """
        )]
    public void ScrubHTMLTags(string source, string expected)
    {
        var result = source.ScrubContent();
        Assert.Equal(expected, result);
    }
}
