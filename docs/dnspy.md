# Patching with dnSpy

Use this when you prefer editing `Assembly-CSharp.dll` directly instead of `scripts/patch-assembly.ps1` (ILRepack).

## Prerequisites

- [dnSpy](https://github.com/dnSpy/dnSpy) or dnSpyEx
- A legal copy of ULTRAKILL (PC) for `ULTRAKILL_Data/Managed/Assembly-CSharp.dll`
- Touch sources in `patches/MobileTouch/` (build with `dotnet build` or paste C# into dnSpy)

## Sidecar DLL (no dnSpy merge)

After `.\scripts\patch-assembly.ps1`:

1. Copy `artifacts/patched/UltrakillMobileTouch.dll` → `ULTRAKILL_Data/Managed/`
2. Merge `artifacts/patched/ScriptingAssemblies.json` into your port’s `ScriptingAssemblies.json` (add `UltrakillMobileTouch.dll` + type `16`).

`MobileTouchBootstrap` uses `[RuntimeInitializeOnLoadMethod]` so it loads without editing `InputManager`.

## Merge into Assembly-CSharp (dnSpy or ILRepack)

1. **Back up** `Assembly-CSharp.dll`.
2. Open dnSpy → **File → Open** → select `Assembly-CSharp.dll`.
3. **Add types** (either paste compiled code or C#):
   - `UltrakillIOS.MobileTouchBootstrap`
   - `UltrakillIOS.MobileTouchHud`
   - `UltrakillIOS.LegacyInputSynthesizer`
4. Right-click the assembly → **Add Class** → paste each file from `patches/MobileTouch/`.
5. Compile (dnSpy checks references against the same `Managed` folder; add `Unity.InputSystem`, `NewBlood.LegacyInput`, `UnityEngine.*` if prompted).
6. Confirm `MobileTouchBootstrap` has `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` on `Install()` — no change to `InputManager` is required.
7. **File → Save Module** → overwrite or save as `Assembly-CSharp.patched.dll`.
8. Copy the patched DLL into your **iOS Unity project** `Assets/Plugins/` or replace `Data/Managed/Assembly-CSharp.dll` in the exported port tree.

## Verify on PC (optional)

Replace the DLL in a copy of `ULTRAKILL_Data/Managed`, launch with `-screen-fullscreen 0 -screen-width 1280 -screen-height 720`, and enable touch simulation in the Unity editor or on a touch laptop.

## iOS build (recommended: scripts, not DLL-only)

1. Decompile once (dnSpy export, AssetRipper Scripts, or `scripts/Decompile-GameScripts.ps1`).
2. Run `scripts/Setup-UnityPortFromDecompile.ps1` to create **`unity-ultrakill`** with `Assets/Game` → decompiled sources, mobile overlay, and external zip loader.
3. Open in Unity **2022.3.62f1**, resolve compile errors (Steam/Discord stubs on mobile), build **iOS** on a Mac.
4. Install engine/data split on device: IPA + `ULTRAKILL-Content.zip` in Documents.

You can still merge touch types into `Assembly-CSharp.dll` with dnSpy for PC testing; the mobile port compiles the same C# from `Assets/Game` instead of shipping the PC DLL.
