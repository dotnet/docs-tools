---
name: cpp-blog-monitor
description: "Check the Microsoft C++ Team Blog for posts in a requested date range, save unseen C++ DevBlog summaries as Markdown in the temp directory, and avoid reporting the same post twice. Use when the user says: check the C++ blog, C++ DevBlog monitor, summarize C++ blog posts, or what's new on the C++ Team Blog."
---

# C++ DevBlog monitor

Check the official Microsoft C++ Team Blog and report only posts that haven't
been successfully summarized in an earlier run.

## Run the monitor

1. If the user hasn't supplied a date range, ask which date range to check.
   Accept explicit inclusive ranges such as `1-1-2025 to 1-1-2026` and natural
   ranges such as `this year`, `this month`, `last month`, or
   `the last two months`.

2. Run the deterministic discovery helper with the user's answer unchanged:

   ```powershell
   python "${SKILL_DIR}/scripts/check_cpp_blog.py" check --date-range "<date range>"
   ```

3. Parse the JSON response. If `unseen` is empty, tell the user there are no new
   posts and stop.

4. For each object in `unseen`, fetch its `url` and summarize the article from
   the page content. Don't summarize from the RSS excerpt alone.

5. Report each post with this concise structure:

   ```markdown
   ## [Post title](canonical URL)

   **Published:** Month D, YYYY | **Author:** Name

   Two or three sentences explaining the announcement and why it matters to a
   C++ developer.

   **Key points**

   - Three to five concrete, technically meaningful points.
   ```

6. After successfully summarizing a post, pipe its complete Markdown block to
   the helper. The helper creates or appends to
   `%TEMP%\BlogSummaries-M-D-YYYY.md`, using the local run date, and prevents
   duplicate entries for the same post identity. Pass the resolved inclusive
   endpoints from the `date_range.from` and `date_range.through` fields in the
   `check` response:

   ```powershell
   @'
   <complete Markdown summary block>
   '@ | python "${SKILL_DIR}/scripts/check_cpp_blog.py" save-summary --post-id "<identity from check output>" --date-from "<date_range.from>" --date-through "<date_range.through>"
   ```

7. Parse the JSON response and report the returned `output` path to the user.
   Only after the Markdown summary is saved, record the post as reported:

   ```powershell
   python "${SKILL_DIR}/scripts/check_cpp_blog.py" record --post-id "<identity from check output>"
   ```

8. Include the same summary block in the chat response. If fetching,
   summarizing, or saving one post fails, don't record that post. Continue with
   the remaining posts and identify the failed URL in the response.

## State and deduplication

The helper stores state at
`~/.copilot/cpp-blog-monitor/reported-posts.json` by default. It compares both
the WordPress post identity and canonical URL, so historical API results and
current RSS results deduplicate against the same records. Writes use a temporary
file followed by an atomic replacement.

Summary reports are stored separately in the operating system's temp directory
as `BlogSummaries-M-D-YYYY.md`. Multiple successful summaries on the same local
date are appended to that day's file. Each distinct resolved inclusive run
range appears directly below the report's **Generated** line. Both report and
ledger writes are atomic.

Explicit range endpoints are inclusive. `this year` and `this month` start at
the corresponding calendar boundary and end today. `last month` is the previous
complete calendar month. `the last N months` is a rolling range from the same
day N months ago through today.

Override the state file for testing or isolated runs with `--ledger <path>`.
Use `--limit <number>` on `check` only when the user requests a maximum number
of posts.

## Boundaries

- This skill runs on demand. It doesn't schedule itself or continuously poll.
- Historical discovery uses the blog's paginated WordPress API, not the
   10-entry RSS window.
- Use Windows Task Scheduler, cron, or another automation service for unattended
  checks.
- Don't record a post before its summary succeeds.
- Don't record a post before its Markdown summary is saved.
- Don't modify or clear the ledger unless the user explicitly asks.
