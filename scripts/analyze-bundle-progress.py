"""Estimate iOS bundle build progress from Unity log + catalog-map."""
import json
import os
import re
import sys
from collections import Counter
from datetime import datetime

LOG = r"C:\Users\v0id\ultrakill-ios-port\artifacts\ios-bundles.log"
MAP = r"C:\Users\v0id\ultrakill-ios-port\artifacts\catalog-map.json"
EXPORT_ASSETS = r"C:\Users\v0id\ultrakill-export\ExportedProject\Assets"


def main():
    with open(MAP, encoding="utf-8") as f:
        cat = json.load(f)

    scene_bundles = [
        b
        for b in cat["bundles"]
        if any(a.get("type", "").endswith("SceneInstance") for a in b.get("assets", []))
    ]
    scene_rows = sum(
        len([a for a in b.get("assets", []) if a.get("type", "").endswith("SceneInstance")])
        for b in scene_bundles
    )

    with open(LOG, "rb") as f:
        data = f.read()
    text = data.decode("utf-8", errors="replace")
    lines = text.splitlines()

    matched_i = next((i for i, l in enumerate(lines) if "[IosBundles] matched" in l), None)
    sbp_i = next((i for i, l in enumerate(lines) if "CalculateSceneDependencyData:Run" in l), None)
    build_result = any("[IosBundles] build result" in l for l in lines)

    print("=== Catalog work units ===")
    print(f"bundles_total={len(cat['bundles'])} scene_bundles={len(scene_bundles)} scene_rows={scene_rows} scenes_table={len(cat.get('scenes', []))}")

    print("\n=== Log ===")
    print(f"log_lines={len(lines)} log_mb={len(data) / 1024 / 1024:.2f} build_result={build_result}")
    if matched_i is not None:
        print(f"matched_line={matched_i + 1}: {lines[matched_i].strip()}")

    sbp_start = sbp_i if sbp_i is not None else (matched_i + 1 if matched_i is not None else 0)
    post = lines[sbp_start:]

    open_re = re.compile(r"Opening scene 'Assets/([^']+)\.unity'")
    opens = []
    for line in post:
        m = open_re.search(line)
        if m:
            opens.append(m.group(1))

    dep_runs = sum(1 for line in post if "CalculateSceneDependencyData:Run" in line)
    counts = Counter(opens)

    print("\n=== Since SBP scene-dependency phase ===")
    print(f"sbp_start_line={sbp_start + 1}")
    print(f"opening_scene_lines={len(opens)} unique_scenes_touched={len(counts)}")
    print(f"CalculateSceneDependencyData_Run_log_lines={dep_runs}")

    for pat in [
        "WriteSerializedFiles",
        "ArchiveAndCompressBundles",
        "GenerateBundleCommands",
        "CreateBuiltInShadersBundle",
        "CreateMonoScriptBundle",
        "PostDependencyCallback",
    ]:
        n = sum(1 for line in post if pat in line)
        if n:
            print(f"  {pat}={n}")

    scene_files = []
    for root, _, files in os.walk(EXPORT_ASSETS):
        for fn in files:
            if fn.endswith(".unity"):
                scene_files.append(fn[:-6])
    scene_set = set(scene_files)

    opened = scene_set & set(counts.keys())
    never = sorted(scene_set - set(counts.keys()))
    print(f"\nexport_scenes={len(scene_files)} opened_at_least_once={len(opened)} never_opened={len(never)}")

    if counts:
        vals = list(counts.values())
        print(f"opens_per_scene: min={min(vals)} max={max(vals)} avg={sum(vals)/len(vals):.1f}")

    # Scene bundles in build: each catalog scene row should map to one exported scene path via internalId filename
    # IosRetailBundleBuild uses scene.internalId as scene file name without extension
    catalog_scene_names = {s["internalId"] for s in cat.get("scenes", [])}
    export_by_name = {g: g for g in scene_files}  # guid names in rip
    # matched scenes use FindAssets by internalId (level name) - log uses guid paths
    # Progress proxy: fraction of export scenes opened, weighted by open count vs max seen

    # Estimate: SBP scene dep often logs one CalculateSceneDependencyData:Run per open batch
    # Use dep_runs as completed units vs expected dep_runs ~ opening_scene_lines (1:1 near start)
    # After scene phase: WriteSerializedFiles etc.

    if dep_runs > 0 and len(opens) > 0:
        # Empirical from this project: each scene bundle scene gets many opens (dependency passes)
        # Use never_opened scenes as remaining scene work lower bound
        if never:
            print(f"\nRemaining scenes (never opened in SBP phase): {len(never)}")
            print("  examples:", ", ".join(never[:8]) + ("..." if len(never) > 8 else ""))

    # Log growth rate (last 5 minutes of file mtime samples via size)
    log_path = LOG
    st = os.stat(log_path)
    print(f"\nlog_mtime={datetime.fromtimestamp(st.st_mtime).isoformat(sep=' ', timespec='seconds')}")

    # Unity process runtime from WMI would be external; print bundle output
    ios_dir = r"C:\Users\v0id\ultrakill-ios-port\artifacts\ios-content\ULTRAKILL_Data\StreamingAssets\aa\iOS"
    bundle_count = 0
    if os.path.isdir(ios_dir):
        for _, _, files in os.walk(ios_dir):
            bundle_count += sum(1 for f in files if f.endswith(".bundle"))
    print(f"ios_bundle_files={bundle_count}")

    # ETA model: if WriteSerializedFiles not started, remaining ~ scene opens for never + re-opens for in-progress
    write_ser = sum(1 for line in post if "WriteSerializedFiles" in line)
    archive = sum(1 for line in post if "ArchiveAndCompressBundles" in line)
    if build_result:
        print("\nETA: DONE (build result in log)")
    elif bundle_count >= 79:
        print("\nETA: DONE (bundles on disk)")
    elif archive > 0 or write_ser > 100:
        print("\nETA_phase: final_write (typically tens of minutes)")
    elif dep_runs > 0:
        # Progress = unique scenes opened / export scenes (conservative; ignores multi-pass per scene)
        frac = len(opened) / max(1, len(scene_files))
        # Also: scenes with low open count may be incomplete - use min opens among opened as pass progress
        if counts:
            # Heuristic: heavy scenes reach 200+ opens; use median opens / max(max,200) capped
            med = sorted(vals)[len(vals) // 2]
            mx = max(vals)
            pass_frac = min(1.0, med / max(mx, 1))
            combined = 0.7 * frac + 0.3 * pass_frac
        else:
            combined = frac
        print(f"\nprogress_heuristic={combined*100:.1f}% (scene_coverage={frac*100:.1f}%)")
        if combined > 0.05:
            # elapsed since log file birth today - rough
            elapsed_h = (st.st_mtime - st.st_ctime) / 3600 if st.st_mtime > st.st_ctime else 0
            if elapsed_h > 0.1 and combined < 0.99:
                rem_h = elapsed_h * (1 - combined) / combined
                print(f"ETA_estimate_hours={rem_h:.2f} (from log_file_age={elapsed_h:.2f}h and heuristic; ±40%)")
            else:
                print("ETA_estimate: insufficient timing data")
        print("note: final bundle write adds ~15-45 min after scene phase")
    else:
        print("\nETA_phase: pre-SBP or asset mapping")

    # --- Quantitative scene-phase model (this build only) ---
    if counts and not build_result and bundle_count == 0:
        done_thresh = 270  # finished scenes cluster at ~280-312 opens in this log
        done = sum(1 for v in counts.values() if v >= done_thresh)
        partial = [v for v in counts.values() if v < done_thresh]
        target_per_scene = max(counts.values())  # use observed max as pass count
        rem_incomplete = sum(target_per_scene - v for v in partial)
        rem_new = (len(scene_files) - len(counts)) * target_per_scene
        rem_opens = rem_incomplete + rem_new
        elapsed_min = max(1.0, (datetime.now() - datetime(2026, 10, 8, 7, 45, 0)).total_seconds() / 60)
        rate = len(opens) / elapsed_min
        tail = post[-3000:]
        tail_opens = sum(1 for line in tail if open_re.search(line))
        print("\n=== Scene-phase ETA model ===")
        print(f"scenes_done(>={done_thresh} opens)={done}/{len(scene_files)} scenes_touched={len(counts)}")
        print(f"target_opens_per_scene~{target_per_scene} total_opens_so_far={len(opens)}")
        print(f"opens_per_min={rate:.2f} (since 07:45 restart) recent_in_last_3000_lines={tail_opens}")
        print(f"remaining_opens_est={rem_opens} -> {rem_opens / rate:.0f} min ({rem_opens / rate / 60:.1f} h)")
        print("after scenes: asset bundle pack/write (log has 0 WriteSerializedFiles) +20-45 min typical")


if __name__ == "__main__":
    main()
