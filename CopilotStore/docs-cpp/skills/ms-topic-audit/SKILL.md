---
name: ms-topic-audit
description: Audit Markdown files in a folder and report whether each ms.topic metadata value matches the article's content and structure. Use when asked to review, validate, or inventory ms.topic metadata.
---

# `ms.topic` metadata audit

Review Markdown articles in a specified folder and determine whether each article's `ms.topic` value accurately represents its content.

## Input

Accept a folder path from the user. Review all `.md` files in that folder recursively unless the user limits the scope.

If no folder is provided, ask for one.

## Workflow

### 1. Determine the repository taxonomy

Before evaluating files:

1. Look for repository metadata guidance, schemas, templates, or contributor instructions.
1. Identify the `ms.topic` values permitted by the repository.
1. Use repository-specific definitions when they differ from general Microsoft Learn conventions.
1. Don't invent or recommend a value that the repository doesn't support.

### 2. Inspect each article

For each Markdown file:

1. Read the YAML frontmatter.
1. Record the current `ms.topic` value.
1. Read enough of the complete article to understand its primary purpose.
1. Examine the title, introduction, headings, steps, tables, examples, and expected reader outcome.
1. Classify the article by its dominant purpose rather than isolated sections.

Ignore Markdown files that aren't publishable articles, such as includes, templates, or generated files, unless they contain `ms.topic` metadata.

### 3. Classify the content

Use these general signals, subject to the repository's taxonomy:

| Article characteristic | Likely topic type |
|---|---|
| Explains what something is, why it matters, or how it works | `concept-article` |
| Gives instructions for completing a task | `how-to` |
| Provides authoritative API, syntax, option, or language-element details | `reference` |
| Teaches a complete scenario through sequential steps | `tutorial` |
| Gets the reader working quickly with minimal explanation | `quickstart` |
| Summarizes a product, feature area, or collection of capabilities | `overview` |
| Diagnoses symptoms and provides causes and resolutions | `troubleshooting` |
| Organizes and links to content rather than teaching the subject directly | `landing-page` |

An article can contain several content types. Choose the type that best describes its primary reader intent.

### 4. Compare the classification

Assign one result:

- **Match:** The existing `ms.topic` accurately describes the article.
- **Likely mismatch:** Another permitted value clearly describes the article better.
- **Missing:** The article requires `ms.topic` but doesn't contain it.
- **Invalid value:** The value isn't permitted by the repository taxonomy.
- **Needs human review:** The article is genuinely mixed-purpose or the taxonomy is ambiguous.
- **Not applicable:** The file isn't a publishable article.

Also assign a confidence level:

- **High:** The article has strong structural evidence for one type.
- **Medium:** The primary intent is apparent, but the article contains significant secondary content.
- **Low:** The classification depends on product or publishing context not available in the file.

## Output

Produce a report with this format:

```markdown
# `ms.topic` audit

**Folder:** `{FOLDER PATH}`
**Files reviewed:** {COUNT}

| Result | Count |
|---|---:|
| Match | {COUNT} |
| Likely mismatch | {COUNT} |
| Missing | {COUNT} |
| Invalid value | {COUNT} |
| Needs human review | {COUNT} |
| Not applicable | {COUNT} |

## Findings

| File | Current `ms.topic` | Recommended value | Result | Confidence | Evidence |
|---|---|---|---|---|---|
| `path/file.md` | `concept-article` | `how-to` | Likely mismatch | High | The article consists primarily of sequential task instructions. |

## Detailed recommendations

### `path/file.md`

- **Current value:** `{CURRENT VALUE}`
- **Recommended value:** `{RECOMMENDED VALUE}`
- **Result:** {RESULT}
- **Confidence:** {CONFIDENCE}
- **Primary reader intent:** {WHAT THE ARTICLE ENABLES THE READER TO DO OR UNDERSTAND}
- **Evidence:** {SPECIFIC INTRODUCTION, HEADINGS, OR STRUCTURAL SIGNALS}
- **Rationale:** {WHY THE CURRENT VALUE MATCHES OR SHOULD CHANGE}

List mismatches, missing values, invalid values, and uncertain classifications before files that match.

Rules

• Evaluate the article's primary purpose, not its title alone.
• Cite specific evidence from the article for every recommended change.
• Don't treat the presence of numbered steps as conclusive when the article is primarily conceptual or reference content.
• Distinguish a focused how-to from a tutorial that teaches a broader end-to-end scenario.
• Don't recommend changing valid metadata based only on neighboring articles.
• Don't modify files unless the user explicitly confirms the proposed changes.
• Preserve all unrelated metadata when applying approved changes.
• Report uncertainty instead of forcing a classification.
