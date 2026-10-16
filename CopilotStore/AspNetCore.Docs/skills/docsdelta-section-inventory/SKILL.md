---
name: docsdelta-section-inventory
description: >
  Builds a numbered inventory of every major area and feature sub-area in a "What's New in
  ASP.NET Core" release-notes article by reading the published Microsoft Learn page, applying
  the standing exclusions (Blazor, Blazor Hybrid, Breaking changes), and presenting a
  selection menu that defaults to everything eligible. Use when scoping a What's New doc gap
  analysis, or any time you need to enumerate and number the sections of a release-notes
  article before processing them.
---

# docsdelta section inventory

Produces the numbered work list that drives a section-by-section analysis. One inventory
entry becomes one unit of work — and, for a doc gap run, one report.

## Parameters

| Parameter | Required | Default | Description |
|---|---|---|---|
| `WHATS_NEW_URL` | No | *(latest — see below)* | Published Learn URL for the article |
| `MAJOR_VERSION` | No | *(derived)* | Derived from the URL's `aspnetcore-{N}` segment |

---

## Step 1: Resolve the article

### The published page is the source of truth

**Read the published Learn article. Not the GitHub source.**

The release note is a shell that pulls its prose from include files, and that structure is an
implementation detail of the release-notes repo. It is not what readers see, and walking it
introduces a correctness bug: include files exist that no `[!INCLUDE]` directive references,
so they never publish. Inventorying one means scoping work for a feature no reader can find.
Reading the rendered page makes that class of error impossible — if it's on the page, it
shipped; if it isn't, there's nothing to audit.

The page also carries clean anchors and headings in true reading order, so nothing has to be
derived or spot-checked.

### Default URL

Propose the **latest** release-notes article and ask the operator to confirm or replace it:

```
https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-{MAJOR_VERSION}
```

Don't hard-code a version — a fixed number means the skill silently analyzes last year's
release forever. Probe upward from the version you believe is current until a URL 404s, and
take the highest one that resolves.

Present it for confirmation before doing any work:

```
Default What's New article for this run:

  https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11

Use this, or give me a different URL?
```

Accept an operator-supplied URL for any version, and derive `MAJOR_VERSION` from its
`aspnetcore-{N}` segment.

### Record the article's source commit

The page's metadata reports the commit it was rendered from:

```
git_commit_id: 0a1b2c3d4e5f60718293a4b5c6d7e8f901234567
source_path:   aspnetcore/release-notes/aspnetcore-11.md
```

Record that as `RELEASE_NOTE_SHA` and use it for permalinks back to the release note. It's a
better pin than resolving `main` yourself, because it's the exact version the reader saw.

> This is **not** the SHA the coverage audit runs against. That one is pinned separately
> against the docset being audited. Two different sources — don't collapse them.

---

## Step 2: Build the outline

Read the article top to bottom, preserving order:

1. Every `##` heading is a **major area**.
2. Every `###` heading beneath it is a **feature sub-area**.
3. A major area with no `###` beneath it is itself a single unit of work.

Record for each entry: heading text, heading level, published anchor, and the enclosing major
area.

### Anchors

Take the anchor from the page rather than deriving it. Learn disambiguates duplicate headings
by appending `-1`, `-2`, and so on, which slugification won't predict.

`SignalR .NET client supports authentication refresh after redirects`
→ `#signalr-net-client-supports-authentication-refresh-after-redirects`

### If the page is truncated

Long release notes exceed a single fetch. Page through with an increasing start index until
the article ends, and confirm the last major area you captured is the one that actually ends
the page. A truncated fetch silently drops whole areas from the inventory.

---

## Step 3: Apply standing exclusions

Three sections are excluded by default. They're owned elsewhere or need a different kind of
analysis:

| Section | Reason |
|---|---|
| `## Blazor` | Owned by a separate documentation team. |
| `## Blazor Hybrid` | Same. |
| `## Breaking changes` | Not a coverage gap. Breaking changes are tracked through the breaking-changes docset and need migration guidance, not feature coverage. |

Also excluded as a **target** for proposed edits:

| Path | Reason |
|---|---|
| `aspnetcore/release-notes/**` | The release notes are where the feature is announced, and the published article is what this inventory reads. Announcing it again is not coverage. Proposed changes must land in the evergreen docset. |

**Show excluded sections in the inventory anyway**, marked and unselected, in the trailing
"Excluded by default" block of the menu. A hidden exclusion is indistinguishable from a bug,
and the operator may deliberately want one.

**Excluded sections do not consume numbers.** Numbering runs over eligible areas only,
preserving their relative order on the page. Blazor and Blazor Hybrid are the first two H2s
on the .NET 11 page, so SignalR — the third H2 — is major area `1`.

This matters more than it looks. If exclusions consumed numbers, `1.2` would mean one thing
in a run that audited everything and another in a run that skipped Blazor, and every issue
thread referring to a number would become ambiguous.

---

## Step 4: Present the numbered menu

Number major areas `1`, `2`, `3`; sub-areas `1.1`, `1.2`. These numbers are how the operator
selects work and how everyone refers to it afterward, so keep them stable for the run.

```
What's New in ASP.NET Core in .NET 11
https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11
Published from 0a1b2c3  ·  38 sub-areas across 5 eligible major areas

 1. SignalR
    1.1  SignalR authentication refresh
    1.2  Cancel hub invocations from the client
    1.3  SignalR .NET client supports authentication refresh after redirects
    1.4  SignalR TypeScript client supports authentication refresh
 2. Minimal APIs
    2.1  ...
 3. OpenAPI
    3.1  ...
 4. Authentication and authorization
    4.1  ...
 5. Miscellaneous
    5.1  ...

Excluded by default:
 ➖ Blazor              (separate docs team)
 ➖ Blazor Hybrid       (separate docs team)
 ➖ Breaking changes    (not a coverage gap)

Which do you want to analyze? [default: all of 1–5]
  · "all"            every eligible sub-area, in order
  · "1"              a whole major area
  · "1.1"            one sub-area
  · "1.1, 3, 4.2"    any mix
  · "1.1-1.3"        a range
  · "+blazor"        opt an excluded section back in
```

### Interpreting the reply

| Reply | Means |
|---|---|
| Empty, `all`, `yes`, `go` | Every eligible sub-area, in inventory order |
| `1` | Every sub-area under major area 1 |
| `1.1` | That sub-area only |
| `1.1-1.3` | Inclusive range within one major area |
| `+blazor`, `+breaking` | Add an excluded section to the selection |
| `-4` | Everything except major area 4 |

Echo the resolved selection as an explicit list before starting work, so a misparse is caught
before it costs an hour:

```
Selected 4 sub-areas:
  1.1  SignalR authentication refresh
  1.2  Cancel hub invocations from the client
  1.3  SignalR .NET client supports authentication refresh after redirects
  1.4  SignalR TypeScript client supports authentication refresh
```

If a number doesn't exist, say which and re-show the menu. Never silently drop it, and never
guess at an adjacent number.

---

## Step 5: Emit the work list

Each selected entry carries everything downstream needs:

```yaml
- id: "1.1"
  major_area: SignalR
  title: SignalR authentication refresh
  published_anchor: "#signalr-authentication-refresh"
  published_url: "https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11#signalr-authentication-refresh"
  excluded: false
```

Process in inventory order unless told otherwise. Order matters: sibling sub-areas of one
major area share context — the SignalR client-refresh sub-areas are nearly the same analysis
three times — and processing them together avoids re-reading the same articles.

---

## Notes on behavior

* **A section may be covered by a sibling's analysis.** Before writing a report for `1.4`,
  check whether `1.1`'s analysis already located that content. Note the overlap rather than
  duplicating the finding.
* **Never inventory from the GitHub source.** Unreferenced include files exist and don't
  publish. Scoping work for one means analyzing a feature no reader can reach.
* **Don't renumber mid-run.** If the article changes while a run is in progress, finish
  against the version you captured and report the drift.
* **The page's heading order is the article's order.** Preserve it *among eligible areas* —
  their relative sequence must match the published page, because operators navigate by
  position. Excluded areas are pulled out into the trailing "Excluded by default" block and
  don't occupy a slot in the numbered list.

---

## Related

* **[docsdelta-coverage-audit](../docsdelta-coverage-audit/SKILL.md)** — analyzes each selected entry.
* **[docsdelta-report-format](../docsdelta-report-format/SKILL.md)** — the report produced for each selected entry.
