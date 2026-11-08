#!/usr/bin/env python3
"""Reads the TRX files of a test run, reports the outcome and fails when the run is not clean.

    eng/check-trx.py [--allow-skip REGEX]... PATH...

PATH is a .trx file or a folder searched for *.trx. The script exits with 1 when

  * no test result is found (the tests did not run at all),
  * a test failed, errored, timed out or was aborted,
  * a test was skipped for a reason that no --allow-skip pattern matches.

The skipped tests are grouped by their reason, so a run that silently stops running a whole family of tests
(Redis was not reachable, a variable was not set) shows up as a new group instead of a green tick.
In GitHub Actions the report is also written to the job summary.
"""

import argparse
import os
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

FAILED = {"Failed", "Error", "Timeout", "Aborted", "Disconnected", "NotRunnable"}
SKIPPED = {"NotExecuted", "Inconclusive"}

# The tests call Assert.Inconclusive("OPENWORKER_TEST_X is not set."); the class initializers wrap that message.
REASON = re.compile(r"(OPENWORKER_TEST_[A-Z_]+) is not set")


def find_trx(paths):
    for path in paths:
        path = Path(path)

        if path.is_dir():
            yield from sorted(path.rglob("*.trx"))
        else:
            yield path


def reason_of(result):
    message = result.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=NS).strip()

    if match := REASON.search(message):
        return f"{match.group(1)} is not set"

    return message.splitlines()[0] if message else "skipped without a message"


def read(trx):
    root = ET.parse(trx).getroot()

    classes = {}

    for unit in root.iterfind("t:TestDefinitions/t:UnitTest", NS):
        method = unit.find("t:TestMethod", NS)

        if method is not None:
            classes[unit.get("id")] = method.get("className", "").rsplit(".", 1)[-1]

    for result in root.iterfind("t:Results/t:UnitTestResult", NS):
        name = result.get("testName", "?")
        owner = classes.get(result.get("testId"), "?")

        yield result.get("outcome", "?"), f"{owner}.{name}", result


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("paths", nargs="+", metavar="PATH")
    parser.add_argument("--allow-skip", action="append", default=[], metavar="REGEX",
                        help="a skip reason that does not fail the run (matched with re.search)")
    args = parser.parse_args()

    allowed = [re.compile(pattern) for pattern in args.allow_skip]
    files = list(find_trx(args.paths))

    passed = 0
    failed = []
    skipped = defaultdict(list)

    for trx in files:
        for outcome, name, result in read(trx):
            if outcome in FAILED:
                failed.append((outcome, name, result.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=NS).strip()))
            elif outcome in SKIPPED:
                skipped[reason_of(result)].append(name)
            else:
                passed += 1

    skipped_total = sum(len(names) for names in skipped.values())
    forbidden = {reason: names for reason, names in skipped.items() if not any(p.search(reason) for p in allowed)}

    lines = [
        f"TRX files: {len(files)}",
        f"passed: {passed}, failed: {len(failed)}, skipped: {skipped_total}",
    ]

    for outcome, name, message in failed:
        lines.append(f"  {outcome}: {name}: {message.splitlines()[0] if message else ''}")

    for reason, names in sorted(skipped.items()):
        mark = "" if reason not in forbidden else "  <-- not allowed"
        lines.append(f"skipped, {reason}: {len(names)}{mark}")

        for name in sorted(names):
            lines.append(f"  {name}")

    print("\n".join(lines))

    summary = os.environ.get("GITHUB_STEP_SUMMARY")

    if summary:
        with open(summary, "a", encoding="utf-8") as out:
            out.write("### Tests\n\n")
            out.write(f"| passed | failed | skipped |\n|---|---|---|\n| {passed} | {len(failed)} | {skipped_total} |\n\n")

            if skipped:
                out.write("| Skipped, reason | Tests |\n|---|---|\n")

                for reason, names in sorted(skipped.items()):
                    out.write(f"| {reason} | {len(names)} |\n")

                out.write("\n")

            for outcome, name, message in failed:
                out.write(f"- **{outcome}** `{name}`\n")

    problems = []

    if not files or passed + len(failed) + skipped_total == 0:
        problems.append("no test result found: the tests did not run")

    if failed:
        problems.append(f"{len(failed)} test(s) failed")

    if forbidden:
        problems.append("tests skipped for a reason that is not allowed: " + ", ".join(sorted(forbidden)))

    for problem in problems:
        print(f"::error::{problem}" if os.environ.get("GITHUB_ACTIONS") else f"error: {problem}", file=sys.stderr)

    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
