# ultrakill-ios-port

**Split install:** small **engine IPA** (GitHub Actions) + large **game data folder** on the phone (you unpack yourself). No multi‑GB uploads to GitHub.

## Phone setup

1. Install the unsigned engine IPA (`ULTRAKILL-unsigned.ipa` from Actions artifacts).
2. On PC, build the content zip (see below) and **unpack** it on the phone (or unpack on PC and copy the folder).
3. In **Files → On My iPhone → ULTRAKILL**, create **`ULTRAKILL-Content`** and put **`ULTRAKILL_Data`** inside it (same layout as the zip root).
4. Launch the app — it uses that folder directly (no in-app unzip) and remaps `StreamingAssets` / Addressables paths.

## Build content for iPhone (Windows + Steam ULTRAKILL)

**Use this for the port** (iOS Addressables + `catalog.json` with `iOS` paths). `Build-ContentZip.ps1` alone copies **PC** `StandaloneWindows64` bundles and is wrong for the phone.

```powershell
# 1) Rebuild iOS bundles from AssetRipper export (hours for all ~81 bundles; tutorial-only is faster):
.\scripts\Build-IosShaders.ps1          # stub shaders.bundle (~0.65 MB, 121 shaders)
.\scripts\Build-IosBundles.ps1          # all bundles → artifacts\ios-content\...
#    .\scripts\Build-IosBundles.ps1 -OnlyBundle tutorial   # just Tutorial scene bundle

# 2) Package for copy/unzip on the phone:
.\scripts\Package-IosContentForDevice.ps1 -Mode overlay
# → %USERPROFILE%\Downloads\ULTRAKILL-Content-for-iPhone.zip

# Full Steam tree + iOS aa overlay (~3.5 GB):
.\scripts\Package-IosContentForDevice.ps1 -Mode merged
```

Phone layout after unpack:

- `ULTRAKILL-Content/ULTRAKILL_Data/` — game data (you can use a slim tree; Addressables must live under `StreamingAssets/aa/`)
- `ULTRAKILL-Content/filelist.txt` — optional completeness check on device
- `ULTRAKILL-Content/manifest.json` — build metadata

Legacy PC-only zip (not for iOS port):

```powershell
.\scripts\Build-ContentZip.ps1   # → artifacts\ULTRAKILL-Content.zip (Windows aa)
```

## Full port (decompiled scripts + dnSpy path)

The gray-screen engine IPA is only a loader. Real gameplay needs **Assembly-CSharp** sources (AssetRipper / **ilspycmd** / dnSpy) in a Unity project:

```powershell
# Optional: fresh decompile from retail DLL (same output as dnSpy)
.\scripts\Decompile-GameScripts.ps1

# Wire rip + retail Managed refs + boot scene + touch + zip loader
$env:ULTRAKILL_RIP = "C:\Users\v0id\Downloads\cockadoodledo\fdadsfsadfff"
.\scripts\Setup-UnityPortFromDecompile.ps1
# Open unity-ultrakill in Unity 2022.3.62f1, fix compile (Steam etc.), build iOS
```

`Assets/Game` junctions to your rip’s `Scripts/Assembly-CSharp` (~1500 files). Retail `Managed` DLLs (except `Assembly-CSharp.dll`) go to `Assets/Plugins/RetailManaged` as references. Define `ULTRAKILL_FULL_PORT` (passed by `IosUnsignedBuild`) enables the Addressables remap into `Documents/ULTRAKILL-Content`.

**GitHub Actions** builds **`unity-ultrakill`** on every push to `main` (artifact **`ultrakill-ios-ipa`**). Managed DLLs live in `unity-ios/Assets/Plugins/UltrakillManaged` (committed). Optional decompiled sources:

```powershell
.\scripts\Sync-GameSources.ps1   # copies rip -> game-sources/Assembly-CSharp, then commit + push
```

CI builds with retail **`Assembly-CSharp.dll`** and `Bootstrap.unity`. On launch, `RetailBootProbe` initializes Addressables from the external catalog and tries to load `Main Menu` / `Intro` / `Tutorial`, logging each step.

Steam's `level0` cannot be used as a scene: it is a compiled player file, which Unity rejects as an editor scene ("Invalid serialized file header").

Local script work: `game-sources/Assembly-CSharp` (~1500 files). Link into Unity with `.\scripts\Link-GameScripts.ps1` (do not commit `Assets/Game` with the DLL present).

## Logs

Every session writes `Documents/ULTRAKILL-Logs/session-<date>-<time>.txt` (last 10 kept), visible in **Files → On My iPhone → ULTRAKILL**. The in-game **LOG** button (top-right) toggles a live console with the boot status.

## GitHub Actions

Secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` (GameCI personal license).

Artifact: **`ultrakill-ios-engine-ipa`** — engine only (~tens of MB).

## dnSpy / PC patch

Touch overlay sources: `patches/MobileTouch/`. For PC DLL patch see `docs/dnspy.md` and `scripts/patch-assembly.ps1`.

## Legal

You must own ULTRAKILL. Do not redistribute Hakita’s game files publicly.
