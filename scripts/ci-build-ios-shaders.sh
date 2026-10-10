#!/usr/bin/env bash
# Rebuild iOS shaders.bundle locally on macOS (or anywhere with licensed Unity iOS).
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
EXPORT_PROJECT="${EXPORT_PROJECT:-$REPO_ROOT/ExportedProject}"
UNITY_VERSION="${UNITY_VERSION:-2022.3.62f1}"
CATALOG_MAP="${CATALOG_MAP:-$REPO_ROOT/ci/catalog-map.json}"
SHADER_OUT="${SHADER_OUT:-${RUNNER_TEMP:-/tmp}/ultrakill-ios-shaders-out}"
ARTIFACT_DIR="${ARTIFACT_DIR:-$REPO_ROOT/artifacts/ios-shaders}"

export EXPORT_PROJECT CATALOG_MAP
bash "$REPO_ROOT/scripts/ci-prepare-export-shaders.sh"

rm -rf "$SHADER_OUT"
mkdir -p "$SHADER_OUT"
LOG="$REPO_ROOT/artifacts/ios-shaders.log"
mkdir -p "$(dirname "$LOG")"

UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$UNITY_BIN" ]]; then
  echo "Unity not found at $UNITY_BIN (set UNITY_BIN)"
  exit 1
fi

"$UNITY_BIN" -batchmode -nographics -quit -buildTarget iOS -projectPath "$EXPORT_PROJECT" -logFile "$LOG" \
  -executeMethod IosRetailBundleBuild.Build \
  -catalogMap "$CATALOG_MAP" -outDir "$SHADER_OUT" \
  -onlyBundle "assets_assets_assets/shaders.bundle"

built=$(find "$SHADER_OUT" -name 'shaders.bundle' -type f -print0 | xargs -0 ls -S 2>/dev/null | head -n 1 || true)
if [[ -z "$built" || ! -f "$built" ]]; then
  echo "shaders.bundle not produced; see $LOG"
  find "$SHADER_OUT" -type f | head -50 || true
  exit 1
fi

mkdir -p "$ARTIFACT_DIR/assets_assets_assets"
cp -f "$built" "$ARTIFACT_DIR/assets_assets_assets/shaders.bundle"
builtin=$(find "$SHADER_OUT" -name 'shader_unitybuiltinshaders.bundle' -type f | head -n 1 || true)
if [[ -n "$builtin" && -f "$builtin" ]]; then
  cp -f "$builtin" "$ARTIFACT_DIR/shader_unitybuiltinshaders.bundle"
fi

ls -lh "$ARTIFACT_DIR/assets_assets_assets/shaders.bundle"
echo "Copy to device:"
echo "  ULTRAKILL-Content/ULTRAKILL_Data/StreamingAssets/aa/iOS/assets_assets_assets/shaders.bundle"
