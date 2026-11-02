# Build and dependency qualification

Inventory date: 2026-10-31. Library targets, consumer compatibility, vendor lifecycle, compilation and runtime qualification are separate. No targets or declared library OS minimums were removed or raised by this maintenance cycle. These workflow files are local configuration, not activated or remotely executed evidence.

## Current-source CI matrix

All rows involve FileStore, SQLite and LiteDB. The native consumer references both the core and the three providers, so the native core asset and the providers' existing `net8.0` assets must resolve together. `MonkeyCachePlatform` selects portable core plus ONE existing native target, avoiding restoration of unrelated workloads; omitted selectors preserve the original full target list. Selected-target automatic packing is disabled and explicit partial packing requires the same local-only consent as portable packing.

| Surface | Runner / toolchain | Check and remaining boundary |
| --- | --- | --- |
| Portable `net8.0` providers consumed by .NET 8 and 10 | `ubuntu-24.04`, `windows-2025`, `macos-15`; SDK 10.0.403 via root SDK feature-band policy; runtimes 8/10 | Full provider tests and fresh-process regressions. Local package smoke on Linux/macOS. Windows portable tests do not select WinRT core assets. Only Linux executed locally. |
| Core `net8.0-windows10.0.19041` | `windows-2025`, SDK 8.0.427, Windows SDK reference package | Three-provider consumer build. Linux cross-build also checked with `EnableWindowsTargeting=true`; neither establishes MAUI, WinAppSDK or package identity behavior. |
| Core `net8.0-android` | `ubuntu-24.04`, SDK/workload set 8.0.427, Android 34.0.154, hosted Android SDK API 34 and JDK 17 | Library/consumer build only, no emulator, APK installation or device test. |
| Core `net8.0-ios` | `macos-15`, SDK/workload set 8.0.427, iOS 18.0.8319, Xcode **16.0** | Unsigned library/consumer build only, no simulator/device/signing. Fixture minimum is 12.2; library's existing declared 10.0 is unchanged and still requires native qualification. |
| Core `net8.0-maccatalyst` | `macos-15`, SDK/workload set 8.0.427, Mac Catalyst 18.0.8319, Xcode **16.0** | Unsigned library/consumer build only, no app launch or distribution. |
| MAUI Windows **unpackaged** | Native Windows + matching MAUI/WinAppSDK runtime required | Outstanding runtime row: use retained #150/#151 protocol, verify absent package identity, startup stage, loaded core asset, default/explicit paths, exception escape and persistence. Do not substitute a portable console result. |
| MAUI Windows **packaged** | Same plus authorized test-package identity/provisioning | Separate outstanding runtime row: verify actual identity and packaged activation, then run the same assertions. No signing secrets or pretend green placeholder job is configured. |

The Apple workflow fails explicitly if Xcode 16.0 is missing; do not silently substitute the runner default Xcode 16.4. Native jobs fail independently (`fail-fast: false`), upload build logs and never sign/publish. Build-only jobs cannot close #150/#151. The retained native fixture/protocol are in the owner artifact handoff and remain unexecuted.

`python3 eng/test-tooling.py` runs offline standard-library fixtures for controller arguments, graph/framework output isolation, TRX/mixed-graph failures, child exit/timeout propagation and dependency inventory. These fixtures simulate subprocess/NuGet responses; they are not restores, builds or platform runs. Portable CI invokes them explicitly through Python. Native PowerShell toolchain capture checks the first command's exit code before running the next, so a successful `workload list` cannot hide a failed `dotnet --info`.

Run a native build from `eng/native` so its **exact** SDK pin applies:

```sh
dotnet --version  # must be 8.0.427
dotnet workload install android --version 8.0.427
dotnet build ../../tests/MonkeyCache.PlatformBuild/MonkeyCache.PlatformBuild.csproj -c Release -p:MonkeyCachePlatform=net8.0-android
```

For Apple, install `ios` or `maccatalyst` instead and select the documented Xcode on macOS. Windows needs no mobile workload. Do not run native and portable builds concurrently in one checkout: they restore different assets into the same project `obj` directories.

## Stable dependency inventory and justified holds

### Current .NET 10 / MAUI API consumer

`tests/MonkeyCache.MauiConsumer` consumes **Microsoft.Maui.Essentials 10.0.120** and reuses the existing regression executable, rather than replacing its assertions. This is an Essentials API consumer, **not a MAUI Controls application**. No `UseMaui` workload or UI/resource-processing target is disabled to manufacture a passing build.

The official `Microsoft.NET.Workloads.10.0.400/10.403.0` manifest contains MAUI **10.0.110**, whereas the current stable MAUI packages are **10.0.120**. Explicit package selection makes this standalone consumer use Essentials/Graphics 10.0.120; it does not upgrade the installed MAUI workload or qualify Controls/build-task compatibility. SDK 10.0.403 consumes the unchanged core/provider .NET 8 contracts.

From the repository root:

```sh
dotnet run --project tests/MonkeyCache.MauiConsumer -c Release -p:MonkeyCachePortable=true
```

This verifies portable MAUI/net8 core asset selection in every process and runs all 18 provider scenarios/150 existing assertions on .NET 10. It deliberately does not call MAUI's native `CacheDirectory` on Linux. The shared regression program's ordinary net8/net10 behavior is unchanged.

On Windows, the separate build-only CI job runs:

```sh
dotnet build tests/MonkeyCache.MauiConsumer -c Release -p:MauiConsumerWindows=true -p:MonkeyCachePlatform=net8.0-windows10.0.19041
```

The consumer targets `net10.0-windows10.0.19041.0`, minimum OS **17763**, framework-dependent **win-x64**, and unpackaged deployment. The library minimum remains **16299**, unchanged. NuGet selects MAUI Essentials/Graphics Windows assets; project references request the existing net8 Windows core and net8 providers. Actual native core loading is asserted by the unexecuted Windows runtime harness, not inferred from portable tests. Resolved native dependencies include Windows App SDK 1.8.260529003, Win2D 1.4.0 and WebView2 1.0.4258.31. An initial AnyCPU warning was corrected with an explicit RID, not suppressed.

Linux restoration succeeds, but the complete Windows consumer build requires Windows `MakePri.exe` for MAUI resources (MSB6003/APPX0002 on Linux). This is different from the already-successful **core-only Windows cross-build**. Resource generation remains enabled. The Windows job is configured but not remotely executed; its full build and runtime acceptance are outstanding.

After a successful build on a suitably provisioned Windows host with the matching Windows App Runtime, a diagnostic native-path run can set `MONKEYCACHE_MAUI_NATIVE=1` and `MONKEYCACHE_EXPECT_PACKAGE_IDENTITY=absent`, then run the same Windows consumer. It checks actual identity in every process, chooses MAUI's real cache directory, and runs the unchanged isolated provider assertions beneath that directory. `present` requires real authorized packaged activation; setting an environment variable does not create identity. Child identity inheritance is checked, not assumed. This opt-in path is **unexecuted** and has no automatic runtime CI job. It exercises an Essentials console consumer, not MAUI UI/DI startup; the original #150/#151 packaged/unpackaged application protocol is still required.

```powershell
$env:MONKEYCACHE_MAUI_NATIVE = '1'
$env:MONKEYCACHE_EXPECT_PACKAGE_IDENTITY = 'absent'
dotnet run --project tests/MonkeyCache.MauiConsumer -c Release --no-build -p:MauiConsumerWindows=true -p:MonkeyCachePlatform=net8.0-windows10.0.19041
```

### Android installation evidence and bounds

The retained SDK/workload 8.0.427 / Android pack 34.0.154 restores successfully. The original Microsoft dependency installer cannot resolve `aka.ms/AndroidManifestFeed/d17-12`; a distinct official Android command-line-tools repository attempt at `https://dl.google.com/android/repository/repository2-3.xml` also fails DNS resolution. No mirror, custom DNS, network-policy change or access-denial bypass was used.

Official Adoptium Temurin **17.0.21+9** was downloaded through its GitHub release, SHA256-verified and extracted into an isolated owned directory. Archive SHA256: `8b9b611d917e3dfd8419d3566b5949226f69b052fed7e109c1368add1dfe8711`. The preinstalled JDK is 27, so it is not silently substituted for the CI's JDK 17 requirement. Retesting the retained Android build with explicit `-p:JavaSdkDirectory=<isolated-jdk-17>` still fails **XA5300**, missing Android SDK. No Android compile success is claimed. The placement has no adb/emulator/KVM device and now insufficient free space for an additional emulator/system image; no emulator was provisioned and no destructive cleanup performed.

| Current local evidence | Configured | Restored | Complete build | Runtime-tested |
| --- | --- | --- | --- | --- |
| Retained net8 Android consumer | Yes | Yes | No: SDK unavailable | No |
| .NET 10 portable MAUI API consumer | Yes | Yes | Yes | 18 processes/150 assertions; portable APIs/assets only |
| .NET 10 Windows MAUI API consumer | Windows build-only job | Yes | No: Windows PRI tool required | No |
| Retained net8 Windows core/consumer | Yes | Yes | Yes: prior Linux cross-build | No native Windows |
| Retained iOS/Mac Catalyst | Yes | Not here | Not here | No |
| Modern Android/Apple MAUI application | Not added as a pretend passing job | No | No | No |

Current Apple requirements remain pinned in the previous matrix: Xcode 27.1/macOS 26.6+ for the latest .NET 10 Apple SDK, not the inspected runner's default Xcode 26.6. Modern Android needs its actual SDK/workload and native library asset qualification. Independent priorities are the remaining SQLite native/NuGet gates and qualification of the modern examples under the chosen policy below; completed Linux checks need not wait for native access or be repeated as a substitute for it.

`python3 eng/dependency-report.py --include-transitive` produces a timestamped, read-only NuGet report, with each source manifest/assets file and exact current stable version. It neither restores projects nor applies upgrades. Restore each native graph before capturing it; one portable assets file cannot inventory mobile dependencies. A lookup failure is an error, not a fabricated "up to date" result. The Linux CI row captures this report and a portable NuGet advisory report on each authorized future run. There is no external activation, dependency PR bot, publishing credential or scheduled cloud agent.

Resolved inventory includes both `project/obj/project.assets.json` and SDK artifacts layouts such as `artifacts/build/candidate/obj/project/project.assets.json` beneath the repository. Outputs outside the repository are not scanned. Declared references are not evaluated MSBuild selections: both branches of an opt-in package condition can appear as declarations; only an actual assets file establishes a resolved graph.

| Dependency/tool | Selected or observed | Current stable / reason if held |
| --- | --- | --- |
| Microsoft.NET.Test.Sdk | 18.11.0 | 18.11.0; supports net8, verified with existing assertions |
| MSTest adapter/framework | 4.5.2 | 4.5.2; test-only major update, verified rather than assuming compatibility |
| LiteDB | 5.0.21 | 5.0.21 |
| sqlite-net-pcl | 1.9.172 | 1.11.285 is opt-in qualification only. Linux database interchange passed, but native ABI/engine, trimming and platform gates below prevent default adoption. |
| SQLitePCLRaw.bundle_green | 2.1.11 | 2.1.11 (last compatible bundle); production and tests now align |
| SQLitePCLRaw core/provider | 2.1.11 via bundle | 3.0.5 belongs to the separate major migration; do not force an incoherent mixed bundle graph |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.14 | SourceGear.sqlite3 is a different packaging line: ORM minimum 3.53.3, latest observed 3.53.4. Retained native binary actually reports SQLite 3.54.0; candidate minimum reports 3.53.3. |
| SQLitePCLRaw.lib.e_sqlite3.android | 2.1.14 | 2.1.14; explicitly overrides old bundle-selected native binaries. Runtime/device coverage outstanding. |
| Microsoft.SourceLink.GitHub | SDK-bundled instead of explicit 1.1.1 | Standalone 10.0.403; use tooling delivered by the selected SDK, including SDK 8 in legacy native lanes |
| SDK/workload set | Portable 10.0.403; native 8.0.427 | Both latest stable in their SDK lines. Do not blanket-retarget libraries to use newer workloads. |
| Current MAUI | No MAUI package in library projects; API consumer pins Essentials 10.0.120 | 10.0.120; workload set 10.0.403 includes MAUI 10.0.110. The API consumer's explicit package selection is verified; Controls/workload compatibility remains unqualified. |
| Current Android/Apple workload | Native lanes retain .NET 8 pack versions above | .NET 10 Android 36.1.118, iOS 27.1.10018, Mac Catalyst 27.0.10742. Latest Apple release requires Xcode 27.1/macOS 26.6+, absent from the inspected standard macos-26 image (default Xcode 26.6). Current native .NET 10 consumer qualification is not claimed. |
| GitHub actions | checkout 7.0.1, setup-dotnet 6.0.0, upload-artifact 7.0.2 | Current releases; Node 24 actions require a current runner. Only hosted runner labels are configured. |

Transitives such as System.Memory/System.Buffers belong to the owning dependency graph, not arbitrary independent pins. The report records their exact resolved and latest stable versions; portable advisory results are not an all-platform security certificate.

## Opt-in SQLite major qualification

**Hold the default graph.** `MonkeyCacheSqliteCandidate=true` selects sqlite-net-pcl **1.11.285** instead of the four default references. The SQLite test project's old bundle is also excluded in this mode. No production resolver, public API, schema, cache path or supported library target changes. Candidate packaging is explicitly rejected, even with partial-package consent.

| Graph | Exact packages / Linux native result |
| --- | --- |
| Default, unchanged | sqlite-net-pcl 1.9.172; bundle_green/core/provider.e_sqlite3 2.1.11; lib.e_sqlite3 and lib.e_sqlite3.android 2.1.14. Loaded SQLite **3.54.0**. |
| Candidate ORM's declared minimum closure | sqlite-net-pcl 1.11.285; core/provider.e_sqlite3 **3.0.3**; SourceGear.sqlite3 **3.53.3**. Loaded SQLite **3.53.3**, not an engine upgrade. |
| Newer feed observations, not qualified | core/provider.e_sqlite3 3.0.5 and SourceGear.sqlite3 3.53.4. Do not mix these into the measured graph or imply that an ORM minimum is the newest transitive release. A newer native patch needs its own ABI/engine checks. |

On a host with enough free space, run from the root, using a **new** output directory:

```sh
python3 eng/qualify-sqlite.py --output artifacts/sqlite-qualification --framework net8.0 --framework net10.0
```

The normal command restores/builds each graph into separate artifacts directories, selects the host desktop RID, runs the unchanged SQLite provider suites, rejects failed/skipped/zero-test results and mismatched test counts, verifies exact restored SQLite package sets, then starts 28 fresh database processes. It records commands, logs, TRX and a summary. Tests use both directions: writer seeds, same graph reopens, other graph reads/updates, original graph reopens/clears, other graph verifies persisted removal. Assertions cover schema/INTEGER DateTime ticks, minimum/maximum expiration, cleanup, default/AutoExpire behavior, custom/generated JSON, ETags, explicit removal, row counts and `integrity_check`. Neither database migration nor new SQLite-only schema features are introduced.

`--default-entry '<path-with-{framework}>.dll' --candidate-entry '<path-with-{framework}>.dll'` explicitly runs **only** the database matrix against existing binaries. It does not restore/build/run provider suites or claim NuGet qualification; its summary identifies that restricted mode. Every child checks loaded ORM/Raw identities and logs the actual engine, SQLite source ID and native module path. Existing output/database directories are never reused or deleted.

The controller also enforces the harness assertion protocol: seed/read 21 each, update 34, updated 18, clear 19, empty 7. Completion/assembly markers alone must not pass a process that omitted checks. If intentionally extending the harness, update its controller and offline fixture counts together; the unchanged current protocol totals 282 assertions per runtime.

The manual-only `.github/workflows/sqlite-qualification.yml` configures normal NuGet qualification on Ubuntu 24.04, Windows 2025 and macOS 15. It is local, unactivated configuration. Those are **portable core** consumers with desktop native SQLite, not MAUI/WinRT, packaged Windows, mobile, minimum-OS or AOT qualification.

### Executed local evidence and limits

The following results are from the preceding SQLite qualification, not a fresh run of all suites on every tooling/documentation edit. Host: Ubuntu 24.04.3 x64, SDK 10.0.403, runtimes 8.0.33/10.0.14:

| Surface | NuGet graph restored | Compiled | Runtime |
| --- | --- | --- | --- |
| Default net8/net10 | Yes; exact six SQLite packages verified | Yes | 67 tests per runtime, 0 failures/skips |
| Candidate net8/net10 | **No full candidate restore**; exact official net8 managed and linux-x64 native assets extracted/hash-verified | Unchanged provider/test sources compiled through explicit binary-reference wrappers | 67 tests per runtime, 0 failures/skips |
| Cross-graph database matrix | Uses the above binaries | Both harnesses compiled | **28 processes / 564 assertions**, all passed |
| Windows/macOS/mobile/minimum Linux OS | Configuration/asset inventory only | Not this batch | Not executed |

Initial free space was 336 MB. The first RID restore downloaded .NET/ASP.NET runtime packs, leaving 124 MB; SourceGear alone is 39,311,481 compressed bytes plus 88,596,812 extracted bytes, before NuGet HTTP caching and build outputs. No full candidate restore was attempted into insufficient available space. Instead, official packages were read into memory and only their unmodified net8 DLLs/linux-x64 `.so` plus metadata were extracted into an isolated directory. No fake/pruned NuGet cache, dependency bypass presented as normal restore, mocked native library or suppressed test was used. The durable handoff includes hashes and the small temporary binary-reference projects. Normal NuGet/MSBuild packaging integration remains unexecuted. After the bounded checks approximately 76 MB remained; no workload installation or destructive cleanup was attempted.

### Adoption blockers and falsification criteria

- **Native ABI change (static, verified ELF metadata):** retained linux-x64 native library requires GLIBC symbols through **2.28**; candidate requires **2.34** and `libc.so.6`. This excludes older glibc hosts still relevant to .NET 8's OS contract unless a compatible native build is selected. No old-glibc host was executed; Ubuntu success cannot falsify the requirement. Qualify a supported official build on minimum/current OSes rather than silently raising support minimums.
- **Engine direction:** the newer ORM currently selects an older SQLite engine. The narrow unchanged `Banana` schema interoperates in both directions, but this is not arbitrary database downgrade approval. Review upstream engine changes/advisories and test the chosen native version before adoption.
- **Initializer/API:** both ORM versions initialize from the `SQLiteConnection` static constructor. The candidate embeds an internal `Batteries_V2` calling `raw.SetProvider`; it no longer depends on bundle_green. Its iOS-specific asset (`net8.0-ios18.0`) selects system `provider.sqlite3`; other groups use e_sqlite3/SourceGear. Library-used SQLite APIs compile and untrimmed tests pass, but consumers explicitly calling the old public bundle initializer need compatibility review. Keep all 2.x bundle references out of the candidate.
- **Trimming/AOT:** candidate compilation reports **IL2026 at SQLite/Barrel.cs:227**, `InsertOrReplace(object)` requiring preservation of the object's members. Do not suppress it. Qualify a compatible typed/preservation approach with real trimmed/AOT schema/serialization/persistence tests before changing production code. Existing default CS1591 warnings remain.
- **Other native assets:** SourceGear includes Windows x86/x64/arm64, Android arm/arm64/x86/x64, iOS arm/arm64 and arm64/x64 simulators, macOS/Mac Catalyst arm64/x64, glibc/musl Linux variants and browser net8/net9 archives. Package presence is not OS-minimum, linking, AOT or runtime proof; browser/extra RIDs do not become new supported Monkey Cache targets. Preserve existing iOS minimums pending asset-selection and system-SQLite qualification.
- **License review:** sqlite-net's included license is MIT-style (retain copyright/permission notice); SQLitePCLRaw core/provider declare Apache-2.0 (license/notice obligations); SourceGear's included `LICENSE.txt` says SQLite is public domain and links its copyright statement. The downloaded package does not impose a paid-service requirement. Retain relevant notices and review distribution obligations; this is package inspection, not legal clearance.

No production SQLite defect was reproduced by this matrix. Change the hold recommendation only after full candidate NuGet integration, an acceptable native engine/ABI choice, trimming and all preserved platform/minimum-OS contracts pass. Linux binary success alone does not close these gates or issues #150/#151.

## Historical samples are not modern supported targets

The old Xamarin.Android (`v12.0`), Xamarin.iOS, UWP (19041/min16299), shared `netstandard2.0` Xamarin.Forms and ASP.NET Core `netcoreapp2.0`/Ooui projects are **reference-only**, by the owner's selected policy. They do not represent supported current-source `.NET 8` library targets: their old frameworks cannot consume the current project references. The v1 README support table is historical and includes .NET Standard 2.0/.NET Framework 4.6.1/Xamarin.Mac, not current assets. None is deleted or represented by a misleading green modern CI job.

Exact source inventory (`src/` prefix; `MC` below means **all four** core/FileStore/SQLite/LiteDB project references):

| Project | Framework / OS declaration | Direct package declarations | Project references |
| --- | --- | --- | --- |
| `MonkeyCache.TestApp` | `netstandard2.0` | Xamarin.Forms 2.5.0.122203; LiteDB 5.0.19; sqlite-net-pcl 1.9.172 | MC |
| `MonkeyCache.TestApp.Android` | Xamarin targets, `TargetFrameworkVersion=v12.0`; manifest min API 16 / target 27 | Xamarin.Forms 2.5.0.122203; LiteDB 5.0.13; sqlite-net-pcl 1.8.116; Android Support Design/v4/v7.AppCompat/v7.CardView/v7.MediaRouter **25.4.0.2 each** | MC; no shared TestApp project reference |
| `MonkeyCache.TestApp.iOS` | Xamarin.iOS targets; plist minimum **11.2**; x86_64 simulator / ARM64 device | Xamarin.Forms 2.5.0.122203; LiteDB 5.0.13; sqlite-net-pcl 1.8.116. Separate `packages.config`: Xamarin.Forms 2.4.0.266-pre1 / `xamarinios10` | MC + shared TestApp |
| `MonkeyCache.TestApp.UWP` | UAP target 10.0.19041.0 / minimum 10.0.16299.0 | Microsoft.NETCore.UniversalWindowsPlatform 6.2.9; Xamarin.Forms 2.5.0.122203; LiteDB 5.0.13; sqlite-net-pcl 1.8.116 | MC; no shared TestApp project reference |
| `MonkeyCache.TestApp.AspNetCore` | `netcoreapp2.0` | Microsoft.AspNetCore.All 2.0.9; Ooui/Ooui.AspNetCore/Ooui.Forms **0.10.222 each**; Xamarin.Forms 2.5.0.122203; LiteDB 5.0.13; sqlite-net-pcl 1.8.116 | MC |

These are declarations, not newly restored dependency graphs. Nested `<Version>` elements are included. Do not interpret the iOS packages.config entry as its resolved package version. Android/UWP startup additionally references the shared `App` class without the corresponding project link. Adding that link alone would not repair the incompatible provider target graph. Their older direct database package pins also need reconciliation with current providers' LiteDB 5.0.21 / sqlite-net-pcl 1.9.172 requirements after any future target migration. No historical sample was restored, compiled or run by this batch.

The current `src/MonkeyCache.sln` has an empty `TestApps` solution folder, not registrations for these sample projects. Use the README's explicit portable solution/consumer commands; neither opening this solution nor installing more mobile workloads makes the old sample project references compatible. `tests/MonkeyCache.PlatformBuild` is a class-library build fixture, and `tests/MonkeyCache.MauiConsumer` is an Essentials API console consumer, not a replacement MAUI UI sample.

Historical dependency inventory (prior feed observations, not freshly restored or updated): Xamarin.Forms 2.5.0.122203 (and iOS packages.config 2.4.0.266-pre1) -> 5.0.0.2662; Android Support Design/v4/AppCompat/CardView/MediaRouter 25.4.0.2 -> 28.0.0.3; UWP 6.2.9 -> 6.2.15; Microsoft.AspNetCore.All 2.0.9 -> 2.2.8; Ooui/Ooui.AspNetCore/Ooui.Forms 0.10.222 -> 0.14.16; sample LiteDB 5.0.13/5.0.19 -> 5.0.21 and sqlite-net-pcl 1.8.116 -> 1.11.285 (other samples 1.9.172). These reference-only sources are not an active dependency-update lane. Bumping packages cannot repair their target graph or restore vendor support.

### Chosen support policy

| Decision / boundary | Compatibility cost |
| --- | --- |
| **Selected: reference-only legacy samples; add modern examples; retain .NET 8 libraries** | Historical source is kept, not maintained as runnable current examples. The small [console and MAUI UI examples](../samples/README.md) are separate from the diagnostic consumers; their distinct qualification limits are explicit. |
| Active legacy Xamarin/UWP/ASP.NET 2 support is not selected | Would require a serviced legacy package/target line, old toolchains, serialization/API/dependency backports and native/signing evidence. Current net8 binaries cannot satisfy those old project references. |
| No in-place migration of old apps/data is implied | Forms UI/startup to MAUI, UWP identity to WinUI and ASP.NET 2/Ooui hosting rewrites still need separately scoped work. Preserve or explicitly migrate application IDs/cache paths; new example identities do not migrate existing caches. |

For library support, **retain the existing net8 contracts while qualifying current .NET 10 consumers**, as selected by the owner. Any additional native net10 assets remain separate scope. A net10-only library line would exclude net8/9 consumers and still requires a breaking-version/support decision; adding net10 alongside net8 expands package/CI/AOT/native workload coverage. No library target change is made here.

Vendor lifecycle is distinct from library compatibility: first-party policy checked 2026-10-31 gives .NET 8/9 end **2026-11-10**, .NET 10 **2028-11-14**, and MAUI 10 **2027-05-11** (MAUI 8/9 already ended). Running net8-targeted library assets inside a .NET 10 app is not running the EOL .NET 8 runtime, but does not by itself establish native API compatibility. Any promised maintenance beyond vendor lifecycles must explicitly cover dependency servicing, frozen compatibility lanes and security expectations; do not call those targets vendor-supported.

## Finite ordered continuation backlog

1. **Native acceptance**: run the configured Windows/Android/Apple builds in authorized placements; retain exact assets/binlogs. Windows cross-compilation is verified locally; Android workload install succeeds but SDK installation is blocked by the installer manifest endpoint/network in this placement. Resolve environment prerequisites, not assertions. Separately execute both MAUI Windows identity modes before any #150/#151 resolution claim.
2. **Modern examples/native consumers**: build and launch the new Windows MAUI UI example on Windows, separately from the Essentials API diagnostic consumer. The new `maui-example` job installs only `maui-windows` from workload set 10.0.403 and builds Controls 10.0.120; it has not run remotely or locally. Complete Android/Apple examples only with suitable SDK/Xcode/host evidence. Preserve the six portable example CI invocations and existing regression lanes; UI/build-only examples do not replace packaged/unpackaged or persistence matrices.
3. **SQLite major remaining gates**: Linux source/binary tests, controlled old/new database interchange and package/license inventory are complete for the measured minimum graph. Next run the normal NuGet lane with sufficient disk, evaluate latest official native ABI/engine options (including 3.53.4), resolve IL2026 without suppressions, then qualify retained native OS/architecture/minimums. Keep the default graph until those gates pass; do not repeat the completed Linux matrix as a substitute.
4. **Selected support policy**: keep historical samples reference-only and retain net8 library contracts; this decision is complete, not a pending question. Native net10 library assets or in-place legacy app/data migration are not authorized by that choice. Vendor lifecycle dates above still apply.
5. **Release gates**: full-platform packaging, source/API/storage-format review, trimming/AOT, current/minimum OS/architecture coverage, Azure pipeline health and explicit publication authorization. Existing CS0108/CS1591 and Windows CS0168 warnings remain visible.

Continue actionable items in order; weekly read-only watermark comparison is only a quiet-period backstop, not a replacement for backlog work.

## First-party sources

- NuGet version/metadata endpoints: `https://api.nuget.org/v3-flatcontainer/<id>/index.json` and `<version>/<id>.nuspec`; exact response-derived inventory is in the handoff.
- [sqlite-net candidate source/initializer](https://github.com/praeclarum/sqlite-net/tree/21e525c7825f8947047c36b3b3a81f70d5710d33/nuget/SQLite-net-std), [Raw 3.0.3 source](https://github.com/ericsink/SQLitePCL.raw/tree/f3f967adafad853375597a6387cacb7573c5b343), [SourceGear native source](https://github.com/sourcegear/sqlite-builds/tree/21770fb613c0513b35289e82e2182e5f5e7e17e1), [.NET 8 supported OSes](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md), [SQLite copyright](https://sqlite.org/copyright.html).
- [.NET release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json), [.NET support lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [MAUI support](https://dotnet.microsoft.com/platform/support/policy/maui), [SDK-bundled SourceLink](https://learn.microsoft.com/dotnet/core/compatibility/sdk/8.0/source-link).
- [Retained Apple SDK / Xcode 16.0](https://github.com/dotnet/macios/releases/tag/dotnet-8.0.1xx-xcode16.0-8319), [current Apple SDK](https://github.com/dotnet/macios/releases/tag/dotnet-10.0.1xx-xcode27.1-10018), [current Android](https://github.com/dotnet/android/releases/tag/36.1.118).
- [macOS 15 runner software](https://github.com/actions/runner-images/blob/main/images/macos/macos-15-Readme.md), [macOS 26](https://github.com/actions/runner-images/blob/main/images/macos/macos-26-Readme.md), [Ubuntu 24.04](https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2404-Readme.md), [Windows 2025](https://github.com/actions/runner-images/blob/main/images/windows/Windows2025-Readme.md).
