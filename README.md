# CyberQuest

CyberQuest is a Unity cybersecurity learning game with six educational units. Scenarios combine quests, persona choices, mail, a simulated terminal and browser, application screens, Cyberpedia articles, map locations and minigames. A retained content editor supports local educational-content authoring.

This source preparation requires separately obtained Unity assets and locally restored resources. It is not a ready-to-run clean checkout. Compilation, builds, gameplay and a clean dependency installation have not been verified.

## Requirements and setup

- Unity **2023.2.22f1**, revision `6b19bf4f8115`, with the build-support module for your intended desktop platform.
- Python **3.11 or newer**, standard library only, for the setup tools.
- The matching local dependencies and customized resources listed in [Dependency setup](DEPENDENCIES.md). Some packages require separate licenses. Package release labels and exact file identities are distinguished in that guide.

Keep the public source tree separate from the licensed input project and the local working project. From this source directory:

```shell
python -B Tools/setup_dependencies.py --inputs ../LicensedInputs --preview
python -B Tools/setup_dependencies.py --inputs ../LicensedInputs --apply --output ../CyberQuest-Local
```

The first command is read-only. The second runs the same full preflight, then creates a separate local Unity project; it never installs vendor files into this source tree. Missing, conflicting or unsupported inputs stop preparation before output is written. It does not download packages or launch Unity. Default scene restoration uses an editor helper in the local project; an optional exact-donor mode is documented in the setup guide.

Open the prepared local project with the pinned Unity editor. Let Package Manager restore the versions already declared in `Packages/manifest.json` and `packages-lock.json`. Finish the resource assignments and local map regeneration (including the standard globe grid mask in either scene mode) described in [Dependency setup](DEPENDENCIES.md). Preserve the package versions and scoped registry rather than accepting blanket upgrades.

## Project layout

| Path | Purpose |
| --- | --- |
| `Assets/_GAME/Scripts` | Gameplay, content models/loaders, UI, minigames, saves, editor integrations and build tools |
| `Assets/_GAME/Scenes/GameScenes` | Menu and globe scene variants |
| `Assets/_GAME/Scenes/ExtensionScenes/DevContentEditorScene.unity` | Educational content editor |
| `Assets/_CONTENT/units/ismg6860_u00` through `ismg6860_u05` | Six selected units and their local task, app, web, mail and image payloads |
| `Assets/_ASSETS/Textures` | Retained project-created media; licensed map resources are restored locally |
| `Packages` | Pinned registry declarations and retained Unity Web Browser / Build Report Inspector packages |
| `Tools` | Hash-qualified local dependency setup, resource requirements and notice collection |
| `Licenses` | Retained third-party license texts and notices |

The runtime loader uses units directly. No separate content repository, submodule or Git LFS is required. Unit `.app` files are educational application definitions and are intentionally tracked.

## Entry points and builds

Open `Assets/_GAME/Scenes/GameScenes/MenuScene.unity` for the menu. The active Game build profiles use that menu and `GlobeScene_vNeon.unity`. For content authoring inside Unity, open `Assets/_GAME/Scenes/ExtensionScenes/DevContentEditorScene.unity` directly.

The editor menu **Build > Build Profiles Window** loads the active Windows and macOS Game profiles from `Assets/_GAME/Resources/BuildProfiles`. Retained Linux/standalone-editor profile assets live under `BuildProfiles_Old` and are not loaded by that window by default; review their scene assignments before intentionally activating one. Demo builds remain disabled.

Build outputs resolve relative to the project under `Builds/<profile>`. The build helper rejects unsafe paths and handles the Windows executable, macOS application bundle and Linux executable conventions. The post-build hook copies educational content into the player's data location and omits authoring/templates and repository metadata. These are source capabilities, not recorded successful builds.

The standalone browser uses Unity Web Browser/CEF and a unit-local HTTP server bound to loopback. Displayed websites, accounts, credentials and command responses are fictional training evidence; the terminal and browser are simulations. Preserve the local web payloads and browser/RPC settings. This preparation does not include the former hosted WebGL deployment, runtime AI integrations, surveys or Steam integration, and needs no associated API keys.

## Minigame credits

The first three minigames added to CyberQuest — **Connections**, **Waves** and **Network** — are based on code shared by **Lukasz Kusyk**. These minigames originated in his independent projects and were adapted for CyberQuest.

## Content, media and limitations

Educational app imagery is retained for the scenarios, including screenshots and simulated/recreated application screens. Character/persona/unit/minigame/logo artwork includes AI-generated media. Fonts, commercial UI/maps/icons/music and their derivatives retain their applicable publisher terms and attribution requirements.

Current persona, learning points and article-reading progress use the retained game state. Unsupported reputation, ethics, affiliation, title and other fixed progress statistics are labeled `Not tracked`.

Before distributing any build, retain the bundled notices and collect the actual installed dependency/native-engine notices as described in [Third-party notices](THIRD_PARTY_NOTICES.md). Runtime behavior, platform support, generated map appearance and media readability in game remain unverified.
