# ULTRAKILL iOS port (TrollStore)

Runs the retail Steam build of ULTRAKILL on iOS. Instead of rebuilding the game from a decompiled
project, it takes the game's own data and runs it in an IL2CPP player built with the same Unity
version (2022.3.29f1):

* `shell/` – minimal Unity project. CI IL2CPP-compiles the patched retail assemblies plus the iOS mod
  into an unsigned IPA (`shell-latest` release). It contains no game data.
* `tools/patcher` – Mono.Cecil patcher for the retail assemblies (stubs Steam/Discord/native plugins,
  moves Saves/Preferences into Documents, turns off the compute-shader path).
* `tools/convert_data.py` – converts the Windows data for iOS: every DX11 shader is translated to
  Metal with Unity's HLSLcc (`tools/hlslcc_cli`), and every file is re-tagged as an iOS build.
* `tools/package_ipa.py` – merges the shell IPA and the converted data into `ULTRAKILL.ipa`.
* `mod/` – iOS runtime additions: touch controls, Documents content loading, log file.

## Building (Windows PC with the Steam game installed)

```
# one-time: build the HLSLcc translator (Visual Studio 2022 + CMake)
cmake -S tools/hlslcc_cli -B tools/hlslcc_cli/build -G "Visual Studio 17 2022" -A x64
cmake --build tools/hlslcc_cli/build --config Release

python tools/convert_data.py --game "C:/Program Files (x86)/Steam/steamapps/common/ULTRAKILL" --out work
python tools/package_ipa.py --work work          # downloads the latest shell IPA from CI
```

Result: `work/ULTRAKILL.ipa` (~200 MB) and `work/ULTRAKILL-Content` (~3.4 GB).

## Installing on the device

1. Install `ULTRAKILL.ipa` with TrollStore.
2. Copy the whole `ULTRAKILL-Content` folder into the app's Documents folder
   (Filza: `/var/mobile/Containers/Data/Application/<ULTRAKILL>/Documents/`, or Finder/iTunes file
   sharing — the app exposes its Documents folder). The result must be
   `Documents/ULTRAKILL-Content/aa/catalog.json`.
3. Launch. Saves and settings live in `Documents/Saves` and `Documents/Preferences`.

Log: `Documents/ULTRAKILL-iOS.log` (previous run: `ULTRAKILL-iOS-prev.log`). Tap the FPS counter in
the top-right corner to see the latest log lines on screen.

## Controls

Left side: movement stick (appears where you touch). Right side: drag to aim. FIRE / ALT also aim
while held. JUMP, DASH, SLIDE, PUNCH, HOOK (whiplash), VAR (next variation), ARM (change arm),
1–5 weapon slots on top, II = pause. Look sensitivity follows the in-game mouse sensitivity option.
