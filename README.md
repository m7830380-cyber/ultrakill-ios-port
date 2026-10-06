# ultrakill-ios-port

**Split install:** small **engine IPA** (GitHub Actions) + large **`ULTRAKILL-Content.zip`** (you copy to the phone). No multi‑GB uploads to GitHub.

## Phone setup

1. Install the unsigned engine IPA (`ULTRAKILL-Engine-unsigned.ipa` from Actions artifacts).
2. On PC, build the content zip (see below).
3. Copy `ULTRAKILL-Content.zip` to the phone (AirDrop, USB, iCloud Drive, etc.).
4. In **Files**, put the zip in **On My iPhone → ULTRAKILL** (the app’s Documents folder).
5. Launch the app — it extracts once into app storage and remaps `StreamingAssets` / Addressables paths.

## Build content zip (Windows, needs Steam ULTRAKILL)

```powershell
$env:ULTRAKILL_RIP = "C:\Users\v0id\Downloads\cockadoodledo\fdadsfsadfff"   # optional AssetRipper scripts merge
.\scripts\Build-ContentZip.ps1
# → artifacts\ULTRAKILL-Content.zip
```

Zip layout at extract:

- `ULTRAKILL_Data/` — full Steam `ULTRAKILL_Data` tree  
- `manifest.json` — build metadata  

## Engine project (touch + external loader)

```powershell
.\scripts\Sync-EngineScripts.ps1
```

Opens `unity-ios` in Unity 2022.3.62f1 for local tweaks; CI builds the same tree.

## GitHub Actions

Secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` (GameCI personal license).

Artifact: **`ultrakill-ios-engine-ipa`** — engine only (~tens of MB).

## dnSpy / PC patch

Touch overlay sources: `patches/MobileTouch/`. For PC DLL patch see `docs/dnspy.md` and `scripts/patch-assembly.ps1`.

## Legal

You must own ULTRAKILL. Do not redistribute Hakita’s game files publicly.
