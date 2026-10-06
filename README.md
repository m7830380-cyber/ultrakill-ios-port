# ultrakill-ios-port

Toolchain to add **on-screen touch controls** to a ULTRAKILL iOS Unity export, patch `Assembly-CSharp.dll` with **dnSpy** or automation, and produce an **unsigned `.ipa`** via GitHub Actions.

You must own ULTRAKILL on PC. This repo does **not** ship game assets or decompiled game code—only the mobile input overlay and build glue.

## Quick start (Windows)

1. Copy `ULTRAKILL_Data/Managed` to `game-managed/` (gitignored), or set:

   ```powershell
   $env:ULTRAKILL_MANAGED = "C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL_Data\Managed"
   ```

2. Build the touch patch:

   ```powershell
   .\scripts\patch-assembly.ps1
   ```

3. Copy `artifacts/patched/UltrakillMobileTouch.dll` and update `ScriptingAssemblies.json` (generated beside it) in your iOS Unity port’s `Data/Managed` tree.

   Optional: `.\scripts\patch-assembly.ps1 -MergeIntoAssemblyCSharp` or dnSpy merge into `Assembly-CSharp.dll`.

4. Build iOS locally or push to GitHub (see below).

## dnSpy (manual)

See [docs/dnspy.md](docs/dnspy.md).

## Touch layout

| Zone | Action |
|------|--------|
| Left stick | Move (W/A/S/D via `LegacyInput`) |
| Right drag | Look (mouse delta) |
| FIRE / ALT / JMP / SLD / DSH | Combat buttons |

Sources: `patches/MobileTouch/`.

## GitHub Actions

1. Add repo secrets for [game-ci Unity activation](https://game.ci/docs/github/activation):
   - `UNITY_LICENSE` (or `UNITY_EMAIL` + `UNITY_PASSWORD`)
2. Optional: add `game-managed/` on a self-hosted runner or private fork for the patch job.
3. Workflow [`.github/workflows/build-ios.yml`](.github/workflows/build-ios.yml):
   - **patch-assembly** — builds `UltrakillMobileTouch.dll` and merged `Assembly-CSharp.dll` when `game-managed/` exists
   - **ios-unsigned** — Unity iOS export + zip **unsigned.ipa** artifact

The bundled `unity-ios` project is a **CI smoke target** (minimal scene). Replace it with your full port tree when building real ULTRAKILL on device.

## Legal

For personal use with assets you already own. Do not redistribute Hakita/Arsi “Hakita” game files.
