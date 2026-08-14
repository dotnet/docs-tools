---
name: uuf-impact-triage
description: Prioritize User Feedback work items by customer impact, page views, validity, and ease of repair. Use when asked to triage UUF items or identify feedback worth fixing.
---

# UUF impact triage

Analyze User Feedback (UUF) work items and recommend the highest-value documentation fixes. Prioritize issues that affect many readers, represent valid customer problems, and can be corrected with focused changes.

## Input

Use the UUF query or list of work items supplied by the user.

If the user doesn't provide a query:

1. Look for a previously identified UUF query.
1. Ask for the query URL if you can't find one.
1. Don't substitute an unrelated work-item query.

The user might specify how many items to recommend. Recommend three items by default.

## Workflow

### 1. Retrieve candidate items

Retrieve all active User Feedback work items from the specified query.

For each item, collect:

- Work item ID and title.
- Customer feedback verbatim.
- Article title and live URL.
- Priority and state.
- Page views.
- Article review date, if available.
- Any existing triage notes.

Don't rank an item until you have read its full feedback.

### 2. Verify the feedback

Open the current English-language article and compare it with the customer report.

Also inspect related articles when the information might already exist elsewhere.

Classify the feedback as:

- **Valid:** The documentation contains an error or meaningful omission.
- **Partially valid:** The customer's explanation isn't fully correct, but the documentation contributed to the confusion.
- **Invalid:** The current documentation already addresses the issue or the requested change would be incorrect.
- **Needs investigation:** Product behavior or technical guidance requires subject-matter expert confirmation.

Don't recommend invalid feedback merely because the article has high traffic.

### 3. Estimate the fix

Classify the likely effort:

- **Small:** A link, note, prerequisite, include directive, value correction, or short clarification.
- **Medium:** A section revision, new example, or several coordinated edits.
- **Large:** Major restructuring, extensive testing, new media, or product-team validation.

Describe the specific proposed fix. Don't use vague recommendations such as “improve clarity.”

### 4. Score each opportunity

Score each item from zero through 10.

#### Audience reach: zero through four points

| Monthly page views | Points |
|---|---:|
| 4,000 or more | 4 |
| 2,000 through 3,999 | 3 |
| 500 through 1,999 | 2 |
| Fewer than 500 | 1 |
| Unknown | 0 |

#### Customer impact: zero through three points

| Impact | Points |
|---|---:|
| Blocks task completion or corrects dangerous or technically incorrect guidance | 3 |
| Addresses a significant omission or recurring source of confusion | 2 |
| Provides a minor clarification or usability improvement | 1 |
| Feedback is invalid | 0 |

#### Fix confidence: zero through two points

| Confidence | Points |
|---|---:|
| Correct solution is verified against authoritative sources | 2 |
| Likely solution is clear but requires minor validation | 1 |
| Product or subject-matter expert confirmation is required | 0 |

#### Effort bonus: zero or one point

| Effort | Points |
|---|---:|
| Small, focused fix | 1 |
| Medium or large fix | 0 |

Use the score as a decision aid, not an automatic verdict. Prefer a lower-scoring technically important issue when it has substantially greater customer consequences.

### 5. Rank the recommendations

Rank valid and partially valid items by score. Use these tie breakers in order:

1. Greater customer impact.
1. More page views.
1. Smaller implementation effort.
1. Older review date.

Exclude invalid items from the recommended list. List them separately when explaining why they shouldn't be fixed.

## Output

Use this format:

```markdown
# UUF triage results

**Items reviewed:** {COUNT}
**Recommended fixes:** {COUNT}
**Query:** {QUERY URL}

## Recommended items

### 1. AB#{WORK ITEM ID}: {TITLE}

- **Score:** {SCORE}/10
- **Page views:** {PAGE VIEWS}
- **Feedback validity:** {VALIDITY}
- **Estimated effort:** {SMALL | MEDIUM | LARGE}
- **Customer report:** "{VERBATIM}"
- **Documentation finding:** {WHAT THE CURRENT ARTICLE SAYS}
- **Recommended fix:** {SPECIFIC CHANGE}
- **Why prioritize it:** {REACH, CUSTOMER IMPACT, AND EFFORT RATIONALE}

## Items not recommended

### AB#{WORK ITEM ID}: {TITLE}

- **Reason:** {INVALID, LOW VALUE, DUPLICATE, OR NEEDS INVESTIGATION}
- **Finding:** {EVIDENCE}
- **Suggested disposition:** {CLOSE, DEFER, OR REQUEST TECHNICAL REVIEW}

Include the score breakdown for each recommended item:

Audience {N}/4 + Impact {N}/3 + Confidence {N}/2 + Effort {N}/1

Rules

• Verify feedback against the live documentation before recommending a change.
• Treat customer feedback as evidence, not automatically correct technical guidance.
• Consider both page views and the severity of the customer problem.
• Favor focused fixes with measurable customer value.
• Preserve technically correct documentation when feedback requests the wrong behavior.
• Identify the exact article section that requires modification.
• Cite authoritative technical sources when validating disputed behavior.
• Don't edit articles, update work items, or create branches unless the user explicitly asks.
• Don't expose private query URLs or internal work-item details outside authorized repositories.
