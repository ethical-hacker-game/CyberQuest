"""Collect actual installed dependency notices into a marked local working project.

Read-only unless --apply is supplied. No network requests or Unity execution.
"""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import tempfile

sys.dont_write_bytecode = True
from setup_dependencies import checked_path, MARKER


def sha(data):
    return hashlib.sha256(data).hexdigest()


def legal_files(root):
    for base, directories, files in os.walk(root, followlinks=False):
        directories[:] = [name for name in directories if name not in {".git", "__pycache__"}]
        for name in files:
            path = Path(base) / name
            if path.suffix == ".meta":
                continue
            if re.search(r"licen[cs]e|notice|copyright|copying|\bofl\b", name, re.I) or any(re.search(r"^licenses?$", part, re.I) for part in path.relative_to(root).parts[:-1]):
                yield path.relative_to(root).as_posix(), checked_path(root, path.relative_to(root).as_posix())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True, help="Local project created by setup_dependencies.py")
    parser.add_argument("--package-cache", type=Path, help="Unity PackageCache location; defaults to the local project's Library/PackageCache")
    parser.add_argument("--apply", action="store_true", help="Copy notices after complete input inspection")
    arguments = parser.parse_args()
    project = arguments.project.absolute()
    if not checked_path(project, MARKER).is_file():
        raise ValueError("Notice collection requires a marked local project, not the public source tree")
    cache = (arguments.package_cache or project / "Library/PackageCache").absolute()
    manifest = json.loads(checked_path(project, "Tools/dependency_manifest.json").read_text(encoding="utf-8"))
    lock = json.loads(checked_path(project, "Packages/packages-lock.json").read_text(encoding="utf-8"))
    required = {name: value["version"] for name, value in lock["dependencies"].items()
                if value["source"] == "registry"}
    installed = {}
    if cache.is_dir():
        for directory in cache.iterdir():
            package = checked_path(cache, directory.name + "/package.json")
            if package.is_file():
                info = json.loads(package.read_text(encoding="utf-8"))
                if info.get("name") in required and info.get("version") == required[info["name"]]:
                    if info["name"] in installed:
                        raise ValueError("Duplicate matching package cache entries; select an unambiguous cache")
                    installed[info["name"]] = directory
    missing = required.keys() - installed.keys()
    if missing:
        raise ValueError("Matching registry packages are not installed: " + ", ".join(sorted(missing)))
    sources = {name: [path] for name, path in installed.items()}
    sources["unity-web-browser"] = [checked_path(project, "Packages/dev.voltstro.unitywebbrowser")]
    for package in manifest["packages"]:
        sources[package["id"]] = [checked_path(project, name) for name in package["target_roots"] if checked_path(project, name).is_dir()]
    payloads = {}
    counts = {}
    for name, directories in sources.items():
        count = 0
        for number, root in enumerate(directories):
            for relative, path in legal_files(root):
                payloads[f"{name}/{number}/{relative}"] = path.read_bytes()
                count += 1
        if count == 0:
            # Absence of a standalone license file is explicit, not silently treated as clearance.
            payloads[f"{name}/NOTICE-REQUIRED.txt"] = (
                "No standalone notice file was found in the selected local dependency files. "
                "Obtain the applicable distribution terms and required third-party notices from the publisher "
                "before distributing a build. This marker does not grant permission.\n").encode()
        counts[name] = count
    index = "# Installed dependency notice inventory\n\n"
    index += "Preserve this directory with distributed builds. Publisher terms remain applicable.\n\n"
    index += "\n".join(f"- {name}: {count} notice files" for name, count in sorted(counts.items())) + "\n"
    payloads["NOTICE-INDEX.md"] = index.encode()
    destination = checked_path(project, "Assets/StreamingAssets/DependencyNotices")
    print(json.dumps({"notice_files": len(payloads), "packages": counts, "apply": arguments.apply}, indent=2))
    if not arguments.apply:
        print("Read-only inventory; no files written.")
        return 0
    if destination.exists():
        actual = {path.relative_to(destination).as_posix(): sha(path.read_bytes())
                  for path in destination.rglob("*") if path.is_file() and path.suffix != ".meta"}
        if actual != {name: sha(payload) for name, payload in payloads.items()}:
            raise FileExistsError("Notice output already exists with different contents; review it before regeneration")
        print("Notices already current; no files changed.")
        return 0
    destination.parent.mkdir(parents=True, exist_ok=True)
    staging = Path(tempfile.mkdtemp(prefix=".dependency-notices-", dir=destination.parent))
    try:
        for name, payload in payloads.items():
            target = checked_path(staging, name)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(payload)
        if destination.exists():
            raise FileExistsError("Notice destination appeared during collection")
        staging.rename(destination)
    except BaseException:
        print(f"Partial notices retained for inspection: {staging}", file=sys.stderr)
        raise
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, KeyError, UnicodeError) as exception:
        print(f"Notice collection refused: {exception}", file=sys.stderr)
        raise SystemExit(2)
