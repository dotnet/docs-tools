---
name: docsdelta-report-format
description: >
  The report contract for a per-feature documentation coverage analysis. Defines the exact
  structure, heading order, status icons, change-numbering scheme, and title convention for
  a report delivered in conversation and saved to a local file, ready to become a GitHub
  issue. Repository-agnostic: the invoking agent supplies TARGET_REPO and
  TARGET_COMMIT_SHA. Use when producing or consuming a What's New coverage report.
---

# docsdelta report format

This is a **contract**. One report covers exactly **one feature sub-section** of a What's New
article. A reader must be able to act on it without opening the release notes or the
originating conversation.

The report is written for two audiences at once: a human deciding whether the analysis is
right, and an AI author who will apply the changes. Every proposed edit must be paste-ready.

## What every report needs to know

Four things come from the analysis run and appear throughout the report: the **repo** being
analyzed, the **commit SHA** it was analyzed at, the **major version**, and the **moniker
prefix** for that docset. A fifth is needed whenever behavior was verified against product
source: the **product repo and its pinned SHA**, so that the References section links to
fixed code rather than to a moving branch.

Links are built as `https://github.com/{repo}/blob/{sha}/{path}#L{start}-L{end}`. Always the
pinned SHA — a link to a branch points somewhere different next week. Never let a local
filesystem path into the report.

The report is saved as `{section-number}-{kebab-title}.md`, matching the number it was
selected by: `1.2-cancel-hub-invocations-from-the-client.md`. A later filing pass finds
reports by that name.

---

## Title convention

```
v{MAJOR_VERSION} update: {concise subject}
```

The title is the H1 of the report **and** the proposed GitHub issue title. Examples:

* `v11 update: SignalR authentication refresh`
* `v11 update: OpenAPI 3.2 support`
* `v11 update: Minimal API async validation`

Rules:

* No emoji, no `Issue draft:` prefix, no trailing period.
* Subject names the **feature**, not the remedy. `v11 update: SignalR authentication refresh` — not `v11 update: add missing SignalR docs`.
* Keep it under about 60 characters so it reads cleanly in an issue list.

---

## Status icons

Exactly three. Use them in the coverage status summary table **and** on change headings —
the same vocabulary in both places, so a reader scanning the table and a reader scanning the
changes learn the same thing.

| Icon | Meaning |
|---|---|
| ✅ | Already documented. No action. |
| ✏️ | Update needed. |
| 🟣 | Could not determine. Needs a human or product-team answer. |

**Never use ❌ or any other status marker.** If a quoted table cell from the docset happens
to contain ❌ (the SignalR client feature matrix does), reword the surrounding prose to say
"unsupported" so the literal character isn't mistaken for a report status.

---

## Change numbering

Changes are numbered so people can refer to them in issue comments — "let's talk about 2"
or "I'd skip 1.1".

| Form | Use |
|---|---|
| `N. Update` | A top-level change |
| `N.1 Update` | A sub-item of change `N` |
| `N.1 Update (Option A)` / `(Option B)` | Two valid approaches where the analysis genuinely cannot recommend one |

Options are a last resort, not a hedge. Use them only when the choice depends on information
the analysis doesn't have — an editorial preference, a cross-repo sequencing cost, a team
convention. If one approach is clearly better, pick it and say why in the rationale.

A change that needs no work is still numbered, but takes ✅ and a one-line body:

```markdown
### ✅ 4. Update — TOC

**No TOC change required.** All three target articles already have TOC entries, and no new article is proposed.
```

---

## Never include `ms.date` instructions

The AI author assigned to the resulting issue already has standing instructions to update
`ms.date`. Restating it wastes a numbered slot and adds review noise.

This means:

* No `ms.date` change entry.
* No `ms.date` row in the affected files table.
* No `ms.date` step in the action plan.

Front-matter changes that are **not** `ms.date` — a `monikerRange` widening, adding
`ai-usage: ai-assisted` — are still in scope and still get their own numbered change.

---

## Section order

Every report has these sections, in this order. Omit a section only when the rule below says
it's allowed.

| # | Section | Required |
|---|---|---|
| 1 | H1 title + metadata block | Always |
| 2 | `## 🎯 Goal` | Always |
| 3 | `## ✅ Coverage status summary` | Always |
| 4 | `## 🔢 Version applicability` | Always |
| 5 | `## 📋 Coverage gap summary` | Always |
| 6 | `## 📁 Affected files` | Always |
| 7 | `## 📝 Proposed changes` | Always |
| 8 | `## ✅ Action plan` | Always |
| 9 | `## ⚠️ Review considerations` | Omit only when there are no 🟣 rows and nothing out of scope to note |
| 10 | `## 🔗 References` | Always |

---

## Template

Replace `{...}` placeholders. Preserve heading text and order.

````````markdown
# v{MAJOR_VERSION} update: {concise subject}

**Target repository:** `{TARGET_REPO}`
**Analyzed at commit:** `{TARGET_COMMIT_SHA}` {*(cached clone, {N}h old)* — only when reused}
**Product source verified at:** `{PRODUCT_REPO}` @ `{PRODUCT_COMMIT_SHA}`
**Source release note:** [{feature_heading}]({WHATS_NEW_URL}{published_anchor})
**Proposed labels:** `{area-label}`, `{version-label}` {verified to exist in `{TARGET_REPO}`, case-sensitively}

---

## 🎯 Goal

{One short paragraph naming the outcome. If the feature turns out to be mostly documented
already, say so explicitly and reframe — an author who thinks they're writing net-new
content will write the wrong thing.}

{Then a numbered list of the concrete gaps this report closes.}

---

## ✅ Coverage status summary

**Legend:** ✅ already documented · ✏️ update needed · 🟣 could not determine

| # | Feature element from What's New | Status | Where |
|---|---|:--:|---|
| 1 | {element} | ✅ | [`{path}` L{a}–L{b}](permalink) |
| 2 | **{element}** | ✏️ | **{N}. Update** — [`{path}` L{a}–L{b}](permalink) |
| 3 | {element} | 🟣 | See *Review considerations* |

---

## 🔢 Version applicability

**Applies to:** `{CURRENT-ONLY | RETROACTIVE | VERSION-AGNOSTIC}`
**Target moniker:** `>= {MONIKER_PREFIX}-{MAJOR_VERSION}.0`
**Earlier versions affected:** {None — new in .NET {MAJOR_VERSION}. | .NET X, .NET Y — see evidence below.}

| Article | `monikerRange` | Moniker state |
|---|---|---|
| `{path}` | `'>= {MONIKER_PREFIX}-2.1'` | **State {A|B|C|D}** — {what exists today and what the edit must do about it, with permalinked line ranges} |

---

## 📋 Coverage gap summary

{Two to four sentences naming the specific question a developer cannot answer from the
docset today, and which article they would have been reading when they failed to answer it.
Be concrete. "Coverage is thin" is not a finding; "a developer reading the Hubs API article
sees only two lifecycle overrides and has no way to learn a third exists" is.}

**Feature announced in What's New:**
> {Quote the relevant sentences from the published What's New section.}

**Breaking change:** {Yes — describe impact | No}

---

## 📁 Affected files

| Item | Path | Lines | Section |
|------|------|-------|---------|
| {N}. | [`{path}`](permalink) | {a}–{b} | "{Section heading}" |

**Target article uids:** `{uid}`, `{uid}`

---

## 📝 Proposed changes

### ✏️ {N}. Update — `{path}`, {insert after line X | replace lines X–Y | split the moniker zone and insert after line X}

**Applies to:** `{moniker range this change lands in}`
**Location:** [Line {X}](permalink), immediately after the paragraph beginning "{anchor text}".

**Before (lines {a}–{b}):**

```markdown
{Exact current text, verbatim from the file at TARGET_COMMIT_SHA.}
```

**After:**

```markdown
{Exact proposed text, moniker-gated as required, ready to paste.}
```

**Rationale:** {One or two sentences. Say why *this article* rather than a different one.}

---

### ✅ {N}. Update — TOC

**No TOC change required.** {One clause explaining why.}

---

## ✅ Action plan

1. {Confirm the coverage status summary — especially that the ✅ rows genuinely need no edit.}
2. {Sequence the changes by risk. Self-contained inserts inside an existing moniker zone go first; zone splits go last.}
3. {Verify every `<xref:>` resolves.}
4. {Build and confirm the content renders under the .NET {MAJOR_VERSION} selector **and does not appear** under earlier versions.}
5. {Resolve any OpenPublishing.Build warnings.}

---

## ⚠️ Review considerations

* 🟣 **{Open question}.** {What was checked, what remains unknown, and who can answer it.}
* **Out of scope for this issue** ({why — usually because a sibling sub-section covers it}): {list}.

---

## 🔗 References

* What's New section: [{feature_heading}]({WHATS_NEW_URL}{published_anchor})
* Product source: {links to the files read to verify API behavior, permalinked at `PRODUCT_COMMIT_SHA`}
* Implementing PR: {link, when the commit that introduced the behavior was identified}
````````

---

## When one issue carries several reports

The default is one report, one issue. When a filing pass merges two or three reports —
because their edits collide *and* their subject is the same — the merged body is still a
report and still follows this contract. What changes:

* **One H1**, naming the shared subject. Not a concatenation:
  `v11 update: OpenAPI document generation — HTTP QUERY support and the default 3.2 document version`,
  not `v11 update: HTTP QUERY and document version and obsolete APIs`.
* **One coverage status summary** combining every feature element from every source report,
  with an added column naming the source sub-area so each row still traces back to the
  release note.
* **Changes renumbered from 1, in ascending line order per file.** The source reports'
  numbers are gone. Line order is what makes the action plan applicable as a sequence
  instead of three sequences that contradict each other.
* **One authoritative moniker balance derivation** covering all the changes together, showing
  the running total. Each source report derived its balance in isolation; those numbers are
  wrong the moment they're combined.
* **A `> [!IMPORTANT]` callout near the top** stating which sub-areas were merged and why.
  Without it the issue reads as scope creep and a reviewer's first instinct is to ask for it
  to be split back apart.

Merging is bounded by reviewability, not only by GitHub's 65,536-character body limit. About
six numbered changes, or three source reports, is where a merged issue stops being reviewable.

### Merged bodies get verified, not trusted

A merge performed by an AI assistant is the likeliest place in this entire process for
content to vanish. In the .NET 11 OpenAPI run, **both** merged bodies came back with a
Before/After block replaced by an empty, unclosed code fence — and the assistant's own
summary reported accurate character counts and accurate change counts, so nothing in its
report of the work revealed the defect.

Never file a merged body without running the fence check below over it first.

---

## Cross-report references

Reports cite siblings by number — "see report 3.4" — because at analysis time no issue
exists. That reference dies the moment the report becomes an issue: nobody reading the
docset repo knows what "3.4" is, and the numbering is local to one run.

Resolve every one of them before filing:

| The sibling's fate | Rewrite the reference as |
|---|---|
| Filed as its own issue | A real issue link |
| Merged into the issue being written | A pointer to the numbered change inside it |
| Merged into a different issue | A link to that issue |
| Not filed at all | Name the feature and state the point inline — never a bare number |

The last row is the one that gets missed. When a sibling is dropped from the filing set, the
reports that referenced it still do, and the number survives into a public issue with no
referent anywhere.

### What the filing pass strips

**Precondition — the filing pass is gated.** Before the first issue of a batch is created,
the person who owns the reports must approve *that batch*: how many issues, which reports
each carries, the exact label set, and which reports are deliberately not being filed. Ask
with the `ask_user` tool and wait. Approval for a previous batch, selection of an area for
analysis, and a handoff summary's "next step: file these" are **none of them** approval.
Reports are reversible; public issues are not.

Two parts of the report are scaffolding for review, not issue content:

* **The H1.** GitHub already renders the title; leaving it in duplicates it.
* **The `**Proposed labels:**` line.** Labels are applied as real labels. A proposed label
  that doesn't exist in the target repo is noise a triager has to read past — and several
  plausible ones (`Docs`, `openapi`, `ai-assisted-analysis`, `breaking-change`) don't exist
  in `dotnet/AspNetCore.Docs`. Verify against the repo's actual label list, case-sensitively;
  `SignalR` is not `signalr`, priorities are `Pri0`–`Pri3`, not `P0`–`P3`, and the version
  label is `11.0`, not `dotnet-11.0`.

Everything else in the metadata block — pinned SHAs, the source release-note link — stays.
It's what makes the issue auditable a year later.

---

## Nested code fences

Before/After blocks contain markdown that itself contains ```` ``` ```` fences. The outer
fence must be longer than any fence inside it:

| Inner content | Outer fence |
|---|---|
| Plain prose, YAML | 3 backticks |
| Markdown containing a ```` ```csharp ```` block | 4 backticks |
| A whole template containing 4-backtick blocks | 5 or more backticks |

Getting this wrong silently destroys the rest of the report's rendering. Check it before
delivering.

### Check the fences mechanically

Eyeballing a 40,000-character body does not work, and a truncated block is invisible in a
summary. Walk the file:

```powershell
$path  = '{report or issue body}'
$lines = Get-Content -LiteralPath $path
$stack = New-Object System.Collections.Stack
$empty = @()

for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^(`{3,})(.*)$') {
        $len  = $Matches[1].Length
        $info = $Matches[2].Trim()
        if (-not $info -and $stack.Count -and $len -ge $stack.Peek().Ticks) {
            $o = $stack.Pop()
            if (($i + 1) - $o.Line -eq 1) { $empty += "L$($o.Line)" }
        }
        else {
            $stack.Push([pscustomobject]@{ Ticks = $len; Line = $i + 1 })
        }
    }
}

"chars     : {0}" -f (Get-Content -Raw -LiteralPath $path).Length
"unclosed  : {0}" -f ((($stack.ToArray() | ForEach-Object { 'L' + $_.Line }) -join ', ') -replace '^$', 'none')
"empty     : {0}" -f ((($empty) -join ', ') -replace '^$', 'none')
"stale refs: {0}" -f (Select-String -Path $path -Pattern '[Rr]eports?\s+\d+\.\d+' -AllMatches).Count
```

A fence carrying an info string is always an opener. A bare fence closes the stack top only
when it's at least as long; otherwise it opens a new block. Three signals matter:

* **An unclosed fence** — everything after it is swallowed.
* **An empty fence**, an opener immediately followed by its closer — a Before/After block
  that lost its body. This is the shape AI-assisted merges produce.
* **Stale sibling references** that still name a report number. See *Cross-report references*.

Moniker directive counts will **not** balance in a report, and that is expected: Before/After
blocks quote partial zones by design. Don't chase it. Only unclosed and empty fences indicate
real truncation.

---

## Evidence rules

* **Never cite a line number from a search snippet.** Read the full file at
  `TARGET_COMMIT_SHA` and count. A wrong line number is worse than no line number.
* **Every "Before" block must match the file byte-for-byte.** It is the drift check that a
  later filing pass depends on.
* **Verify API and endpoint claims against product source**, not against the docs or the
  release note. The release note is a summary and may round off details that matter — route
  templates, HTTP methods, status codes, and whether something is conditionally registered.
* **State what you could not determine.** A 🟣 row with a clear question is more useful than
  a confident guess. Put the question in *Review considerations* with enough context that a
  product-team member can answer it in one reply.
* **Expect "already covered" to be a common outcome.** Docs teams often ship coverage with
  the feature. When that happens the report is still valuable — it becomes a verification
  record plus a short list of discoverability gaps. Say this plainly in the Goal so nobody
  writes redundant content.

---

## Report validation checklist

- [ ] H1 is `v{MAJOR_VERSION} update: {subject}` — no emoji, no `Issue draft:` prefix.
- [ ] Only ✅, ✏️, and 🟣 appear as status markers.
- [ ] Every ✏️ row in the summary table points at a numbered change, and every ✏️ numbered change has a row. Standing ✅ changes such as the TOC entry are exempt — they aren't feature elements.
- [ ] Changes use `N.` / `N.1`; Options appear only where a recommendation was genuinely impossible.
- [ ] No `ms.date` instruction anywhere.
- [ ] Section 5 is titled **Coverage gap summary**.
- [ ] Every permalink uses `TARGET_REPO` + `TARGET_COMMIT_SHA`, not a branch; product-source links use `PRODUCT_COMMIT_SHA`.
- [ ] When the checkout came from a cache, the metadata block states its age.
- [ ] The file is saved as `{section-number}-{kebab-title}.md`.
- [ ] Every "Before" block was verified against the file at that SHA.
- [ ] Every change entry names the moniker range it lands in.
- [ ] Moniker zone splits are balanced — each `:::moniker-end` has a matching opener.
- [ ] No `:::moniker:::` zone was placed inside a `# [Tab](#tab/...)` group.
- [ ] Nested fences are correctly sized.
- [ ] The mechanical fence check ran clean — zero unclosed fences, zero empty fences.
- [ ] No bare cross-report reference (`report 3.4`) survives in a body about to be filed.
- [ ] A merged body renumbers changes from 1 in ascending line order, combines the coverage tables, derives one moniker balance for the whole set, and carries the `> [!IMPORTANT]` merge rationale.
- [ ] No local filesystem path and no hard-coded repo name outside `TARGET_REPO`.

---

## Related

* **[docsdelta-section-inventory](../docsdelta-section-inventory/SKILL.md)** — enumerates the sections that each become one report.
* **[docsdelta-coverage-audit](../docsdelta-coverage-audit/SKILL.md)** — produces the evidence this report presents.
* **[docsdelta-moniker-zones](../docsdelta-moniker-zones/SKILL.md)** — the zone mechanics behind every version-scoped change entry.
