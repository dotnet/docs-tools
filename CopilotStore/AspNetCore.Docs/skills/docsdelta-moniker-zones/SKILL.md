---
name: docsdelta-moniker-zones
description: >
  Insert version-scoped content into Microsoft Learn articles that use moniker zones.
  Covers determining which zone an insertion point falls inside, the four moniker states an
  article can be in, the split pattern required when new content must be scoped more
  narrowly than the zone containing it, why moniker zones must never be placed inside tab
  groups, and how to verify zone balance. Use when proposing or applying an edit to a
  versioned docset article that contains :::moniker range=::: directives.
---

# docsdelta moniker zones

Moniker zones control which .NET version selector shows a block of content. Get them wrong
and the failure is silent and public: content appears under versions where the API doesn't
exist, or vanishes from the version it documents.

This is the highest-risk mechanical part of a versioned docs edit. Everything here assumes
you have read the target article **in full** and recorded every zone boundary with its line
numbers.

## Parameters

| Parameter | Example | Purpose |
|---|---|---|
| `MONIKER_PREFIX` | `aspnetcore` | Produces `>= aspnetcore-11.0` |
| `MAJOR_VERSION` | `11.0` | The version new content is scoped to |
| `VERSION_FLOOR` | `8.0` | Oldest supported version; never create a zone below it |

---

## The two version mechanisms

| Mechanism | Scope | Where |
|---|---|---|
| `monikerRange` | The whole article | YAML front matter |
| `:::moniker range="..."` / `:::moniker-end` | A block within the article | Body |

Front matter sets the article's outer bounds. A zone can only narrow that range, never widen
it. An article with `monikerRange: '>= aspnetcore-3.1'` cannot show content to .NET Core 2.1
readers no matter what its zones say.

---

## Step 1: Find the enclosing zone

Before proposing any insertion, determine what zone the insertion point is already inside.

From the target line, scan **backwards** for the nearest `:::moniker range=` and **forwards**
for the nearest `:::moniker-end`. That pair is the enclosing zone. If the backward scan
reaches the top of the body without a match, the insertion point is unzoned and governed
only by front matter.

Record the zone's range expression and both boundary line numbers. You need all three.

> Zones don't nest. A `:::moniker-end` closes the most recent opener, so a stray opener
> silently swallows the rest of the article.

## Step 2: Identify the article's moniker state

| State | Condition | What the edit must do |
|---|---|---|
| **A** | No front-matter range, no zones anywhere | Add a zone around the new content only |
| **B** | Front-matter range only, no zones in the body | Add the first zone around the new content |
| **C** | Insertion point is already inside a zone matching the target version | Insert directly — **no structural change** |
| **D** | Insertion point is inside a zone that's broader or different from the target version | **Split the zone** — see Step 3 |

State C is the safe case. Sequence C edits first when an audit produces several, so early
progress carries no structural risk.

State D is where mistakes happen.

### Don't be fooled by an unrelated zone

An article can contain a zone for your target version that has nothing to do with your
insertion point. In a .NET 11 SignalR audit, `hubs.md` had a `>= aspnetcore-11.0` zone at L49–L54 — but
it covered an unrelated feature, and the actual insertion point at L381 sat inside a
`>= aspnetcore-8.0` zone spanning L56–L411. That's **State D**, not State C.

The state is a property of the **insertion point**, not of the article.

## Step 3: The split pattern

When content must be scoped to `>= {MONIKER_PREFIX}-{MAJOR_VERSION}` but sits inside a
broader zone, close the broader zone, open the narrow one, close it, and reopen the broader
one.

Given an insertion inside a `>= aspnetcore-8.0` zone:

````markdown
{last line of existing 8.0 content}

:::moniker-end

:::moniker range=">= aspnetcore-11.0"

{new version-scoped content}

:::moniker-end

:::moniker range=">= aspnetcore-8.0"

{existing content resumes}
````

Four directives, always in that order. The net change to zone balance is zero: one opener and
one closer added to each range.

Rules:

* **Reopen with the original range expression, character for character.** `>= aspnetcore-8.0`
  and `>= aspnetcore-8.0 <= aspnetcore-10.0` are different zones; retyping from memory
  silently changes what the rest of the article applies to.
* **Blank lines around every directive.** A directive adjacent to prose or a list can fail to
  parse.
* **Split at a section boundary where possible.** Splitting mid-list or mid-table produces
  content that renders correctly but is unmaintainable.
* **Never create a zone below `VERSION_FLOOR`.**

### Overlap is intentional

`>= aspnetcore-8.0` and `>= aspnetcore-11.0` both match a .NET 11 reader, so the 11.0 block
appears in document order between two 8.0 blocks — exactly the intent. Don't "fix" this by
bounding the 8.0 zone to `<= aspnetcore-10.0` unless the surrounding content genuinely stops
applying at 10.0. Bounding it removes that content from .NET 11 readers, which is almost
never what's wanted.

## Step 4: Never put a zone inside a tab group

Tab groups look like this:

```markdown
# [.NET](#tab/dotnet)
...
# [JavaScript](#tab/javascript)
...
---
```

Placing `:::moniker:::` directives inside one is fragile and breaks rendering in ways that
don't reliably show up in a local build.

If version-scoped content belongs near a tab group, put it **before or after** the group, or
zone the entire group. If that isn't editorially acceptable, say so in the report and leave
the group alone rather than shipping a fragile edit.

In the .NET 11 SignalR audit this rule kept two client-options tables untouched. That turned out to be correct on
the merits too — the APIs in question were builder methods, not members of the type those
tables document.

## Step 5: Verify balance

Before delivering, count directives across the whole file — not just the edited region.

```powershell
$t = Get-Content -Raw {path}
($t | Select-String ':::moniker range=' -AllMatches).Matches.Count
($t | Select-String ':::moniker-end' -AllMatches).Matches.Count
```

The counts must match. Then walk the file top to bottom confirming that every opener closes
before the next one opens.

A file that's balanced but interleaved renders as nonsense, so the count alone isn't enough.

---

## Choosing the version scope

| Applicability | Meaning | Zone required |
|---|---|---|
| `CURRENT-ONLY` | New in this release | `>= {MONIKER_PREFIX}-{MAJOR_VERSION}` |
| `RETROACTIVE` | Also shipped in earlier supported versions | One zone per applicable range — cite evidence |
| `VERSION-AGNOSTIC` | Conceptual, no version dependency | None |

Default to `CURRENT-ONLY` for a feature announced in a release note. Claim `RETROACTIVE` only
with evidence — a backport PR, a servicing note, or an API reference showing the earlier
version. "It probably works in 10 too" is not evidence.

---

## Zone editing checklist

- [ ] The article was read in full and every zone boundary recorded with line numbers.
- [ ] The enclosing zone of the insertion point was identified by scanning both directions.
- [ ] The moniker state was determined from the **insertion point**, not from the article.
- [ ] Any same-version zone elsewhere in the article was confirmed relevant before relying on it.
- [ ] State D edits use the full four-directive split.
- [ ] The reopened range expression matches the original character for character.
- [ ] Blank lines surround every directive.
- [ ] No zone was placed inside a `# [Tab](#tab/...)` group.
- [ ] No zone was created below `VERSION_FLOOR`.
- [ ] Opener and closer counts match across the whole file.
- [ ] Zones were confirmed non-interleaved by a top-to-bottom walk.
- [ ] `RETROACTIVE` claims cite evidence.
