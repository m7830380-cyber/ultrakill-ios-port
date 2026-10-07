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
  # Only Hakita/third-party DLLs. Never copy Unity.* — those come from Packages/manifest.json.
  local allow=(
    NewBlood.DomainReloading.dll
    NewBlood.EngineInterop.dll
    NewBlood.LegacyInput.dll
    Naelstrof.JigglePhysics.dll
    NavMeshComponents.dll
    MaskedOcclusionCulling.dll
    Newtonsoft.Json.dll
    UnityUIExtensions.dll
    Vertx.Debugging.Runtime.dll
    TriInspector.dll
    plog.dll
    plog.unity.dll
    pcon.core.dll
    PrivateAPIEditorBridge.Core.dll
    PrivateAPIEngineBridge.Core.dll
    PrivateAPIEngineBridge.Ping.dll
    Bcl.CollectionsMarshal.dll
    Autodesk.Fbx.dll
    FbxBuildTestAssets.dll
    System.Runtime.CompilerServices.Unsafe.dll
  )
  for name in "${allow[@]}"; do
    if [[ -f "$MANAGED_SRC/$name" ]]; then
      cp "$MANAGED_SRC/$name" "$MANAGED_DST/"
    fi
  done
}

# Touch + zip loader (full port defines)
if command -v pwsh >/dev/null 2>&1; then
  pwsh -File "$ROOT/scripts/Sync-EngineScripts.ps1" -UnityProject "$PROJECT" -RipRoot "$ROOT/game-sources"
  pwsh -Command "
    Copy-Item '$ROOT/patches/MobileTouch/LegacyInputSynthesizer.cs' '$PROJECT/Assets/UltrakillIOS/' -Force
    Copy-Item '$ROOT/patches/MobileTouch/AddressablesContentRemap.cs' '$PROJECT/Assets/UltrakillIOS/' -Force
    Copy-Item '$ROOT/patches/MobileTouch/ExternalContentBootstrap.cs' '$PROJECT/Assets/UltrakillIOS/' -Force
    Copy-Item '$ROOT/patches/MobileTouch/RetailGameHooks.cs' '$PROJECT/Assets/UltrakillIOS/' -Force
    Copy-Item '$ROOT/patches/MobileTouch/ContentStatusHud.cs' '$PROJECT/Assets/UltrakillIOS/' -Force
  "
else
  echo "pwsh not found; overlay must already be in unity-ultrakill/Assets/UltrakillIOS"
fi

rm -rf "$PROJECT/Assets/Game"
copy_third_party_dll
if [[ ! -f "$MANAGED_SRC/Assembly-CSharp.dll" ]]; then
  echo "Missing $MANAGED_SRC/Assembly-CSharp.dll"
  exit 1
fi
cp "$MANAGED_SRC/Assembly-CSharp.dll" "$MANAGED_DST/"
# Referenced by game assembly; stripped from iOS player via IosPluginFilter.
if [[ -f "$MANAGED_SRC/Facepunch.Steamworks.Win64.dll" ]]; then
  cp "$MANAGED_SRC/Facepunch.Steamworks.Win64.dll" "$MANAGED_DST/"
fi
echo "CI game code: Assembly-CSharp.dll + third-party Managed refs (UltrakillIOS sources for touch/zip)."

# Steam level0 is a compiled player scene; Unity cannot import it as an editor scene.
rm -f "$PROJECT/Assets/Scenes/MainBoot.unity" "$PROJECT/Assets/Scenes/MainBoot.unity.meta"

if [[ ! -f "$PROJECT/Assets/Scenes/Bootstrap.unity" ]]; then
  echo "Missing Assets/Scenes/Bootstrap.unity"
  exit 1
fi

echo "UltrakillIOS overlay:"
ls "$PROJECT/Assets/UltrakillIOS"

echo "CI port tree ready under $PROJECT"
