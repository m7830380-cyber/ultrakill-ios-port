# ULTRAKILL iOS port (TrollStore)

Runs the retail ULTRAKILL build on iOS by transplanting the game's own data into an IL2CPP player
built from the same Unity version (2022.3.29f1).

* `shell/` – minimal Unity project. CI IL2CPP-compiles the patched retail assemblies into an unsigned
  IPA (`shell-latest` release). Contains no game data.
* `tools/patcher` – Mono.Cecil patcher for the retail assemblies (native plugin stubs, save paths, ...).
* `tools/*.py` – converts the retail Windows data for iOS (DX11 shaders -> Metal via HLSLcc, platform
  tags, ...) and packages the final IPA on the PC.
