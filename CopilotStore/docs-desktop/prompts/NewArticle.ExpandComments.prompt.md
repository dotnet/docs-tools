---
model: Claude Sonnet 4.6 (copilot)
agent: DocsEditor
description: "Expand selected areas with more details"
---

Expand selected high-level bullet points into detailed, narrative documentation content.

## Step 1 — Validate selection

If the user hasn't selected any content, stop and ask them to select the bullet points or notes they want expanded. Do not proceed without a selection.

## Step 2 — Read the article's reference material

Before writing anything, find and read the XML comment near the top of the article (after the frontmatter and H1). It is typically named `REFERENCE MATERIAL AND RULES` and contains:

- Target audience
- Key points to cover
- Links to reference material

Fetch every linked reference and read it thoroughly. The expanded content must be grounded in that material.

## Step 3 — Identify context

Note the parent heading of the selected content. The heading defines the topic scope — use it to stay focused and avoid introducing unrelated information.

## Step 4 — Write the expanded content

Replace the selected content using this structure:

1. Move the original source bullet points into an XML comment directly above the expanded content (skip this if they're already in an XML comment).
2. Write the expanded content immediately after the comment.

**Writing rules:**

- Write in a narrative style — connected prose, not a list of facts.
- Be detailed but not verbose. Cover every source point without padding.
- Maintain logical flow. Each idea should lead naturally to the next.
- Link to source material or related articles when relevant.
- Balance breadth (covering all scenarios) with depth (actionable guidance).
