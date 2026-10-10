#!/usr/bin/env python3
"""Export repaired BakedData_0.asset index lists to JSON (no ScriptableObject / Missing Script)."""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


def parse_int_list(text: str, field: str) -> list[int]:
    # Do NOT use DOTALL — '.' must not cross newlines or lists merge into one blob.
    m = re.search(
        rf"(?m)^[ \t]*{re.escape(field)}:\s*\n((?:[ \t]*- .+\n)*(?:[ \t]*- .+)\n?)",
        text,
    )
    if not m:
        raise SystemExit(f"missing YAML list for {field}")
    out: list[int] = []
    for line in m.group(1).splitlines():
        line = line.strip()
        if line.startswith("- "):
            out.append(int(line[2:].strip()))
    return out


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "--asset",
        default=r"C:\Users\v0id\ultrakill-export\ExportedProject\Assets\MonoBehaviour\BakedData_0.asset",
    )
    ap.add_argument(
        "--out",
        default=r"C:\Users\v0id\ultrakill-export\ExportedProject\Assets\IosBundleTools\tutorial_bake.json.txt",
    )
    args = ap.parse_args()
    text = Path(args.asset).read_text(encoding="utf-8", errors="replace")
    payload = {
        "meshNameContains": "StaticSceneOptimizer",
        "mainAtlasName": "Texture2D_2",
        "blendAtlasName": "Texture2D_1",
        "backingMeshHashes": parse_int_list(text, "backingMeshHashes"),
        "mrLightIndices": parse_int_list(text, "mrLightIndices"),
        "mrMeshIndices": parse_int_list(text, "mrMeshIndices"),
        "firstSubMesh": parse_int_list(text, "firstSubMesh"),
    }
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(payload, separators=(",", ":")), encoding="utf-8")
    print(
        f"wrote {out} firstSubMesh={len(payload['firstSubMesh'])} "
        f"mrMeshIndices={len(payload['mrMeshIndices'])} lights={len(payload['mrLightIndices'])}"
    )


if __name__ == "__main__":
    main()
