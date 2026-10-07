# ultrakill-ios-port

**Split install:** small **engine IPA** (GitHub Actions) + large **game data folder** on the phone (you unpack yourself). No multi‑GB uploads to GitHub.

## Phone setup

1. Install the unsigned engine IPA (`ULTRAKILL-unsigned.ipa` from Actions artifacts).
2. On PC, build the content zip (see below) and **unpack** it on the phone (or unpack on PC and copy the folder).
3. In **Files → On My iPhone → ULTRAKILL**, create **`ULTRAKILL-Content`** and put **`ULTRAKILL_Data`** inside it (same layout as the zip root).
4. Launch the app — it uses that folder directly (no in-app unzip) and remaps `StreamingAssets` / Addressables paths.

## Build content zip (Windows, needs Steam ULTRAKILL)

```powershell
$env:ULTRAKILL_RIP = "C:\Users\v0id\Downloads\cockadoodledo\fdadsfsadfff"   # optional AssetRipper scripts merge
.\scripts\Build-ContentZip.ps1
# → artifacts\ULTRAKILL-Content.zip
```

Zip layout at extract:

- `ULTRAKILL_Data/` — full Steam `ULTRAKILL_Data` tree  
- `manifest.json` — build metadata  

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

`Assets/Game` junctions to your rip’s `Scripts/Assembly-CSharp` (~1500 files). Retail `Managed` DLLs (except `Assembly-CSharp.dll`) go to `Assets/Plugins/RetailManaged` as references. Boot scene is copied from `ULTRAKILL_Data/level0`. Define `ULTRAKILL_FULL_PORT` enables Addressables remap into the Documents zip.

**GitHub Actions** builds **`unity-ultrakill`** on every push to `main` (artifact **`ultrakill-ios-ipa`**). Managed DLLs live in `unity-ios/Assets/Plugins/UltrakillManaged` (committed). Optional decompiled sources:

```powershell
.\scripts\Sync-GameSources.ps1   # copies rip -> game-sources/Assembly-CSharp, then commit + push
```

CI builds game logic from retail **`Assembly-CSharp.dll`** (same binary you’d patch in dnSpy). `game-sources/` is for local script edits; full recompile on Actions is not reliable yet.

## GitHub Actions

Secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` (GameCI personal license).

Artifact: **`ultrakill-ios-engine-ipa`** — engine only (~tens of MB).

## dnSpy / PC patch

Touch overlay sources: `patches/MobileTouch/`. For PC DLL patch see `docs/dnspy.md` and `scripts/patch-assembly.ps1`.

## Legal

You must own ULTRAKILL. Do not redistribute Hakita’s game files publicly.
