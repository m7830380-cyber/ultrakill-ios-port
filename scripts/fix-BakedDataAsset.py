#!/usr/bin/env python3
"""Rewrite AssetRipper hex-blob lists in BakedData_*.asset into Unity YAML arrays."""
from __future__ import annotations

import argparse
import re
import struct
from pathlib import Path


def parse_hex_field(text: str, field: str) -> bytes | None:
    m = re.search(rf"(?m)^[ \t]*{re.escape(field)}:\s*([0-9a-fA-F]+)\s*$", text)
    if not m:
        return None
    return bytes.fromhex(m.group(1))


def yaml_int_list(name: str, values: list[int]) -> str:
    lines = [f"  {name}:"]
    for v in values:
        lines.append(f"  - {int(v)}")
    return "\n".join(lines)


def repair(path: Path) -> bool:
    text = path.read_text(encoding="utf-8", errors="replace")
    if "mrMeshIndices:\n  -" in text or re.search(r"(?m)^  mrMeshIndices:\n  - ", text):
        print(f"already repaired: {path}")
        return False

    patches: list[tuple[str, str]] = []
    for field, fmt, size in (
        ("backingMeshHashes", "<i", 4),
        ("mrLightIndices", "<i", 4),
        ("mrMeshIndices", "<H", 2),
        ("firstSubMesh", "<H", 2),
    ):
        raw = parse_hex_field(text, field)
        if raw is None:
            print(f"skip {field}: not a hex blob")
            continue
        if len(raw) % size != 0:
            raise SystemExit(f"{field}: bad length {len(raw)}")
        count = len(raw) // size
        values = list(struct.unpack("<" + fmt[-1] * count, raw))
        block = yaml_int_list(field, values)
        text2, n = re.subn(
            rf"(?m)^[ \t]*{re.escape(field)}:\s*[0-9a-fA-F]+\s*$",
            block,
            text,
            count=1,
        )
        if n != 1:
            raise SystemExit(f"failed to rewrite {field}")
        text = text2
        print(f"repaired {field}: {count} values")
        patches.append((field, str(count)))

    if not patches:
        print(f"nothing to repair: {path}")
        return False

    path.write_text(text, encoding="utf-8")
    print(f"wrote {path}")
    return True


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "root",
        nargs="?",
        default=r"C:\Users\v0id\ultrakill-export\ExportedProject\Assets\MonoBehaviour",
    )
    args = ap.parse_args()
    root = Path(args.root)
    files = sorted(root.glob("BakedData*.asset"))
    if not files:
        raise SystemExit(f"no BakedData*.asset under {root}")
    any_fixed = False
    for f in files:
        any_fixed = repair(f) or any_fixed
    raise SystemExit(0 if any_fixed or files else 1)


if __name__ == "__main__":
    main()
