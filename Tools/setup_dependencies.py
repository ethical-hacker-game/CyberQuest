"""Prepare a separate local Unity project from licensed, hash-qualified inputs.

No downloads, package purchases, Unity execution or changes to this source tree.
Default/--check/--preview are read-only. --apply requires a separate --output.
"""

import argparse
from collections import Counter
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import sys
import tempfile

sys.dont_write_bytecode = True
SOURCE = Path(__file__).resolve().parents[1]
MANIFEST = Path(__file__).with_name("dependency_manifest.json")
MARKER = ".cyberquest-local-project.json"


def sha(payload):
    return hashlib.sha256(payload).hexdigest()


def normalize(payload):
    return payload.decode("utf-8-sig").replace("\r\n", "\n")


def matches(payload, expected):
    try:
        mode = expected["hash_mode"]
        if mode == "utf8-lf":
            value = normalize(payload).encode()
        elif mode == "text-bytes-lf":
            value = payload.replace(b"\r\n", b"\n")
        elif mode == "bytes":
            value = payload
        else:
            raise ValueError("Unsupported hash mode")
        return sha(value) == expected["hash"]
    except UnicodeError:
        return False


def checked_path(root, relative):
    parts = PurePosixPath(relative)
    if not relative or parts.is_absolute() or any(p in {"..", ".git"} for p in parts.parts) or "\\" in relative or ":" in relative:
        raise ValueError(f"Unsafe manifest path: {relative}")
    path = root.joinpath(*parts.parts)
    if not path.resolve().is_relative_to(root.resolve()):
        raise ValueError(f"Path escapes its root: {relative}")
    current = path
    while current != root.parent:
        if current.is_symlink() or (current.exists() and getattr(current.lstat(), "st_file_attributes", 0) & 1024):
            raise ValueError(f"Links/reparse points are unsupported: {relative}")
        if current == root:
            break
        current = current.parent
    return path


def input_index(root):
    result = {}
    for base, directories, files in os.walk(root, followlinks=False):
        directories[:] = [name for name in directories if name not in {".git", "Library", "Temp", "Logs", "UserSettings", "_authoring", "_Templates"}]
        for name in directories:
            checked_path(root, (Path(base) / name).relative_to(root).as_posix())
        for name in files:
            if name.endswith(".meta"):
                path = checked_path(root, (Path(base) / name).relative_to(root).as_posix())
                text = path.read_text(encoding="utf-8-sig")
                match = re.search(r"(?m)^guid: ([a-f0-9]{32})", text)
                if match:
                    result.setdefault(match[1], []).append(path)
    return result


def patch_payload(payload, recipe):
    text = normalize(payload)
    previous = len(text)
    for edit in sorted(recipe["edits"], key=lambda item: (item["start"], item["end"]), reverse=True):
        start, end = edit["start"], edit["end"]
        if not 0 <= start <= end <= previous:
            raise ValueError("Overlapping or invalid patch positions")
        text = text[:start] + edit["text"] + text[end:]
        previous = start
    # Preserve the input encoding/BOM and newline convention.
    if b"\r\n" in payload:
        text = text.replace("\n", "\r\n")
    result = (b"\xef\xbb\xbf" if payload.startswith(b"\xef\xbb\xbf") else b"") + text.encode("utf-8")
    if not matches(result, recipe["expected"]):
        raise ValueError("Patched output does not match its supported hash")
    return result


def select_input(root, index, recipe):
    candidates = [checked_path(root, name) for name in recipe["input_paths"]]
    if recipe.get("guid"):
        for meta in index.get(recipe["guid"], []):
            candidates.append(meta if recipe["target"].endswith(".meta") else Path(str(meta)[:-5]))
    for path in dict.fromkeys(candidates):
        if not path.is_file():
            continue
        path = checked_path(root, path.relative_to(root).as_posix())
        payload = path.read_bytes()
        if matches(payload, recipe["expected"]) or any(matches(payload, item) for item in recipe.get("equivalent_inputs", [])):
            return path, payload, "matching", sha(payload)
        if recipe["mode"] == "patch" and matches(payload, recipe["before"]):
            return path, patch_payload(payload, recipe), "patch", sha(payload)
    return None


def blocks(text):
    parts = re.split(r"(?=^--- !u!)", text, flags=re.M)
    result = {}
    for block in parts[1:]:
        match = re.match(r"--- !u!(\d+) &(-?\d+)", block)
        if not match or match[2] in result:
            raise ValueError("Unsupported Unity scene document layout")
        result[match[2]] = (int(match[1]), block)
    return parts[0], result


def fields(block):
    return {match[1]: match[0] for match in re.finditer(r"^  (\w+):.*?(?=^  \w+:|\Z)", block, re.M | re.S)}


def restore_scene(public_payload, donor_payload, spec):
    if sha(normalize(public_payload).encode()) != spec["public_sha256"] or sha(normalize(donor_payload).encode()) != spec["donor_sha256"]:
        raise ValueError("Scene/donor identity differs from the supported snapshot")
    header, public = blocks(normalize(public_payload))
    _, donor = blocks(normalize(donor_payload))
    vendor_ids = set(spec["vendor_local_ids"])
    outputs = []
    for ident, (kind, block) in public.items():
        if ident not in donor:
            raise ValueError("Public scene member is absent from the supported donor")
        for name, value in fields(donor[ident][1]).items():
            local_refs = set(re.findall(r"\{fileID: (-?\d+)\}", value))
            geography = name == "provinceFrontiersPoints" and ident in spec["geography_component_ids"]
            if local_refs & vendor_ids or geography:
                existing = fields(block).get(name)
                if existing is None:
                    raise ValueError("Expected vendor field is absent from the public scene")
                block = block.replace(existing, value, 1)
        outputs.append(block)
    for ident in spec["vendor_local_ids"]:
        kind, block = donor[ident]
        if kind not in {21, 43} or ident in public:
            raise ValueError("Unsupported donor resource type or existing local ID")
        outputs.append(block)
    return (header + "".join(outputs)).encode("utf-8")


def public_files(manifest):
    roots = [root for package in manifest["packages"] for root in package["target_roots"]]
    allowed = {"Assets", "Packages", "ProjectSettings", "Tools", "Licenses"}
    root_files = {"README.md", "DEPENDENCIES.md", "THIRD_PARTY_NOTICES.md", "LICENSE", "LICENSE.md", ".gitignore", ".gitattributes", ".editorconfig"}
    for base, directories, files in os.walk(SOURCE, followlinks=False):
        directories[:] = [name for name in directories if name not in {".git", "Library", "Temp", "Logs", "UserSettings", "_authoring", "_Templates", "__pycache__"}]
        for name in files:
            path = Path(base) / name
            relative = path.relative_to(SOURCE).as_posix()
            if relative.split("/")[0] not in allowed and relative not in root_files:
                continue
            if any(relative == root + ".meta" or relative.startswith(root + "/") for root in roots):
                continue
            yield relative, checked_path(SOURCE, relative)


def preflight(manifest, inputs, scene_mode, verbose=False):
    index = input_index(inputs)
    plan = {}
    results = {}
    for package in manifest["packages"]:
        results[package["id"]] = Counter()
    for recipe in manifest["files"]:
        selected = select_input(inputs, index, recipe)
        if selected is None:
            results[recipe["package"]]["missing_or_unsupported"] += 1
            print(f"[{recipe['package']}] missing/unsupported: {recipe['target']} ({recipe['mode']})", file=sys.stderr)
        else:
            path, payload, status, source_hash = selected
            # Keep bytes only for transformed text; direct copies are read again with a hash guard.
            plan[recipe["target"]] = {"source": path, "payload": payload if status == "patch" else None,
                                      "source_sha256": source_hash, "output_sha256": sha(payload)}
            results[recipe["package"]][status] += 1
            if verbose and status == "patch":
                print(f"[{recipe['package']}] planned patch: {recipe['target']}")
    for addition in manifest["owner_additions"] + manifest["local_editor_additions"]:
        path = checked_path(MANIFEST.parent, addition["payload"])
        payload = path.read_bytes()
        if sha(normalize(payload).encode()) != addition["sha256"]:
            raise ValueError("Owner addition payload differs from its manifest")
        allowed = {addition["sha256"], addition.get("accepted_existing_sha256")}
        for root in (SOURCE, inputs):
            existing = checked_path(root, addition["target"])
            if existing.is_file():
                data = existing.read_bytes()
                if sha(data) not in allowed and sha(normalize(data).encode()) not in allowed:
                    raise FileExistsError(f"Conflicting owner addition: {addition['target']}")
        plan[addition["target"]] = {"source": path, "payload": None,
                                    "source_sha256": sha(payload), "output_sha256": sha(payload)}
    for spec in manifest["scene_restoration"]:
        public = checked_path(SOURCE, spec["target"])
        if sha(normalize(public.read_bytes()).encode()) != spec["public_sha256"]:
            raise ValueError("Public scene differs from the supported setup snapshot")
        if scene_mode == "regenerate":
            results.setdefault("scene-resources", Counter())["local_editor_regeneration_required"] += 1
            if verbose:
                print(f"[scene-resources] regenerate locally: {spec['target']}")
            continue
        donor = checked_path(inputs, spec["target"])
        if not donor.is_file() or sha(normalize(donor.read_bytes()).encode()) != spec["donor_sha256"]:
            results.setdefault("scene-resources", Counter())["missing_or_unsupported"] += 1
            print(f"[scene-resources] matching licensed donor required: {spec['target']}", file=sys.stderr)
            continue
        payload = restore_scene(checked_path(SOURCE, spec["target"]).read_bytes(), donor.read_bytes(), spec)
        plan[spec["target"]] = {"source": donor, "payload": payload,
                                "source_sha256": sha(donor.read_bytes()), "output_sha256": sha(payload)}
        results.setdefault("scene-resources", Counter())["restore"] += 1
    for key, counts in results.items():
        print(f"{key}: " + ", ".join(f"{name}={count}" for name, count in sorted(counts.items())))
    return plan, any(item.get("missing_or_unsupported", 0) for item in results.values())


def apply(plan, base_files, output, inputs):
    output = output.absolute()
    if (output.resolve().is_relative_to(SOURCE.resolve()) or SOURCE.resolve().is_relative_to(output.resolve())
            or output.resolve().is_relative_to(inputs.resolve()) or inputs.resolve().is_relative_to(output.resolve())):
        raise ValueError("Output must be separate from both the public source and licensed inputs")
    manifest_hash = sha(MANIFEST.read_bytes())
    if output.exists():
        marker = checked_path(output, MARKER)
        if not marker.is_file() or json.loads(marker.read_text())["manifest_sha256"] != manifest_hash:
            raise FileExistsError("Output exists and is not this manifest's local project; refusing to overwrite")
        for name, entry in plan.items():
            path = checked_path(output, name)
            if not path.is_file() or sha(path.read_bytes()) != entry["output_sha256"]:
                raise FileExistsError(f"Local dependency or restored scene was modified: {name}; no files changed")
        print("Already prepared; no files changed.")
        return
    # Preflight is complete before any directory/file write.
    checked_path(output.parent, output.name)
    output.parent.mkdir(parents=True, exist_ok=True)
    staging = Path(tempfile.mkdtemp(prefix=".cyberquest-setup-", dir=output.parent))
    try:
        for name, entry in base_files.items():
            payload = entry["source"].read_bytes()
            if sha(payload) != entry["sha256"]:
                raise ValueError("Public source changed after preflight")
            target = checked_path(staging, name)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(payload)
        for name, entry in plan.items():
            payload = entry["source"].read_bytes()
            if sha(payload) != entry["source_sha256"]:
                raise ValueError("Licensed input changed after preflight")
            if entry["payload"] is not None:
                payload = entry["payload"]
            target = checked_path(staging, name)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(payload)
        (staging / MARKER).write_text(json.dumps({"manifest_sha256": manifest_hash,
            "purpose": "Local licensed working copy; contains no public Git history; do not redistribute vendor source or regenerated scenes."}, indent=2) + "\n")
        if output.exists():
            raise FileExistsError("Output appeared during preparation; refusing to replace it")
        staging.rename(output)
    except BaseException:
        # Keep the partial sibling for inspection; never delete an existing output or user tree.
        print(f"Preparation failed; partial local files retained at {staging}", file=sys.stderr)
        raise
    print(f"Prepared local project: {output}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inputs", type=Path, required=True, help="Licensed Unity input project containing matching assets/resources and scene donors")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="Read-only preflight (also the default)")
    mode.add_argument("--preview", action="store_true", help="Read-only preflight with package/path results")
    mode.add_argument("--apply", action="store_true", help="Create a separate local working project after complete preflight")
    parser.add_argument("--output", type=Path, help="New local project directory; required with --apply")
    parser.add_argument("--scene-mode", choices=("regenerate", "donor"), default="regenerate",
                        help="Regenerate maps in the local editor (default), or restore from exact matching licensed scene donors")
    arguments = parser.parse_args()
    if arguments.apply and arguments.output is None:
        parser.error("--apply requires --output")
    if not arguments.inputs.is_dir():
        parser.error("--inputs must be an existing licensed input project")
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    if manifest["schema_version"] != 1:
        raise ValueError("Unsupported dependency manifest schema")
    base_files = {}
    for name, path in public_files(manifest):
        payload = path.read_bytes()
        if payload.startswith(b"version https://git-lfs.github.com/spec/v1"):
            raise ValueError(f"Public source contains an unhydrated pointer: {name}")
        base_files[name] = {"source": path, "sha256": sha(payload)}
    plan, failed = preflight(manifest, arguments.inputs.absolute(), arguments.scene_mode, arguments.preview)
    if failed:
        print("Preflight incomplete. No files were written. Supply matching inputs or recreate the listed resources.", file=sys.stderr)
        return 1
    if arguments.apply:
        apply(plan, base_files, arguments.output, arguments.inputs.absolute())
    else:
        print("Preflight complete. Read-only; no files were written.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, KeyError, UnicodeError) as exception:
        print(f"Setup refused: {exception}", file=sys.stderr)
        raise SystemExit(2)
