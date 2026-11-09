#!/usr/bin/env python3
"""Fails when two projects carry the same file. CI runs it on every push and pull request; run it before you push:

    python3 eng/check-duplicates.py

What the district and the maze share lives once, in OpenWorker.Server.Common, and code that two servers need is moved down
to a project both reference instead of being copied. The check looks at every file of every project of the repository
(a folder named OpenWorker.*) but the test projects (*.Test), obj/ and bin/, and reports each group of files from different
projects that are the same. Two C# files are the same when they differ only in their using and namespace directives, the
byte order mark and white space: a copy whose namespace was changed is still a copy.

A file that is meant to be the same in two projects goes to EXEMPT with its reason: "path<TAB>why", the path relative to the
repository root. There is none today: every Program.cs, appsettings.json and Dockerfile of the servers names its own server.
"""

import hashlib
import re
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

SKIPPED_FOLDERS = {"obj", "bin"}

EXEMPT = {
    # "OpenWorker.X/File.cs": "why",
}

DIRECTIVE = re.compile(r"^\s*(global\s+)?(using\s+[\w.=\s]+|namespace\s+[\w.]+)\s*;\s*$", re.MULTILINE)
SPACE = re.compile(r"\s+")


def projects():
    for folder in sorted(ROOT.glob("OpenWorker.*")):
        if folder.is_dir() and not folder.name.endswith(".Test"):
            yield folder


def files(project):
    for path in sorted(project.rglob("*")):
        relative = path.relative_to(project)

        if path.is_file() and not SKIPPED_FOLDERS.intersection(relative.parts[:-1]):
            yield path


def fingerprint(path):
    text = path.read_bytes().decode("utf-8", errors="replace").lstrip("﻿")

    if path.suffix == ".cs":
        text = DIRECTIVE.sub("", text)

    return hashlib.sha256(SPACE.sub(" ", text).strip().encode("utf-8")).hexdigest()


def main():
    groups = defaultdict(list)

    for project in projects():
        for path in files(project):
            name = path.relative_to(ROOT).as_posix()

            if name not in EXEMPT:
                groups[fingerprint(path)].append((project.name, name))

    duplicates = [sorted(name for _, name in group) for group in groups.values() if len({p for p, _ in group}) > 1]

    for group in sorted(duplicates):
        print("Same file in more than one project:")

        for name in group:
            print(f"  {name}")

        print()

    if duplicates:
        print("Move what the projects share to a project they both reference (OpenWorker.Server.Common for the district and the")
        print("maze), or, if the files are meant to be the same, add them to EXEMPT in eng/check-duplicates.py with the reason.")
        return 1

    print("No file is repeated across projects.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
