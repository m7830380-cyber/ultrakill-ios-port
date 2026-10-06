#!/usr/bin/env bash
# Run on GitHub Actions before game-ci Unity build.
set -euo pipefail

ROOT="${GITHUB_WORKSPACE:-$(cd "$(dirname "$0")/.." && pwd)}"
PROJECT="$ROOT/unity-ultrakill"
MANAGED_SRC="$ROOT/unity-ios/Assets/Plugins/UltrakillManaged"
MANAGED_DST="$PROJECT/Assets/Plugins/RetailManaged"
SOURCES="$ROOT/game-sources/Assembly-CSharp"

if [[ ! -d "$PROJECT/ProjectSettings" ]]; then
  echo "unity-ultrakill project missing at $PROJECT"
  exit 1
fi

if [[ ! -d "$MANAGED_SRC" ]]; then
  echo "Missing committed Managed DLLs at $MANAGED_SRC"
  exit 1
fi

mkdir -p "$MANAGED_DST"
rm -rf "$MANAGED_DST"/*
copy_third_party_dll() {
  for dll in "$MANAGED_SRC"/*.dll; do
    base="$(basename "$dll")"
    case "$base" in
      Assembly-CSharp.dll|Unity.*|UnityEngine.*|mscorlib.dll|netstandard.dll|System.*|Mono.Security.dll)
        continue
        ;;
    esac
    cp "$dll" "$MANAGED_DST/"
  done
}

# Touch + zip loader (full port defines)
if command -v pwsh >/dev/null 2>&1; then
  pwsh -File "$ROOT/scripts/Sync-EngineScripts.ps1" -UnityProject "$PROJECT" -RipRoot "$ROOT/game-sources"
  pwsh -Command "
    Copy-Item '$ROOT/patches/MobileTouch/LegacyInputSynthesizer.cs' '$PROJECT/Assets/UltrakillIOS/' -Force
    Copy-Item '$ROOT/patches/MobileTouch/AddressablesContentRemap.cs' '$PROJECT/Assets/UltrakillIOS/' -Force
    Copy-Item '$ROOT/patches/MobileTouch/ExternalContentBootstrap.cs' '$PROJECT/Assets/UltrakillIOS/' -Force
  "
else
  echo "pwsh not found; overlay must already be in unity-ultrakill/Assets/UltrakillIOS"
fi

# Prefer decompiled sources in repo when present; otherwise IL2CPP uses Assembly-CSharp.dll from RetailManaged.
rm -rf "$PROJECT/Assets/Game"
if [[ -d "$SOURCES" ]] && [[ -n "$(ls -A "$SOURCES" 2>/dev/null || true)" ]]; then
  echo "Using decompiled sources from game-sources/Assembly-CSharp"
  cp -R "$SOURCES" "$PROJECT/Assets/Game"
  copy_third_party_dll
else
  echo "ERROR: game-sources/Assembly-CSharp missing. Run scripts/Sync-GameSources.ps1 locally and push."
  exit 1
fi

BOOT="$PROJECT/Assets/Scenes/MainBoot.unity"
if [[ ! -f "$BOOT" ]]; then
  echo "Missing $BOOT (commit retail level0 as MainBoot.unity)"
  exit 1
fi

echo "CI port tree ready under $PROJECT"
