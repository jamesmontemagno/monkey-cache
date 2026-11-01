# 🐒Cache

> Choose a provider package (FileStore, SQLite, or LiteDB), not just the core package. Published NuGet 2.1.1 targets .NET 6; 3.0.0-beta and current source target .NET 8. Source changes and portable test results are not a new release or full platform qualification.

Start with the [modern examples](samples/README.md): a runnable .NET 8/10 console walkthrough and a minimal Windows MAUI UI example with explicit native qualification limits. Historical `src/MonkeyCache.TestApp*` projects are **reference-only**, not runnable current-source examples. Their sources remain intact; [the chosen policy and exact inventory](eng/README.md#historical-samples-are-not-modern-supported-targets) retain .NET 8 library contracts without promising legacy application support.

Easily cache any data structure for a specific amount of time in any .NET application.

Monkey Cache is comprised of one core package (MonkeyCache) and three providers which reference the core package as a dependency. At least one provider must be installed for Monkey Cache to work and each offer the same API (IBarrel). Depending on your existing application you may already have SQLite or LiteDB installed so these would be your natural choice. A lightweight file based Monkey Cache is also provided if you aren't already using one of these options.

Listen to our podcast [Merge Conflict: Episode 76](http://www.mergeconflict.fm/76) for an overview of Monkey Cache and it's creation.

A full breakdown of performance can be found in the performance.xlsx. When dealing with a small amount of records such as inserting under 50 records, the performance difference between each provider is negligible and it is only when dealing with a large amount of records at a single time that you should have to worry about the provider type.

## Azure DevOps

You can follow the full project here: https://dev.azure.com/jamesmontemagno/MonkeyCache

**Build Status**: ![](https://jamesmontemagno.visualstudio.com/_apis/public/build/definitions/00ee1525-d4f2-42b3-ab63-16f5d8b8aba0/6/badge)

## NuGets

|Name|Description|NuGet|
| ------------------- | -------- | :------------------: |
|🐒 MonkeyCache|Contains base interfaces and helpers|[![NuGet](https://img.shields.io/nuget/v/MonkeyCache.svg?label=NuGet)](https://www.nuget.org/packages/MonkeyCache/)|
|🙊 MonkeyCache.SQLite|A SQLite backing for Monkey Cache|[![NuGet](https://img.shields.io/nuget/v/MonkeyCache.SQLite.svg?label=NuGet)](https://www.nuget.org/packages/MonkeyCache.SQLite/)|
|🙉 MonkeyCache.LiteDB|A LiteDB backing for Monkey Cache|[![NuGet](https://img.shields.io/nuget/v/MonkeyCache.LiteDB.svg?label=NuGet)](https://www.nuget.org/packages/MonkeyCache.LiteDB/)|
|🙈 MonkeyCache.FileStore|A local file based backing for Monkey Cache|[![NuGet](https://img.shields.io/nuget/v/MonkeyCache.FileStore.svg?label=NuGet)](https://www.nuget.org/packages/MonkeyCache.FileStore/)|
|Development Feed| |[MyGet](http://myget.org/F/monkey-cache)|


## Package targets and platform qualification

| Package/source | Provider target | Core targets |
| --- | --- | --- |
| Published NuGet 2.1.1 (GitHub release tag `v2.1.1.3`) | .NET 6 | .NET 6, Android, iOS, Mac Catalyst, Windows |
| Published NuGet 3.0.0-beta / current source | .NET 8 | .NET 8, Android, iOS, Mac Catalyst, Windows |

The Windows core target is `windows10.0.19041`; its declared minimum is `10.0.16299.0`. These are different from an application's SDK target and minimum OS requirements. For example, the .NET MAUI 9 template targets Windows SDK 19041 with minimum OS 17763. Modern MAUI Windows uses WinUI, not UWP.

Compatible newer .NET applications can consume older library targets. Portable tests and fresh-process provider checks run on .NET 8 and .NET 10 without changing library targets. They do **not** qualify MAUI Windows packaged/unpackaged behavior, mobile workloads, trimming/AOT, or every supported OS/architecture.

As of October 2026, .NET 8 and .NET 9 reach end of support on November 10, 2026; .NET 10 is LTS. MAUI has a separate lifecycle: MAUI 8 and 9 are out of support, while MAUI 10 is supported. Historical targets are retained for compatibility, not a promise of continued vendor support. Consult the [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) and [MAUI support policy](https://dotnet.microsoft.com/platform/support/policy/maui) before choosing an application toolchain.


## Platform Support - v1.X

This historical table describes v1 packages, not the current source or a working sample matrix. That generation used .NET Standard 2.0 with platform-specific cache-directory handling.

|Platform|Version|
| ------------------- | :------------------: |
|Xamarin.iOS|iOS 7+|
|Xamarin.Mac|All|
|Xamarin.Android|API 14+|
|Windows 10 UWP|10.0.16299+|
|.NET Core|2.0+|
|ASP.NET Core|2.0+|
|.NET Framework|4.6.1+|

## Setup

First, select an implementation of **Monkey Cache** that you would like (LiteDB, SQLite, or FileStore). Install the specific NuGet for that implementation, which will also install the base **MonkeyCache** library. Installing **MonkeyCache** without an implementation will only give you the high level interfaces. 

It is required that you set an ApplicationId for your application so a folder is created specifically for your app on disk. This can be done with a static string on Barrel before calling ANY method:

```csharp
Barrel.ApplicationId = "your_unique_name_here";
```

The first `Barrel.Current` access constructs the store, even in an expression such as `Barrel.Current.EmptyExpired()`. Configure paths and IDs before DI singleton creation or any cache access. The base path is shared by providers, while each provider has its own ApplicationId. Use distinct directories if using SQLite and LiteDB together: both name their database `Barrel.db`.

### Windows packaged and unpackaged applications

New MAUI 9 Windows projects default to unpackaged deployment. Published MonkeyCache 2.1.1 calls `Windows.Storage.ApplicationData.Current` for its default path; a process without package identity can fail there during initialization. Current source and 3.0.0-beta already contain a fallback to `Environment.SpecialFolder.LocalApplicationData`. This is shared initialization behavior, not unique to FileStore.

Issues [#150](https://github.com/jamesmontemagno/monkey-cache/issues/150) and [#151](https://github.com/jamesmontemagno/monkey-cache/issues/151) remain **native-runtime unverified** by this maintenance work. Their exact package/toolchain versions were not supplied. A debugger first-chance exception is not by itself an escaped application failure.

An application-level path override can avoid the default lookup:

```csharp
// In a MAUI app, once, before any provider is initialized:
MonkeyCache.BarrelUtils.SetBaseCachePath(Microsoft.Maui.Storage.FileSystem.CacheDirectory);
MonkeyCache.FileStore.Barrel.ApplicationId = "your_unique_name_here";
```

MAUI's Windows CacheDirectory handles packaged and unpackaged paths differently. Alternatively, use `Barrel.Create(absoluteWritableDirectory)` and retain the returned `IBarrel`; FileStore and SQLite `Create` do not replace `Current`. Choose an app-owned path and preserve it across restarts. Changing paths can make existing cache data inaccessible; no automatic migration is performed.

If the application needs packaged deployment, remove `<WindowsPackageType>None</WindowsPackageType>` and use `"commandName": "MsixPackage"` in the relevant launch profiles. Unpackaged profiles use `None` and `"Project"`. See [Microsoft's deployment guidance](https://learn.microsoft.com/dotnet/maui/windows/setup). Bootstrap/runtime dependencies and package identity are separate requirements: delaying cache access or raising the OS target does not grant package identity. These controls still need qualification on the application's actual Windows environment; switching providers or upgrading a package alone is not a verified fix for those issues.

### LiteDB Encryption
LiteDB offers [built in encryption support](https://github.com/mbdavid/LiteDB/wiki/Connection-String), which can be enabled with a static string on Barrel before calling ANY method. You must choose this up front before saving any data.

```csharp
Barrel.EncryptionKey = "SomeKey";
```

### LiteDB Upgrade (4 -> 5) | NuGet 1.3 -> 1.5

If you are upgrading from 1.3 to 1.5 of the NuGet package it also went through an upgrade of LiteDB.

You may need to do an upgrade of the database before using it. In your project you can set the folowing after setting your EncryptionKey or Applicationid:

```csharp
Barrel.Upgrade = true;
```

### What is Monkey Cache?

The goal of Monkey Cache is to enable developers to easily cache any data for a limited amount of time. It is not Monkey Cache's mission to handle network requests to get or post data, only to cache data easily.

All data for Monkey Cache is stored and retrieved in a Barrel. 

For instance you are making a web request and you get some `json` back from the server. You would want the ability to cache this data in case you go offline, but also you need it to expire after 24 hours.

That may look something like this:

```csharp
async Task<IEnumerable<Monkey>> GetMonkeysAsync()
{
    var url = "http://montemagno.com/monkeys.json";

    //Dev handle online/offline scenario
    if (!CrossConnectivity.Current.IsConnected)
    {
        return Barrel.Current.Get<IEnumerable<Monkey>>(key: url);
    }
    
    //Dev handles checking if cache is expired
    if (!Barrel.Current.IsExpired(key: url))
    {
        return Barrel.Current.Get<IEnumerable<Monkey>>(key: url);
    }

    var client = new HttpClient();
    var monkeys = await client.GetFromJsonAsync<IEnumerable<Monkey>>(url);

    //Saves the cache and pass it a timespan for expiration
    Barrel.Current.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));
    return monkeys;
}
```

Ideally, you can make these calls extremely generic and just pass in a string:

```csharp
public async Task<T> GetAsync<T>(string url, int days = 7, bool forceRefresh = false)
{
    if (!CrossConnectivity.Current.IsConnected)
        return Barrel.Current.Get<T>(url);

    if (!forceRefresh && !Barrel.Current.IsExpired(url))
       return Barrel.Current.Get<T>(url);

    try
    {
        T result = await httpClient.GetFromJsonAsync<T>(url);
        Barrel.Current.Add(url, result, TimeSpan.FromDays(days));
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unable to get information from server {ex}");
        //probably re-throw here :)
    }

    return default;
}
```

MonkeyCache will never delete data unless you want to, which is pretty nice incase you are offline for a long period of time. However, there are additional helpers to clean up data:

```csharp
    //removes all data
    Barrel.Current.EmptyAll();
	
	//removes all expired data
    Barrel.Current.EmptyExpired();

    //param list of keys to flush
    Barrel.Current.Empty(key: url);
```

The above shows how you can integrate Monkey Cache into your existing source code without any modifications to your network code. However, MonkeyCache can help you there too! MonkeyCache also offers helpers when dealing with network calls via HttpCache.

HttpCache balances on top of the Barrel and offers helper methods to pass in a simple url that will handle adding and updating data into the Barrel based on the ETag if possible.

```csharp
Task<IEnumerable<Monkey>> GetMonkeysAsync()
{
    var url = "http://montemagno.com/monkeys.json";

    //Dev handle online/offline scenario
   
	var result = await HttpCache.Current.GetCachedAsync(barrel, url, TimeSpan.FromSeconds(60), TimeSpan.FromDays(1));
    return JsonConvert.DeserializeObject<IEnumerable<Monkey>>(result);
}
```

Unless an explicit path is supplied, the core chooses a platform-specific base and appends `ApplicationId`. FileStore adds `MonkeyCacheFS`; SQLite and LiteDB add `MonkeyCache/Barrel.db`.

|Platform|Location|
| ------------------- | :------------------: |
|iOS / Mac Catalyst|`NSSearchPath` CachesDirectory|
|Android|`Application.Context.CacheDir.AbsolutePath`|
|Windows with package identity|`Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path`|
|Windows fallback (current source / 3.0.0-beta)|`Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`|
|Portable .NET|`Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`|

This table describes modern source, not the historical v1 implementation. Portable and Windows fallback directories do not necessarily have the OS cleanup behavior of mobile cache directories.


#### Persisting Data Longer
Since the default is to use the Cache directories the platform can clean this up at any time.  If you want to change the base path of where the data is stored you can call the following static method:

```csharp
BarrelUtils.SetBaseCachePath("Path");
```

You MUST call this before initializing or accessing anything in the Barrel, and it can only ever be called once else it will throw an `InvalidOperationException`.

FileStore persists UTC expiration as integer epoch seconds (one-second precision). Current source reads legacy 32-bit timestamps and writes 64-bit values to preserve dates beyond 2038 and extreme expiration intervals. The tab-separated index layout is unchanged, but older FileStore versions cannot read newly written timestamps outside their 32-bit range. Do not downgrade a cache requiring those dates without accounting for that limitation.

#### Json Serialization

MonkeyCache v2.0 and higher uses System.Text.Json to serialize objects to/from the backing store. By default, the default System.Text.Json serialization behavior is used. There are two options for controlling this serialization:

1. Pass an optional [JsonSerializationOptions](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance to `Barrel.Current.Add` and `Barrel.Current.Get`.
2. Pass a `JsonTypeInfo<T>` instance to `Barrel.Current.Add` and `Barrel.Current.Get`. You can get a `JsonTypeInfo<T>` instance by using the System.Text.Json source generator. See [How to use source generation in System.Text.Json](https://docs.microsoft.com/dotnet/standard/serialization/system-text-json-source-generation) for more information.

No matter which option you choose, it is recommended to use the same option between `Barrel.Current.Add` and `Barrel.Current.Get`. If the options are inconsistent, the information going into the backing store may not be read properly when retrieving it back from the Barrel.

## Building and testing current source

Use the .NET 10 SDK selected by `global.json`, plus .NET 8 and .NET 10 runtimes. Library targets remain .NET 8; the development SDK requirement does not make consumers .NET 10-only.

From the repository root:

```sh
python3 eng/test-tooling.py
dotnet test src/MonkeyCache.Portable.slnx -p:MonkeyCachePortable=true --configuration Release
dotnet run --project tests/MonkeyCache.Regression -f net8.0 -c Release -p:MonkeyCachePortable=true
dotnet run --project tests/MonkeyCache.Regression -f net10.0 -c Release -p:MonkeyCachePortable=true
bash eng/smoke-packages.sh
```

The Python command uses only the standard library and synthetic process/argument/NuGet-response fixtures. It tests tooling wiring, not real restores or native platforms. The shell/Python scripts are invoked through their interpreters; executable file bits are not required.

| Current consumer entrypoint | Purpose and execution boundary |
| --- | --- |
| `samples/MonkeyCache.Console` | Small add/read/expiration example for all three default providers on .NET 8/10; [run commands](samples/README.md#portable-console) |
| `samples/MonkeyCache.Maui` | Actual Windows MAUI UI startup example; source/configuration checked here, **not built or launched locally**; [native prerequisites](samples/README.md#windows-maui-ui) |
| `tests/MonkeyCache.Regression` commands above | Executable provider/configuration/persistence examples on .NET 8 and 10; portable assets, not a UI application |
| `dotnet run --project tests/MonkeyCache.MauiConsumer -c Release -p:MonkeyCachePortable=true` | .NET 10 Essentials API consumer; portable lane previously executed, not MAUI Controls/native UI qualification |
| `tests/MonkeyCache.PlatformBuild` | Native class-library **build-only** consumer; use the exact SDK/workload commands in [eng/README.md](eng/README.md#current-source-ci-matrix), not `dotnet run` |
| `eng/qualify-sqlite.py` | Opt-in candidate regression controller, not an application sample; default SQLite graph remains held. See [qualification modes and limits](eng/README.md#opt-in-sqlite-major-qualification). |

`MonkeyCachePortable=true` selects only the core's existing `net8.0` target and disables automatic package generation. It does not override test targets or skip assertions. Both test runtimes run by default; `-f net8.0` or `-f net10.0` can select a specific test runtime. Omitting the property preserves the full platform target list and requires the matching mobile workloads (and a Windows host for the Windows target).

Provider suites use a unique temporary base directory per test process. HTTP tests serve deterministic responses over loopback, with no external service dependency after restore. The regression executable runs each provider/scenario in a fresh child process and checks configuration, independent instances, serialization, expiration, cleanup and persistence across writer/reader processes. Fixture data remains in the printed temporary locations for inspection; tests do not clean arbitrary application caches.

The smoke script uses a separate package consumer directory and a unique `0.0.0-local.*` version. It packs core and all providers, then runs the same process regressions against **packages**, not project references, on both runtimes. Portable pack requires explicit `AllowPortablePackage=true`; these packages omit platform assets and **must not be published**. Full release packaging still requires native/platform builds, supported-OS checks and separate release authorization.

Test tooling uses Test SDK 18.11.0 and MSTest 4.5.2, with the same assertions exercised on both runtimes. LiteDB stays at the current stable 5.0.21. SQLite retains sqlite-net-pcl 1.9.172 with the latest compatible `bundle_green` 2.1.11, and explicit desktop/Android native 2.1.14 packages. This avoids relying on the older native versions selected by the ORM, but does not establish native runtime qualification. A sqlite-net/SQLitePCLRaw major upgrade requires separate API, existing-database and native/platform validation. The obsolete SourceLink 1.1.1 references are removed: .NET SDK 8 and later include SourceLink.

The GitHub Actions configurations add portable checks on Linux, Windows and macOS, plus explicit native-target **build-only** checks. They do not replace the linked Azure DevOps release process. See [the toolchain matrix, held dependencies and finite qualification backlog](eng/README.md). Local configuration is not evidence that remote CI has run.

### FAQ

Have questions? Open up an issue. Here are a few:

#### How does Monkey Cache differ from the Settings Plugin?
Great question. I would also first say to read through this: https://github.com/jamesmontemagno/SettingsPlugin#settings-plugin-or-xamarinforms-appproperties as it will compare it to app.properties.

So with the Settings Plugin it is storing properties to the users local preferences/settings api of each platform. This is great for simple data (bool, int, and small strings). Each platform has different limitations when it comes to these different types of data and strings especially should never house a large amount of data. In fact if you try to store a super huge string on Android you could easily get an exception.

Monkey Cache enables you to easily store any type of data or just a simple string that you can easily serialize and deserialize back and forth. The key here is that you can set an expiration data associated with that data. So you can say this data should be used for the next few days. A key here is the ETag that is extra data that can be used to help with http caching and is used in the http caching library.


#### Isn't this just Akavache?
Akavache offers up a great and super fast asynchronous, persistent key-value store that is based on SQLite and Reactive Extensions. I love me some Akavache and works great for applications, but wasn't exactly what I was looking for in a data caching library. Akavache offers up a lot of different features and really cool Reactive type of programming, but Monkey Cache focuses in on trying to create a drop dead simple API with a focus on data expiration. My goal was also to minimize dependencies on the NuGet package, which is why Monkey Cache offers a SQLite, LiteDB, or a simple FileStore implementation for use.

#### How about the link settings?

`--linkskip=SQLite-net` is historical Xamarin linker guidance, not a current .NET/MAUI recommendation. Do not disable linking/trimming to claim compatibility. Generated JSON metadata covers serialization, not every reflected database member; current trimming/AOT and candidate SQLite IL2026 qualification remain separate [release gates](eng/README.md#adoption-blockers-and-falsification-criteria).


#### Where Can I Learn More?
Listen to our podcast [Merge Conflict: Episode 76](http://www.mergeconflict.fm/76) for an overview of Monkey Cache and it's creation.

### License
Under MIT (see license file)

### Want To Support This Project?
All I have ever asked is to be active by submitting bugs, features, and sending those pull requests down! Want to go further? Make sure to subscribe to my weekly development podcast [Merge Conflict](http://mergeconflict.fm), where I talk all about awesome Xamarin goodies and you can optionally support the show by becoming a [supporter on Patreon](https://www.patreon.com/mergeconflictfm).
