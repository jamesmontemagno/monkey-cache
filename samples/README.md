# Modern examples

These examples consume the retained current-source .NET 8 libraries, not a new NuGet release. They do not replace `tests/MonkeyCache.Regression` or the native #150/#151 protocol. Legacy `src/MonkeyCache.TestApp*` sources are reference-only; no legacy framework, library target, OS minimum or cache format has been changed.

## Portable console

From the repository root, with the SDK selected by `global.json` and .NET 8/10 runtimes:

```sh
dotnet run --project samples/MonkeyCache.Console -f net8.0 -c Release -p:MonkeyCachePortable=true -- filestore
dotnet run --project samples/MonkeyCache.Console -f net10.0 -c Release -p:MonkeyCachePortable=true -- sqlite
```

Choose `filestore`, `sqlite` or `litedb` on either runtime. Each invocation uses one provider in a new process with a unique temporary base directory, printed at startup. It does not open an existing application's cache or delete directories. Example entries are removed, but backend files/directories remain for inspection. Invalid arguments return exit code 2; failed cache checks throw and produce a nonzero exit.

`Shared/CacheWalkthrough.cs` is linked into both examples. Its eight checks demonstrate generated JSON serialization, add/read, a fresh entry, ETags, expiration with `AutoExpire`, expired cleanup preserving fresh data, and explicit removal. A negative expiration makes the stale entry deterministic without sleeping. This is a small walkthrough, not a duplicate process/persistence diagnostic controller. `EmptyExpired` affects the example's own cache; do not transplant it into unrelated stores without considering ownership.

The console sets `BarrelUtils.SetBaseCachePath` and the chosen provider's `ApplicationId` before accessing `Current`. Its temporary cache is intentionally isolated and is not a production cache-path recommendation. SQLite uses the unchanged default 1.9.172/Raw 2.x graph; no candidate flag is needed.

## Windows MAUI UI

`MonkeyCache.Maui` is a minimal **actual MAUI Controls application**, not the existing Essentials API console consumer. This first example is Windows-only to bound native scaffolding; it neither removes nor promises additional library platforms. It has a WinUI entry point, `MauiProgram`, `Application.CreateWindow` and a button that runs the same caching walkthrough using FileStore.

`MauiProgram.CreateMauiApp` sets the MAUI cache directory and a stable example-only cache application ID **before** building services or constructing pages. The first `Barrel.Current` occurs on button click. Do not add a static/service initializer that accesses it earlier. No fallback rewrite or catch masks startup/cache exceptions. Example initialization is once per process. No custom fonts/icons, network, signing identity or diagnostic stubs are required.

On a suitably provisioned Windows host, from the repository root:

```powershell
dotnet --version # 10.0.403 for the configured CI workload set
dotnet workload install maui-windows --version 10.0.403
dotnet build samples/MonkeyCache.Maui -c Release -p:MonkeyCachePlatform=net8.0-windows10.0.19041
dotnet run --project samples/MonkeyCache.Maui -c Release --no-build -p:MonkeyCachePlatform=net8.0-windows10.0.19041
```

Use the matching Windows App Runtime/WinUI prerequisites. Press **Run cache example** and expect eight PASS lines; press again to verify repeat use in the same UI process. Cache/startup failures must remain visible, not be interpreted as a successful run. Inspect the cache under MAUI's actual `FileSystem.Current.CacheDirectory` plus `MonkeyCache.Maui.Example/MonkeyCacheFS`. This intentionally uses the existing explicit-base-path API; it is not testing the library's default Windows resolver. Never use this example identity/path as an implicit migration of an old app.

The project uses `UseMaui=true`, Controls **10.0.120**, net10 Windows target **19041**, minimum **17763**, win-x64 and unpackaged deployment. These are example requirements, not changes to the net8 Windows library minimum **16299**. Workload set SDK **10.0.403** includes MAUI **10.0.110**; the explicit Controls package selects **10.0.120**. Their full build-task/UI compatibility requires the configured Windows build and launch, not the prior Essentials-only results. No trimming/resource processing is disabled. The minimal Windows bootstrap follows the [official 10.0.120 template structure](https://github.com/dotnet/maui/tree/10.0.120/src/Templates/src/templates/maui-mobile).

## Qualification and CI

| Surface | Configured | Restored | Compiled | Runtime |
| --- | --- | --- | --- | --- |
| Console net8/net10, all three providers | Six commands in existing Linux/Windows/macOS portable CI | Locally from retained cache, empty package source | Both TFMs on Linux | **6 processes / 48 checks passed** on Linux; other OSes not run |
| Shared walkthrough | Linked source, generated JSON | Via console | Both console targets | Same 48 checks; not native MAUI |
| MAUI UI Windows | Separate build-only job in `platform-builds.yml` | **Not here** | **Not here** | **Not launched** |
| Packaged MAUI / Android / Apple UI | Not scaffolded in this bounded example | No | No | No |

Local .NET SDKs/runtimes were retained; no workload or package downloads were performed. MAUI Controls/workload and Windows execution are unavailable in this Linux placement, and disk fell from about 71 MiB to 2.6 MiB after the cache-only portable build. XML/C# source/configuration inspection is not a successful MAUI build. Native Windows validation must check real early startup, paths, minimum OS, build assets and repeated button use; signed/packaged activation is a separate fixture/protocol gate. Neither this example nor Linux checks reproduce or close #150/#151. No remote CI was dispatched.

The local `--no-restore` MAUI build probe stopped at **NETSDK1147** (the Linux SDK's suggested workload was `maui-tizen`). That suggestion is not a supported Windows cross-build recipe: no Tizen target/workload was added or installed. The configured Windows lane explicitly uses `maui-windows`; Windows workload restoration, compilation and UI launch remain unverified.
