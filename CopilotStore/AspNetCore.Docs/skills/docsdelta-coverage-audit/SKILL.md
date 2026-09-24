---
name: docsdelta-coverage-audit
description: >
  Determine whether a documentation set already covers a product feature, and locate the
  exact articles and line numbers where missing coverage belongs. Pins a commit SHA, sets up
  a sparse checkout, extracts the feature's API surface, searches the docset for every
  symbol, reads candidate articles in full, verifies behavioral claims against product
  source, and classifies each feature element as covered, needing an update, or undetermined.
  Use when auditing docs coverage for a release note, feature, or API — or any time you need
  defensible line-number citations rather than search snippets.
---

# docsdelta coverage audit

The method this skill encodes exists because the obvious approach fails. Asking "is this
feature documented?" and searching for its name produces confident wrong answers in both
directions: it misses coverage written in different words, and it reports coverage that is
actually just the release note mentioning itself.

The audit is evidence-first. Every conclusion traces to a file read in full at a known
commit.

## Parameters

| Parameter | Example | Purpose |
|---|---|---|
| `TARGET_REPO` | `dotnet/AspNetCore.Docs` | Docset being audited |
| `TARGET_REF` | `main` | Branch to pin a SHA from |
| `DOCSET_ROOT` | `aspnetcore/` | Publishable content root |
| `EXCLUDE_AS_TARGET` | `aspnetcore/release-notes/` | Excluded from search results and never proposed as an edit target |
| `WORKDIR` | *(see caching)* | Clone location — a cache dir, never the session's own repo |
| `CACHE_ROOT` | `~/.copilot/cache/docsdelta-coverage-audit/` | Where reusable clones live |
| `CACHE_MAX_AGE_HOURS` | `24` | Reuse a cached clone younger than this |
| `SAMPLES_REPOS` | `dotnet/AspNetCore.Docs.Samples` | Sibling repos that `~/../` references resolve into |
| `PRODUCT_REPO` | `dotnet/aspnetcore` | Where behavior is verified; pin a SHA for it too |

---

## Step 1: Get a checkout and pin its SHA

Line numbers are only meaningful against a known commit, so a SHA must be fixed **before
reading a single file**. How it's fixed depends on whether a usable cached clone exists.

### Check the cache first

Clones live at `{CACHE_ROOT}{repo-name}`, with a marker file at
`{CACHE_ROOT}{repo-name}.cache.json` **beside** the repo — never inside it, or the marker
makes the tree dirty and fails check 4 below.

A cached clone is usable only if **all** of these hold:

1. The marker file exists and parses.
2. `cloned_at_epoch` is present, and `now_epoch - cloned_at_epoch` is **between 0 and
   `CACHE_MAX_AGE_HOURS`**. Reject a negative age too — it means a corrupt or
   future-dated marker, not a fresh one.
3. `git rev-parse HEAD` succeeds and equals the marker's `sha`.
4. `git status --porcelain` is empty — nothing wrote to the tree.
5. The marker's `sparse_scope` is the full `DOCSET_ROOT` with **no file-type filter**, and
   its `samples_repos` are present as siblings. A narrower scope silently breaks the
   docset-wide search in Step 5 and leaves code references unresolvable.

Check 5 matters more than it looks: an area-scoped or markdown-only cache turns real gaps
into false ✅ rather than failing loudly.

Store the timestamp as **integer epoch seconds**, never an ISO-8601 string. PowerShell's
`ConvertFrom-Json` re-parses date-shaped strings into `DateTime` with `Kind=Unspecified`,
dropping the `Z`; a following `.ToUniversalTime()` then *adds* the local offset and dates the
marker into the future. The resulting negative age satisfies `< 24` forever, so the cache
never expires. Integers can't be reinterpreted.

```powershell
cd {CACHE_ROOT}{repo-name}
git rev-parse HEAD          # this is TARGET_COMMIT_SHA
git status --porcelain      # must print nothing

$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$age = ($now - $marker.cloned_at_epoch) / 3600     # must be >= 0 and < CACHE_MAX_AGE_HOURS
```

**When the cache is usable, adopt its SHA. Don't re-resolve `TARGET_REF`.** Re-resolving
would name a commit the cache doesn't contain, and checking it out would force a fetch —
defeating the point. The accepted trade is explicit: a run may be up to
`CACHE_MAX_AGE_HOURS` behind `main`, so a PR merged in that window won't be seen.

```powershell
cd {CACHE_ROOT}{repo-name}
git rev-parse HEAD          # this is TARGET_COMMIT_SHA
git status --porcelain      # must print nothing
```

Report the age alongside the SHA so the operator can judge it:

```
Auditing dotnet/AspNetCore.Docs at a1b2c3d (cached clone, 6h old).
```

### Otherwise clone fresh

Any check failing means clone from scratch. Delete a stale or dirty cache rather than
repairing it.

```powershell
gh api repos/{TARGET_REPO}/commits/{TARGET_REF} --jq '.sha'
```

Record it as `TARGET_COMMIT_SHA`, report it, and use it in every permalink. Never link a
branch — `blob/main/file.md#L141` points somewhere different next week.

## Step 2: Set up the checkout

Reading files one at a time over the API is slow and, worse, it encourages citing line
numbers from truncated responses. A local checkout makes full reads and repeated greps cheap,
which is what keeps the citations honest.

**Scope it to the whole content root, and do not filter by file type.** Articles are not
self-contained: they pull their code in by reference with `:::code source="…"` and
`[!code-csharp[](…)]`, so the `.cs`, `.csproj`, `.json`, `.razor`, `.cshtml`, and `.js` files
those directives point at are part of the evidence. On `dotnet/AspNetCore.Docs` there are
about 1,890 such in-repo references, 1,737 of them to `.cs` files. A markdown-only checkout
makes every one of them unresolvable, and an audit that can't open the sample can't tell
whether the sample already demonstrates the feature.

That cost is real — roughly 422 MB and 19,500 files versus 18 MB for markdown alone — but
it's paid once per `CACHE_MAX_AGE_HOURS`, and correctness isn't the thing to trade away here.

```powershell
$cache = "{CACHE_ROOT}{repo-name}"
git clone --filter=blob:none --no-checkout https://github.com/{TARGET_REPO}.git $cache
cd $cache
git sparse-checkout init --cone
git sparse-checkout set {DOCSET_ROOT}
git checkout {TARGET_COMMIT_SHA}
```

Then **verify HEAD matches the pinned SHA** before trusting anything:

```powershell
git rev-parse HEAD
```

### Sibling sample repositories

Some references escape the docset entirely. A leading `~/../` resolves to a *sibling repo*,
not to this one:

| Reference root | Repository | Needed for |
|---|---|---|
| `~/../AspNetCore.Docs.Samples/` | `dotnet/AspNetCore.Docs.Samples` | SignalR, gRPC, and other non-Blazor areas |
| `~/../blazor-samples/` | `dotnet/blazor-samples` | Blazor only |
| `~/../maui-samples/` | `dotnet/maui-samples` | Blazor Hybrid only |

Clone `SAMPLES_REPOS` as **siblings inside the cache root**, so `~/../` resolves by plain
path arithmetic:

```powershell
git clone --filter=blob:none https://github.com/dotnet/AspNetCore.Docs.Samples.git `
  "{CACHE_ROOT}AspNetCore.Docs.Samples"
```

If a `:::code source=` path can't be resolved on disk, the element is **🟣, not ✅** — an
unread sample is an unknown, and guessing what it contains is how a false ✅ gets written.

Write the marker beside the repo so the next run can reuse it:

```powershell
@{
  repo = "{TARGET_REPO}"; sha = "{TARGET_COMMIT_SHA}"
  cloned_at_epoch = [int][double]::Parse(((Get-Date).ToUniversalTime() - [datetime]'1970-01-01').TotalSeconds)
  docset_root = "{DOCSET_ROOT}"
  sparse_scope = "{DOCSET_ROOT}"        # full content root, no file-type filter
  samples_repos = @('dotnet/AspNetCore.Docs.Samples')
} | ConvertTo-Json | Set-Content "{CACHE_ROOT}{repo-name}.cache.json"
```

Notes:

* Cone mode takes directory prefixes. Don't narrow it with `--no-cone` extension globs —
  that's what made sample code unreadable.
* `--filter=blob:none` fetches blobs on demand, so the clone stays cheaper than the working
  tree suggests.
* The cache lives under `CACHE_ROOT`, never in the session's own working repo.
* **Never reuse a clone the audit didn't create.** A developer's working clone may sit on a
  feature branch or carry uncommitted edits, and checking out `TARGET_COMMIT_SHA` in it
  detaches their HEAD. The marker file distinguishes an audit cache from a working copy —
  no marker, no reuse.
* Do **not** use `--depth=1`; you can't check out an arbitrary SHA from a shallow clone.
* This is read-only. Never commit, push, create a branch, or write a file inside the cache —
  a dirty tree invalidates it for every future run.

### Pin the product repo too

Check out `PRODUCT_REPO` the same way — same cache rules, same marker, same age limit — and
record its SHA as `PRODUCT_COMMIT_SHA`. Reports link into product source to justify
behavioral claims, and a link to `blob/main/...` rots exactly like any other branch link. A
local checkout is also what makes Step 4's history search possible.

## Step 3: Extract the feature's API surface

Read the source describing the feature — release note, spec, or PR — **in full**. From it,
build an explicit inventory. This list is what you search for, and anything not on it won't
be found.

Capture every:

* Type, member, property, and option name
* Builder or extension method
* Event and callback name
* Overridable virtual method
* Endpoint path, route template, and HTTP method
* Configuration key and default value
* Behavioral claim — "the connection stays open", "the negotiate response reports the lifetime"

Also record what the source *doesn't* say. Gaps in the release note are often the most
important thing to document.

### Element types

Most elements are symbols. Not all of them are.

| Type | Example |
|---|---|
| New API | A type, member, option, event, or overridable method |
| New endpoint | A route template plus its method, codes, and registration condition |
| **Behavior change on an existing API** | Same signature, different runtime behavior |
| Default change | A value that changed without any signature moving |

**The behavior-change type is the one that gets missed**, because searching for new symbols
finds nothing and the feature looks absent — or worse, looks already covered because the
old signature is documented. Client cancellation of non-streaming hub invocations used
`InvokeAsync` overloads that had shipped years earlier; the entire feature was a behavior
change.

When an element is a behavior change, record **what the old behavior was** alongside the
new one. The contrast is the thing the documentation has to convey, and it's also what
decides whether the change is breaking.

## Step 4: Verify behavior against product source

**This is the step that prevents publishing wrong guidance.** Release notes summarize, and
summaries round off exactly the details that end up in a proposed edit.

Before asserting any of the following in a report, read `PRODUCT_REPO`:

| Claim | What to confirm |
|---|---|
| An endpoint exists | Its route template, and whether registration is conditional on an option |
| An HTTP method | What the handler does with other methods |
| A status code | The actual code and error payload |
| A parameter's location | Query string vs. body vs. header |
| A default value | The field initializer, not the documentation |

A .NET 11 SignalR audit found the release note said only "exposes a `/refresh` endpoint alongside
`/negotiate`". Product source showed the endpoint is registered *only* when an option is
enabled, takes the connection token from the **query string**, returns **405** for non-`POST`,
and **404** with a specific error code when refresh is disabled. None of that was in the
release note, and all of it belonged in the proposed edit.

If you can't verify a claim, it becomes a 🟣, not a confident sentence.

### Find the implementing pull request

The highest-value move available, and the one most likely to be skipped. Locate the commit
that introduced the behavior, then look at everything it touched.

```powershell
git log -L{start},{end}:{file}        # history of just the changed region
git show --stat {commit}              # the full change surface of that commit
```

One commit typically spans client, server, protocol specification, and internal descriptors.
Seeing them together reveals the parts of a feature the release note never mentions — in the
test run this surfaced that the server token is linked to `ConnectionAborted`, that
cancellation is cooperative and still sends a `Completion`, and that a descriptor change made
`CancellationToken` synthetic on *all* hub methods rather than only streaming ones. None of
that was in the note.

It also settles version applicability with evidence. A single commit adding both halves of a
feature is proof it's `CURRENT-ONLY`; a backport or servicing commit is proof it isn't.

Cite the PR in the report and record `PRODUCT_COMMIT_SHA` in every product-source link.

## Step 5: Search the docset

For every symbol from Step 3, search `DOCSET_ROOT`. Exclude `EXCLUDE_AS_TARGET` from results
— the release notes mentioning a feature is not coverage, and leaving them in makes every
search look like a hit.

Search for more than exact symbols:

* The symbol itself, and its `xref` UID form
* The concept in prose — a reader searching "token expired" won't type `MaximumAuthenticationExpiration`
* The feature's marketing name and its API name, which are often different

**A hit is a candidate, not a finding.** Search output is for locating files. Nothing else.

## Step 6: Read candidate articles in full

Open every candidate at `TARGET_COMMIT_SHA` and read it — front matter to end.

This is non-negotiable and it is the step most likely to be skipped under time pressure.
Skipping it produces the two failure modes this skill exists to prevent:

1. **Citing a line number from a search snippet.** Snippet line numbers drift from real ones, and a wrong line number in an issue is worse than no line number — it sends an author to the wrong paragraph and quietly destroys trust in the whole report.
2. **Missing coverage that uses different words.** The docset may document the feature thoroughly under terminology the release note never uses.

While reading, record:

* `uid` and `monikerRange` from front matter — read them, never infer them
* Every `:::moniker range=` / `:::moniker-end` boundary with its line numbers
* Every `# [Tab](#tab/...)` group boundary
* Section headings with line numbers
* Exact insertion points, with the anchor text of the preceding paragraph
* Every `:::code source=` and `[!code-*[](…)]` reference in the sections you're judging

### Then follow the code references

Work outward in layers, and only as far as the question requires:

1. **`toc.yml`** — establishes what exists and where a reader would look for it.
2. **The articles** — prose, headings, moniker zones, insertion points.
3. **The code each article pulls in** — only for sections you're actually classifying.

Layer 3 is where a coverage call gets decided, because article prose routinely says "see the
following example" and lets the sample carry the API surface. A feature can be fully
demonstrated in a `Program.cs` the article never names in prose, which reads as a gap until
you open the file.

Resolve the path before judging the element:

| Path form | Resolves to |
|---|---|
| `~/../AspNetCore.Docs.Samples/…` | sibling repo at `{CACHE_ROOT}AspNetCore.Docs.Samples` |
| `~/…` | `DOCSET_ROOT` |
| relative (`generic-host/samples/…`) | the **article's own directory**, not the area root |

The relative case is the one that trips people up. `aspnetcore/fundamentals/host/generic-host.md`
referencing `generic-host/samples/6.x/GenericHostSample/Program.cs` means
`aspnetcore/fundamentals/host/generic-host/samples/…` — resolve from the file's directory,
not from `fundamentals/`.

Only part of a file usually publishes. `id="snippet_Host"` selects the span between
`// <snippet_Host>` and `// </snippet_Host>` markers in the source; `range="1-20"` selects
lines. Judge the span that actually publishes, not the whole file — a feature demonstrated
outside the included region is invisible to readers.

If a path won't resolve, the element is **🟣**; never assume a sample's contents.

## Step 7: Classify every feature element

One row per element from Step 3.

| Status | Criteria |
|---|---|
| ✅ | Documented in the evergreen docset, accurately, under the correct moniker |
| ✏️ | Absent, inaccurate, or present but undiscoverable from the article a reader would actually be in |
| 🟣 | Could not determine — needs a human or product-team answer |

### Expect ✅ to dominate

Documentation teams frequently ship coverage alongside the feature. The .NET 11 SignalR
authentication-refresh audit found **11 of 14** elements already documented.

This is not a wasted run, and it must not be reported as one. Reframe the output explicitly:
it is a verification record plus a short list of residual gaps. Say so in the report's goal
section. An author who believes they are writing net-new content for an already-covered
feature will produce redundant, conflicting prose — the reframing is what prevents that.

### Discoverability is a real gap

Content that exists in one article but is missing from the article a reader would actually
be in is a ✏️, not a ✅. Ask: *which article is the reader in when this question occurs to
them?* Today, hub lifecycle content lived only in the authentication article — complete, but
invisible to anyone reading the Hubs API reference to learn the lifecycle.

Three recurring shapes:

* **Canonical-reference gap** — the API reference article for a type doesn't list a new member.
* **Options-reference gap** — the options article documents the server half of a feature but not the client half.
* **Infrastructure gap** — a new endpoint or route is never named, so nobody configuring a proxy, WAF, or route policy can find it.

### Record what you couldn't settle

A 🟣 with a sharp question beats a guess. Phrase it so a product-team member can answer in
one reply, and put it in the report's review considerations with the evidence you did gather.

## Step 8: Choose the target article

For each ✏️, pick the article a reader is already in — not the one that's easiest to edit,
and not the one that already has a related section.

* Prefer the canonical reference for the API being changed.
* Add a cross-reference from the deep-dive article rather than duplicating prose.
* Never propose an edit inside `EXCLUDE_AS_TARGET`.
* Check whether a sibling feature's audit already covers the content, and note the overlap
  instead of proposing the same edit twice.

Then pin the exact insertion point by line number, and quote the surrounding text verbatim
as the "Before" block. That quote is what a later pass uses to detect drift.

---

## Audit validation checklist

- [ ] A SHA was pinned before the first file read, and reported.
- [ ] The checkout's HEAD was verified to equal the pinned SHA.
- [ ] The checkout covers the whole `DOCSET_ROOT` at **every file type** — not markdown-only, not one area.
- [ ] `SAMPLES_REPOS` were cloned as siblings, so `~/../` references resolve.
- [ ] The clone is an audit cache with a valid marker, within `CACHE_MAX_AGE_HOURS`, clean, and not a developer's working clone.
- [ ] Cache age was reported alongside the SHA when a cached clone was reused.
- [ ] `PRODUCT_COMMIT_SHA` was pinned and used in every product-source link.
- [ ] The API surface inventory was built from a full read of the source.
- [ ] Behavior changes on existing APIs were listed as elements, with the old behavior recorded.
- [ ] The implementing commit was located and its full change surface reviewed.
- [ ] Every endpoint, status code, default, and parameter location was verified against `PRODUCT_REPO`.
- [ ] `EXCLUDE_AS_TARGET` was excluded from search results and never proposed as an edit target.
- [ ] Every candidate article was read in full at the pinned SHA.
- [ ] Code referenced by the sections being judged was resolved and read, including the specific `id=`/`range=` span.
- [ ] Any unresolvable `:::code source=` path was recorded 🟣 rather than assumed.
- [ ] No line number came from a search snippet.
- [ ] `uid` and `monikerRange` were read from front matter, not inferred.
- [ ] Every element has exactly one of ✅, ✏️, 🟣.
- [ ] Discoverability gaps were classified ✏️, not ✅.
- [ ] Unverifiable claims became 🟣 with a specific question.
- [ ] If coverage was largely complete, the report says so up front.
