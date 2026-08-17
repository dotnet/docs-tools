#!/usr/bin/env python3
"""Discover and record unseen Microsoft C++ Team Blog posts."""

from __future__ import annotations

import argparse
import calendar
import hashlib
import html
import json
import os
import re
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
from datetime import date, datetime, timedelta, timezone
from email.utils import parsedate_to_datetime
from pathlib import Path
from typing import Any


DEFAULT_FEED_URL = "https://devblogs.microsoft.com/cppblog/feed/"
DEFAULT_API_URL = "https://devblogs.microsoft.com/cppblog/wp-json/wp/v2/posts"
DEFAULT_LEDGER = Path.home() / ".copilot" / "cpp-blog-monitor" / "reported-posts.json"
USER_AGENT = "cpp-blog-monitor/1.0 (+https://devblogs.microsoft.com/cppblog/)"
CONTENT_NAMESPACE = "{http://purl.org/rss/1.0/modules/content/}encoded"
CREATOR_NAMESPACE = "{http://purl.org/dc/elements/1.1/}creator"


class MonitorError(Exception):
    """A user-actionable feed or ledger error."""


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def canonicalize_url(value: str) -> str:
    value = value.strip()
    if not value:
        return ""
    parsed = urllib.parse.urlsplit(value)
    scheme = "https" if parsed.scheme in {"http", "https"} else parsed.scheme.lower()
    hostname = (parsed.hostname or "").lower()
    port = parsed.port
    netloc = hostname
    if port and not ((scheme == "https" and port == 443) or (scheme == "http" and port == 80)):
        netloc = f"{hostname}:{port}"
    path = re.sub(r"/{2,}", "/", parsed.path or "/")
    if path != "/":
        path = path.rstrip("/") + "/"
    return urllib.parse.urlunsplit((scheme, netloc, path, "", ""))


def element_text(parent: ET.Element, tag: str) -> str:
    element = parent.find(tag)
    return "" if element is None or element.text is None else element.text.strip()


def plain_text(value: str) -> str:
    without_markup = re.sub(r"<[^>]+>", " ", value)
    return re.sub(r"\s+", " ", html.unescape(without_markup)).strip()


def normalize_date(value: str) -> str:
    if not value:
        return ""
    try:
        parsed = parsedate_to_datetime(value)
    except (TypeError, ValueError):
        return value
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")


def parse_calendar_date(value: str) -> date:
    for date_format in ("%m-%d-%Y", "%m/%d/%Y", "%Y-%m-%d"):
        try:
            return datetime.strptime(value.strip(), date_format).date()
        except ValueError:
            continue
    raise MonitorError(
        f"Unsupported date '{value.strip()}'. Use M-D-YYYY, M/D/YYYY, or YYYY-MM-DD."
    )


def shift_months(value: date, months: int) -> date:
    month_index = value.year * 12 + value.month - 1 + months
    year, zero_based_month = divmod(month_index, 12)
    month = zero_based_month + 1
    day = min(value.day, calendar.monthrange(year, month)[1])
    return date(year, month, day)


def parse_date_range(value: str, today: date | None = None) -> tuple[date, date]:
    today = today or datetime.now().astimezone().date()
    normalized = re.sub(r"\s+", " ", value.strip().lower())
    number_words = {
        "one": 1,
        "two": 2,
        "three": 3,
        "four": 4,
        "five": 5,
        "six": 6,
        "seven": 7,
        "eight": 8,
        "nine": 9,
        "ten": 10,
        "eleven": 11,
        "twelve": 12,
    }

    if normalized == "this year":
        start, through = date(today.year, 1, 1), today
    elif normalized == "this month":
        start, through = date(today.year, today.month, 1), today
    elif normalized == "last year":
        start, through = date(today.year - 1, 1, 1), date(today.year - 1, 12, 31)
    elif normalized == "last month":
        through = date(today.year, today.month, 1) - timedelta(days=1)
        start = date(through.year, through.month, 1)
    elif match := re.fullmatch(r"(?:the )?last (\d+|[a-z]+) months?", normalized):
        token = match.group(1)
        months = int(token) if token.isdigit() else number_words.get(token)
        if months is None or months < 1:
            raise MonitorError(f"Unsupported month count in date range: {value}")
        start, through = shift_months(today, -months), today
    elif match := re.fullmatch(r"(.+?)\s+to\s+(.+)", normalized):
        start, through = parse_calendar_date(match.group(1)), parse_calendar_date(match.group(2))
    else:
        raise MonitorError(
            "Unsupported date range. Try 'this year', 'this month', 'the last two months', "
            "or '1-1-2025 to 1-1-2026'."
        )

    if start > through:
        raise MonitorError(f"Date range starts after it ends: {value}")
    return start, through


def identity_for(guid: str, url: str) -> str:
    if guid:
        return guid.strip()
    if url:
        return url
    raise MonitorError("An RSS item has neither a GUID nor a URL.")


def api_identity(post_id: int) -> str:
    return f"https://devblogs.microsoft.com/cppblog/?p={post_id}"


def fetch_feed(feed_url: str, timeout: float) -> bytes:
    request = urllib.request.Request(
        feed_url,
        headers={"Accept": "application/rss+xml, application/xml;q=0.9", "User-Agent": USER_AGENT},
    )
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return response.read()
    except (urllib.error.URLError, TimeoutError, OSError) as error:
        raise MonitorError(f"Unable to fetch RSS feed: {error}") from error


def parse_feed(data: bytes) -> list[dict[str, Any]]:
    try:
        root = ET.fromstring(data)
    except ET.ParseError as error:
        raise MonitorError(f"The RSS feed isn't valid XML: {error}") from error

    posts: list[dict[str, Any]] = []
    for item in root.findall("./channel/item"):
        guid = element_text(item, "guid")
        url = canonicalize_url(element_text(item, "link"))
        description = element_text(item, "description")
        content = element_text(item, CONTENT_NAMESPACE)
        post = {
            "identity": identity_for(guid, url),
            "guid": guid,
            "url": url,
            "title": html.unescape(element_text(item, "title")),
            "author": html.unescape(element_text(item, CREATOR_NAMESPACE)),
            "published": normalize_date(element_text(item, "pubDate")),
            "categories": [html.unescape(category.text.strip()) for category in item.findall("category") if category.text],
            "excerpt": plain_text(description or content)[:1000],
        }
        posts.append(post)
    if not posts:
        raise MonitorError("The RSS feed contains no posts.")
    return posts


def api_post_to_record(item: dict[str, Any]) -> dict[str, Any]:
    post_id = int(item["id"])
    embedded = item.get("_embedded", {})
    authors = embedded.get("author", []) if isinstance(embedded, dict) else []
    author = authors[0].get("name", "") if authors and isinstance(authors[0], dict) else ""
    title = item.get("title", {})
    excerpt = item.get("excerpt", {})
    published = str(item.get("date_gmt") or item.get("date") or "")
    if published and not published.endswith("Z"):
        published += "Z"
    guid = api_identity(post_id)
    return {
        "identity": guid,
        "guid": guid,
        "url": canonicalize_url(str(item.get("link", ""))),
        "title": html.unescape(str(title.get("rendered", ""))),
        "author": html.unescape(str(author)),
        "published": published,
        "categories": [],
        "excerpt": plain_text(str(excerpt.get("rendered", "")))[:1000],
    }


def fetch_api_page(api_url: str, parameters: dict[str, str], timeout: float) -> tuple[Any, int]:
    url = f"{api_url}?{urllib.parse.urlencode(parameters)}"
    request = urllib.request.Request(
        url,
        headers={"Accept": "application/json", "User-Agent": USER_AGENT},
    )
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            data = json.loads(response.read().decode("utf-8"))
            total_pages = int(response.headers.get("X-WP-TotalPages", "1"))
            return data, total_pages
    except (urllib.error.URLError, TimeoutError, OSError, UnicodeDecodeError, json.JSONDecodeError) as error:
        raise MonitorError(f"Unable to fetch C++ Team Blog API: {error}") from error


def fetch_api_posts(api_url: str, start: date, through: date, timeout: float) -> list[dict[str, Any]]:
    parameters = {
        "after": f"{start - timedelta(days=1)}T23:59:59",
        "before": f"{through + timedelta(days=1)}T00:00:00",
        "per_page": "100",
        "page": "1",
        "orderby": "date",
        "order": "desc",
        "_embed": "author",
    }
    first_page, total_pages = fetch_api_page(api_url, parameters, timeout)
    if not isinstance(first_page, list):
        raise MonitorError("The C++ Team Blog API returned an unexpected response.")
    items = first_page
    for page in range(2, total_pages + 1):
        parameters["page"] = str(page)
        page_items, _ = fetch_api_page(api_url, parameters, timeout)
        if not isinstance(page_items, list):
            raise MonitorError("The C++ Team Blog API returned an unexpected response.")
        items.extend(page_items)
    return [api_post_to_record(item) for item in items]


def fetch_api_post(api_url: str, post_id: int, timeout: float) -> dict[str, Any]:
    item, _ = fetch_api_page(f"{api_url}/{post_id}", {"_embed": "author"}, timeout)
    if not isinstance(item, dict):
        raise MonitorError("The C++ Team Blog API returned an unexpected post response.")
    return api_post_to_record(item)


def empty_ledger() -> dict[str, Any]:
    return {"version": 1, "reported_posts": {}}


def load_ledger(path: Path) -> dict[str, Any]:
    if not path.exists():
        return empty_ledger()
    try:
        ledger = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise MonitorError(f"Unable to read ledger {path}: {error}") from error
    if ledger.get("version") != 1 or not isinstance(ledger.get("reported_posts"), dict):
        raise MonitorError(f"Unsupported or invalid ledger format: {path}")
    return ledger


def seen_values(ledger: dict[str, Any]) -> set[str]:
    values: set[str] = set()
    for identity, record in ledger["reported_posts"].items():
        values.add(identity)
        if isinstance(record, dict):
            guid = record.get("guid")
            url = canonicalize_url(str(record.get("url", "")))
            if guid:
                values.add(str(guid))
            if url:
                values.add(url)
    return values


def is_seen(post: dict[str, Any], seen: set[str]) -> bool:
    return any(value and value in seen for value in (post["identity"], post["guid"], post["url"]))


def write_ledger(path: Path, ledger: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(prefix=f".{path.name}.", suffix=".tmp", dir=path.parent)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as stream:
            json.dump(ledger, stream, indent=2, ensure_ascii=False, sort_keys=True)
            stream.write("\n")
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary_name, path)
    except Exception:
        try:
            os.unlink(temporary_name)
        except OSError:
            pass
        raise


def default_summary_path(today: date | None = None) -> Path:
    today = today or datetime.now().astimezone().date()
    filename = f"BlogSummaries-{today.month}-{today.day}-{today.year}.md"
    return Path(tempfile.gettempdir()) / filename


def write_text_atomic(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(prefix=f".{path.name}.", suffix=".tmp", dir=path.parent)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as stream:
            stream.write(content)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary_name, path)
    except Exception:
        try:
            os.unlink(temporary_name)
        except OSError:
            pass
        raise


def format_report_date(value: date) -> str:
    return f"{value.strftime('%B')} {value.day}, {value.year}"


def save_summary(args: argparse.Namespace) -> dict[str, Any]:
    summary = sys.stdin.read().strip()
    if not summary:
        raise MonitorError("Summary Markdown is empty. Pipe a completed summary to save-summary.")

    start = parse_calendar_date(args.date_from)
    through = parse_calendar_date(args.date_through)
    if start > through:
        raise MonitorError("Summary date range starts after it ends.")
    date_range_line = (
        f"**Date range:** {format_report_date(start)} through {format_report_date(through)}"
    )

    output = args.output or default_summary_path()
    marker = f"<!-- cpp-blog-post: {args.post_id} -->"
    existing = ""
    if output.exists():
        try:
            existing = output.read_text(encoding="utf-8")
        except OSError as error:
            raise MonitorError(f"Unable to read summary file {output}: {error}") from error
    if marker in existing:
        return {"status": "already_saved", "post_id": args.post_id, "output": str(output)}

    if existing:
        if date_range_line not in existing:
            generated_line = re.search(r"^\*\*Generated:\*\*.*$", existing, re.MULTILINE)
            if generated_line:
                insert_at = generated_line.end()
                existing = existing[:insert_at] + f"\n\n{date_range_line}" + existing[insert_at:]
        content = existing.rstrip() + f"\n\n{marker}\n\n{summary}\n"
    else:
        generated = format_report_date(datetime.now().astimezone().date())
        content = (
            f"# C++ Team Blog summaries\n\n**Generated:** {generated}\n\n"
            f"{date_range_line}\n\n{marker}\n\n{summary}\n"
        )
    try:
        write_text_atomic(output, content)
    except OSError as error:
        raise MonitorError(f"Unable to write summary file {output}: {error}") from error
    return {"status": "saved", "post_id": args.post_id, "output": str(output)}


def discover(args: argparse.Namespace) -> dict[str, Any]:
    start, through = parse_date_range(args.date_range)
    posts = fetch_api_posts(args.api_url, start, through, args.timeout)
    ledger = load_ledger(args.ledger)
    seen = seen_values(ledger)
    unseen = [post for post in posts if not is_seen(post, seen)]
    if args.limit is not None:
        unseen = unseen[: args.limit]
    return {
        "status": "ok",
        "source_url": args.api_url,
        "checked_at": utc_now(),
        "date_range": {"from": start.isoformat(), "through": through.isoformat()},
        "post_count": len(posts),
        "unseen_count": len(unseen),
        "unseen": unseen,
        "ledger": str(args.ledger),
    }


def record(args: argparse.Namespace) -> dict[str, Any]:
    posts = parse_feed(fetch_feed(args.feed_url, args.timeout))
    post = next(
        (
            candidate
            for candidate in posts
            if args.post_id in {candidate["identity"], candidate["guid"], candidate["url"]}
        ),
        None,
    )
    if post is None:
        parsed_id = urllib.parse.parse_qs(urllib.parse.urlsplit(args.post_id).query).get("p", [])
        if not parsed_id or not parsed_id[0].isdigit():
            raise MonitorError(f"Post ID isn't a recognized C++ Team Blog identity: {args.post_id}")
        post = fetch_api_post(args.api_url, int(parsed_id[0]), args.timeout)

    ledger = load_ledger(args.ledger)
    existing = is_seen(post, seen_values(ledger))
    ledger["reported_posts"][post["identity"]] = {
        "guid": post["guid"],
        "url": post["url"],
        "title": post["title"],
        "published": post["published"],
        "reported_at": utc_now(),
    }
    write_ledger(args.ledger, ledger)
    return {
        "status": "already_recorded" if existing else "recorded",
        "identity": post["identity"],
        "ledger": str(args.ledger),
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--feed-url", default=DEFAULT_FEED_URL)
    parser.add_argument("--api-url", default=DEFAULT_API_URL)
    parser.add_argument("--ledger", type=Path, default=DEFAULT_LEDGER)
    parser.add_argument("--timeout", type=float, default=20.0)
    subparsers = parser.add_subparsers(dest="command", required=True)

    check_parser = subparsers.add_parser("check", help="List posts absent from the ledger.")
    check_parser.add_argument(
        "--date-range",
        required=True,
        help="Examples: 'this year', 'the last two months', or '1-1-2025 to 1-1-2026'.",
    )
    check_parser.add_argument("--limit", type=int, default=None)
    check_parser.set_defaults(handler=discover)

    record_parser = subparsers.add_parser("record", help="Record one successfully reported post.")
    record_parser.add_argument("--post-id", required=True)
    record_parser.set_defaults(handler=record)

    save_parser = subparsers.add_parser(
        "save-summary", help="Append one Markdown summary to today's temp-directory report."
    )
    save_parser.add_argument("--post-id", required=True)
    save_parser.add_argument("--date-from", required=True, help="Resolved inclusive start date.")
    save_parser.add_argument("--date-through", required=True, help="Resolved inclusive end date.")
    save_parser.add_argument("--output", type=Path, default=None, help=argparse.SUPPRESS)
    save_parser.set_defaults(handler=save_summary)
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    if getattr(args, "limit", None) is not None and args.limit < 1:
        parser.error("--limit must be at least 1")
    try:
        result = args.handler(args)
    except MonitorError as error:
        json.dump({"status": "error", "error": str(error)}, sys.stderr, ensure_ascii=False)
        sys.stderr.write("\n")
        return 1
    json.dump(result, sys.stdout, indent=2, ensure_ascii=False)
    sys.stdout.write("\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())