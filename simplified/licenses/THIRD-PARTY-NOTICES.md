# Third-party components

The loader binaries originate from the official BepInEx 5.4.23.5 x86 distribution. In package 0.1.13, MonoMod.Utils.dll is locally modified: SetMonoCorlibInternal uses the verified Unity 6000.2.8f1 x86 MonoAssembly layout (corlib_internal offset 83 instead of 79). The input DLL and reproducible patch script are included under source. All other loader binaries remain unmodified. Their upstream licenses are included here. The plugin does not bundle Unity, game assemblies, game artwork, Newtonsoft.Json, or system fonts; these are supplied by the installed game or operating system.

| Component | Bundled version | License file | Upstream source |
|---|---|---|---|
| BepInEx | 5.4.23.5 | BepInEx-LICENSE.txt | https://github.com/BepInEx/BepInEx/tree/v5.4.23.5 |
| HarmonyX | 2.9.0 | HarmonyX-LICENSE.txt; Harmony-original-LICENSE.txt | https://github.com/BepInEx/HarmonyX/tree/v2.9.0 |
| BepInEx.Harmony / HarmonyXInterop / 0Harmony20 | d4cdcb4cdeac14a0b77012165f5f5a9f5032a9fa | HarmonyInterop-LICENSE.txt | https://github.com/BepInEx/BepInEx.Harmony/tree/d4cdcb4cdeac14a0b77012165f5f5a9f5032a9fa |
| Mono.Cecil and companion assemblies | 0.10.4 | Cecil-LICENSE.txt | https://github.com/jbevain/cecil/tree/0.10.4 |
| MonoMod.RuntimeDetour / MonoMod.Utils | 22.01.29.01 | MonoMod-LICENSE.txt | https://github.com/MonoMod/MonoMod/tree/v22.01.29.01 |
| UnityDoorstop (winhttp.dll) | 4.5.0 | Doorstop-LICENSE.txt | https://github.com/NeighTools/UnityDoorstop/tree/v4.5.0 |

UnityDoorstop is supplied as a separate, unmodified DLL. Its corresponding upstream source, build scripts and license are included in `UnityDoorstop-v4.5.0-source.zip`. That archive was fetched from `https://codeload.github.com/NeighTools/UnityDoorstop/zip/refs/tags/v4.5.0`; the tag has no Git submodules. Users may replace or rebuild the loader; the localization installer refuses to overwrite an existing modified loader. These notices do not restrict rights granted by the upstream licenses.

Asbury Pines and its original narrative, names and artwork belong to their respective owners. The translation files are for use with a separately installed copy of the game. The source code for this localization plugin is included separately; the third-party license files above apply to their respective components, not automatically to the game or translations.
