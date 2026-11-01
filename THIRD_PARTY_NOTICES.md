# Third-party notices and distribution

These notices cover identified dependencies and do not select a project-wide code/content license. Commercial assets and derived fonts/maps/UI/icons/music are local-install resources; their inclusion in a local working copy does not permit publishing their source files. Preserve the applicable publisher terms when distributing a player build.

## Bundled material

| Component | Version and notice | Location |
| --- | --- | --- |
| Unity Web Browser, Voltstro-Studios | 2.2.7, MIT; original package notice preserved | `Packages/dev.voltstro.unitywebbrowser/LICENSE.md` and `Licenses/UnityWebBrowser-MIT.txt` |
| Build Report Inspector, Unity Technologies | 0.4.5-preview, Unity Companion License notice; editor functionality | `Packages/com.unity.build-report-inspector/LICENSE.md` and `Licenses/BuildReportInspector-Notice.txt`; [Unity terms](https://unity.com/legal/licenses/unity-companion-license) |
| D3, Mike Bostock | 7.9.0, ISC | `Licenses/d3-7.9.0-ISC.txt`; [versioned upstream text](https://raw.githubusercontent.com/d3/d3/v7.9.0/LICENSE) |
| html2canvas, Niklas von Hertzen | 1.4.1, MIT | `Licenses/html2canvas-1.4.1-MIT.txt`; [versioned upstream text](https://raw.githubusercontent.com/niklasvh/html2canvas/v1.4.1/LICENSE) |
| UniTask, Yoshifumi Kawai / Cysharp | 2.5.10, MIT; code restored through Package Manager | `Licenses/UniTask-2.5.10-MIT.txt`; [versioned upstream text](https://raw.githubusercontent.com/Cysharp/UniTask/2.5.10/LICENSE) |

Browser and web-library notice copies are also included under `Assets/StreamingAssets/ThirdPartyNotices` so that Unity includes them in desktop players. The six units retain their local web-library banners. Build Report Inspector's original package notice links the Unity Companion License and remains distinct from runtime MIT notices.

## Notices from the actual local installation

CEF/Chromium binaries and registry transitives are not bundled in this source preparation. Match the declared engine/transitive versions, then preserve the licenses, Chromium credits and other notices shipped with the actual selected engine/platform build. Do not treat the Unity Web Browser wrapper's MIT license as a license for all native Chromium components. NativeArraySpanExtensions and VoltRpc are separate dependencies as well; their actual installed package notices must accompany relevant distributions.

After Package Manager has restored the local project, run the collector from this source tree:

```shell
# Read-only inventory of the actual local assets and package cache.
python -B Tools/collect_dependency_notices.py --project ../CyberQuest-Local

# Copy the inspected license/notice files into that local player's StreamingAssets.
python -B Tools/collect_dependency_notices.py --project ../CyberQuest-Local --apply
```

The default package cache is the local project's `Library/PackageCache`; provide `--package-cache` if it lives elsewhere. Package names/versions must match the lockfile. Notice output is restricted to a marked local project and refuses conflicting existing output. Preserve both `ThirdPartyNotices` and the generated `DependencyNotices` directory with the build. A `NOTICE-REQUIRED.txt` marker identifies a dependency for which no standalone notice was found; obtain the applicable publisher terms/notice and resolve that marker before distributing a build. File collection cannot establish that every native third-party notice has been supplied.

For installed licensed assets, keep their subcomponent notices as well: Runtime Inspector's Asset Store distribution identifies Unity_ColorWheel separately; the map assets contain PolyTri/Clipper and other component notices; charts include SimpleJSON notices; TMP/Beam/map/chart font families have individual font licenses. The source LiberationSans OFL does not cover other fonts, SDFs or EmojiOne sprite artwork.

If restoring the soundtrack, credit **Dark Fantasy Studio and Nicolas Jeudy** for **The Lab**, as requested by the [publisher](https://darkfantasystudio.itch.io/the-lab). Retain the distribution terms supplied with that licensed audio package.

Registry notice collection includes packages published under `com.unity.*`, such as the Newtonsoft.Json wrapper, as well as the browser/native transitives. A Unity package prefix does not replace the licenses of its third-party components.

## Project content

Scenario text/data, first-party code, educational application imagery and AI-generated artwork are not relicensed by the dependency licenses above. Brand names and imagery used in educational simulations may have separate rights. Project-wide licensing and attribution scope remain pending before release. No third-party terms are replaced by a claim of educational purpose.
