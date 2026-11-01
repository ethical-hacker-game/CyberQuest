# Local dependency setup

The setup catalogue identifies exact accepted file contents, original/patched hashes, metadata GUIDs, relocated paths and position-only edits. It contains no complete commercial asset source, textures, fonts, music or scene resources. A matching file hash establishes input identity, not Unity-tested compatibility.

## Dependency identity and acquisition

Use the publisher's official distribution. Obtain the matching release or matching archived files rather than installing the latest version. An observed release label does not override the catalogue's file checks. Availability of historical releases is not guaranteed; request the matching distribution from the publisher when needed.

| Dependency | Identified input evidence | Required use |
| --- | --- | --- |
| [Beam - Complete Fluent UI](https://assetstore.unity.com/packages/2d/gui/beam-complete-fluent-ui-272318) by Michsky | 1.0.3 archive identified; accepted file hashes are authoritative | Core UI, fonts, presets and customized prefab behavior |
| [Graph And Chart](https://assetstore.unity.com/packages/tools/gui/graph-and-chart-data-visualization-78488) by BitSplash Interactive | 1.218.0 archive identified; accepted file hashes are authoritative | Persona radar and chart labels |
| [Runtime Inspector & Hierarchy](https://assetstore.unity.com/packages/tools/gui/runtime-inspector-hierarchy-111349) by yasirkula | 1.7.0 archive identified; accepted file hashes are authoritative | Runtime/content-editor inspectors and owner attributes; this distribution uses Asset Store terms |
| [Auto Radial Layout](https://assetstore.unity.com/packages/tools/gui/auto-radial-layout-293726) by GIGA Softworks | Exact imported API/file snapshot; numbered release not established. The inspected 1.1.4 API differs | Quest-tree layout; `AddNode` must return the created object |
| [World Map Globe Edition 2](https://store.kronnect.com/products/world-map-globe-edition-2) by Kronnect | Local README identifies 19.0; accepted file hashes remain authoritative | Gameplay/menu globe, maps, labels and generated geometry |
| [World Map Strategy Kit 2](https://assetstore.unity.com/packages/tools/game-toolkits/world-map-strategy-kit-2-150938) by Kronnect | Exact imported file snapshot; numbered release not established | Retained 2D map/editor integration and its local Resources tree |
| [Odin Inspector and Serializer](https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041) by Sirenix | `3.3.1.7` marker in the editor assembly; package release identity is not independently established. 4.0.2.3 is not qualified | Inspector attributes and serialization/editor support |
| [Simple Vector Icons](https://assetstore.unity.com/packages/2d/gui/icons/simple-vector-icons-101218) by Unruly Games | 0.6 archive identified; file hashes/GUIDs govern | UI icons plus separately configured local variants |
| [The Lab](https://darkfantasystudio.itch.io/the-lab) by Dark Fantasy Studio / Nicolas Jeudy | Eleven named MP3 tracks identified by hash; no numbered release established | Soundtrack; preserve applicable attribution and distribution terms |
| [TextMeshPro resources](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) | Pinned Unity editor plus exact essentials/font/sprite resource hashes | Text rendering, default font/material and configured EmojiOne sprite asset |

These packages are locally installed dependencies, including Runtime Inspector. Its upstream GitHub MIT distribution is a distinct provenance; substituting it without matching the accepted files/GUIDs is unsupported. TMP essentials do not require another commercial TMP purchase. Preserve each font's own license rather than treating all text assets as one font.

## Installation order and inputs

1. Create a separate licensed input Unity project using **2023.2.22f1**. Import TMP essential resources through the editor and preserve the matching font/material/sprite resources and their notices. Do not upgrade the public project's package declarations.
2. Import Odin and Runtime Inspector, then Beam and its required Input System, charts and radial layout. The prepared project pins Input System 1.7.0 and uses both input backends. [Beam's publisher guide](https://docs.michsky.com/docs/beam-ui/quick-start/) describes its input-system configuration.
3. Import both Kronnect map packages with their Resources, geodata, meshes, materials and fonts. Source preparation split resources between the package roots and `Assets/_ASSETS`; the catalogue maps destinations by path and metadata GUID. Keep one copy per resource GUID. Missing original Resources cannot be reconstructed by copying only the currently retained scripts.
4. Add the icons and soundtrack. Preserve the locally customized resources listed in `Tools/local_resource_requirements.json`, including their `.meta` files. Original package imports alone do not provide those custom variants.
5. Close the input project before preparation. Supply its project root to `--inputs`; it must contain `Assets/...`, not just one package's leaf directory. Files may be found at recorded source paths or by their matching metadata GUID, and must match an accepted hash. Do not copy whole dependency directories into the public source tree.

The tool requires Python 3.11+, uses only the standard library and does not download anything:

```shell
# Both forms are read-only; --check is also the default.
python -B Tools/setup_dependencies.py --inputs ../LicensedInputs --check
python -B Tools/setup_dependencies.py --inputs ../LicensedInputs --preview

# All required inputs are preflighted before creating a separate output project.
python -B Tools/setup_dependencies.py --inputs ../LicensedInputs --apply --output ../CyberQuest-Local
```

Per-package output distinguishes matching files, planned patches and missing/unsupported inputs. Exit code 1 means preflight is incomplete; exit code 2 means an invalid/conflicting input or operation. There is no force/skip-hash mode. Missing/wrong versions, conflicting owner additions, unsafe paths and links/reparse points are refused. Normalized text hashes tolerate BOM/newline differences where declared; patches preserve the input's BOM/newline convention.

The tool creates a temporary sibling, copies the approved source roots, assembles local dependencies and renames the completed directory. It does not mutate source or input files. If preparation fails, the public tree stays untouched and a named partial local sibling is retained for inspection. Existing unmarked outputs are refused. Re-running against an unchanged marked output with identical inputs is a no-op; changed local dependency/scene files are refused rather than overwritten. Use a new output directory for a reviewed new configuration.

## Focused patches and owner additions

The catalogue ports locking/info/array expansion for Runtime Inspector, the radial node return value, Beam settings accessors/sway/click audio/panel guards, radar support for two personas and label canvas opt-out, and globe keyboard direction corrections. Small reviewed resource/importer adjustments are included where supported. Six owner attribute source/metadata payloads preserve their GUIDs. Temporary presentation/demo achievement hacks and owner organizational Beam/radial/globe assembly definitions are omitted; package-supplied assembly definitions remain local inputs.

For large derived resources, the tool accepts the explicitly recorded configured local file; it does not publish a vendor-derived binary patch, invent a replacement font or bypass a version mismatch. The local-resource catalogue names the required hashes/GUIDs and technical reconstruction settings. Recreate or obtain a matching locally licensed resource before running `--apply`; regeneration may produce different bytes and must receive its own reviewed catalogue identity before being accepted. A successful preflight does not certify such a new variant.

## Scene resources and manual assignments

Default `--scene-mode regenerate` keeps the sanitized first-party scenes and installs an editor helper only in the local output project. After importing the local project, open each retained map scene and choose **Tools > CyberQuest > Rebuild Map Resources in Open Scenes**. The helper is guarded by the local-project marker. It uses the matched globe/map APIs to reload package geodata, redraw generated resources and reconnect empty mesh colliders to regenerated sibling meshes. Review and save the local scenes. It also assigns the licensed standard globe grid mask from `Assets/_ASSETS/WorldPoliticalMapGlobeEdition/Resources/Textures/Earth/EarthMask.png`. Enable Read/Write on that texture in the local Texture Import Settings before running the helper; a missing or unreadable mask stops restoration with an error. It skips inactive map objects; activate and handle those intentionally when needed.

The affected scenes are:

- `Assets/_GAME/Scenes/GameScenes/{MenuScene,MenuScene_Demo,GlobeScene,GlobeScene_vGreen,GlobeScene_vNeon}.unity`
- `Assets/_GAME/Scenes/ExtensionScenes/DevContentEditorScene.unity`
- `Assets/_GAME/Scenes/DevelopmentScenes/DevSandboxScene.unity`

The package code provides resource-generation APIs, but this helper has not been run in Unity. Generated appearance/bindings are not verified. The matching Resources tree must provide the sphere meshes, Earth/grid/cursor/glow materials, country/province/city geodata and fonts. Preserve the scene's first-party map transforms, style settings, cameras and integration components; do not replace an entire scene with a vendor demo.

If an existing locally licensed configured scene snapshot matches the donor hash recorded for a scene, an alternative mode can restore only its reviewed material/mesh objects, their resource fields and province data:

```shell
python -B Tools/setup_dependencies.py --inputs ../LicensedInputs --scene-mode donor --apply --output ../CyberQuest-Local
```

Donor mode requires all seven exact donors at their recorded relative scene paths. It does not copy complete donor scenes or reintroduce unrelated callbacks/configuration. Run the same editor helper after donor restoration to assign the standard globe mask; donor mode does not restore the obsolete mask binding. Regenerated or donor-restored scene data stays in the separate local project, which has no copied public Git history. Do not merge vendor serialization back into the public source tree.

Also review these resource groups locally:

| Group | Required restoration/assignment |
| --- | --- |
| Map images | Restore all eight named files under `Assets/_ASSETS/Textures/Map`, their recorded metadata and configured 2D/UI assignments. Their source is the map dependency; they are not bundled first-party artwork. |
| Fonts/TMP | Restore essentials, default sprite and font assets/materials with matching GUIDs; use the recorded font source, Unicode coverage, atlas/face settings and material overrides when recreating a local variant. Do not silently substitute a stock font for the glow/arrow configuration. |
| Beam | Preserve configured UI manager/presets, splash controller, button/hint/localization bindings, modified font and shadow resources. The three settings accessors and click integration are code patches, not a replacement for these assets. |
| Charts/radial | Preserve the chart label/icon prefab bindings, radar fonts/themes, node prefabs and their GUIDs. The two-persona/radar-label patches do not recreate serialized theme assets. |
| Runtime Inspector/Odin | Keep field prefabs/settings/resources and plugin importer/platform settings alongside patched scripts and owner attributes. |
| Icons/music | Retain the named local icon variants and audio metadata, and confirm their existing UI/audio bindings. Preserve the publisher's applicable notices. |

## Resource simplifications

Two settings element prefabs use plain colored UI Images instead of the missing legacy sprite. Their colors and interaction wiring are retained; the original sprite's decoration is omitted.

The obsolete `2DMapScreenshotScene` authoring scene is excluded. The main educational content editor remains available.

The retained globe scenes use the standard locally licensed globe grid mask assigned by the editor helper above. The old unresolved mask bindings are cleared in source; running the helper and saving each local scene is required in both regeneration and donor modes. Missing unused HDR environment bindings are cleared while preserving the current UserDefined/Basic skybox selections. No substitute vendor images are bundled.

These source changes remove the ten legacy unresolved GUIDs identified during static review. File identity/reference inspection does not verify generated appearance, serialized fileIDs or runtime behavior. Review the resulting UI and map scenes locally before distribution.

## Browser and package restore

The embedded Unity Web Browser package is **2.2.7**. The lockfile also pins CEF/platform engines to **2.2.7**, NativeArraySpanExtensions **1.0.1**, UniTask **2.5.10**, VoltRpc **3.2.1** and Newtonsoft.Json **3.2.1**. Use the scoped registry already declared as `https://upr.voltstro.dev`; [the browser's official repository](https://github.com/Voltstro-Studios/UnityWebBrowser) is the upstream entry point. The public package manifest omits samples that are not included here.

Native CEF engines are restored by Package Manager in the local project. Keep the browser/RPC connection on loopback and allow local loopback binding in the operating system when required. No hosted-domain replacement, Steam SDK, real training-site credentials or AI API configuration is needed. Windows x64, Linux x64 and macOS x64/arm64 engines are declared; declaration is not a platform build result.

After actual local installation, collect the dependency/native-engine notices as described in [Third-party notices](THIRD_PARTY_NOTICES.md). Runtime/platform checks were not performed. This guide and the setup tools were prepared without running tests, Unity or dependency installation.
