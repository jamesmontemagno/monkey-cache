# 🐒Cache
Easily cache any data structure for a specific amount of time in any .NET application.

MonkeyCache is comprised of one core package (MonkeyCache) and three providers which reference the core package as a dependency. At least one provider must be installed for MonkeyCache to work and each offer the same API (IBarrel). Depending on your existing application you may already have SQLite or LiteDB installed so these would be your natural choice. A light weight file based MonkeyCache is also provided. A full breakdown of performance can be found in the performance.xlsx. When dealing with small amount of records such as inserting under 50 records the performance difference between each provider is negligible and it is only when dealing with a large amount of records at a single time should you have to worry about the provider type.

**Build Status**: ![](https://jamesmontemagno.visualstudio.com/_apis/public/build/definitions/00ee1525-d4f2-42b3-ab63-16f5d8b8aba0/4/badge)

**NuGets**

|Name|Info|
| ------------------- | :------------------: |
|🐒 MonkeyCache|[![NuGet](https://img.shields.io/nuget/v/MonkeyCache.svg?label=NuGet)](https://www.nuget.org/packages/MonkeyCache/)|
|🙊 MonkeyCache.SQLite|[![NuGet](https://img.shields.io/nuget/v/MonkeyCache.SQLite.svg?label=NuGet)](https://www.nuget.org/packages/MonkeyCache.SQLite/)|
|🙉 MonkeyCache.LiteDB|[![NuGet](https://img.shields.io/nuget/v/MonkeyCache.LiteDB.svg?label=NuGet)](https://www.nuget.org/packages/MonkeyCache.LiteDB/)|
|🙈 MonkeyCache.FileStore|[![NuGet](https://img.shields.io/nuget/v/MonkeyCache.FileStore.svg?label=NuGet)](https://www.nuget.org/packages/MonkeyCache.FileStore/)|
|Development Feed|[MyGet](http://myget.org/F/monkey-cache)|

**Platform Support**

MonkeyCache is a .NET Standard 2.0 library, but has some platform specific tweaks for storing data in the correct Cache directory.

|Platform|Version|
| ------------------- | :------------------: |
|Xamarin.iOS|iOS 7+|
|Xamarin.Mac|All|
|Xamarin.Android|API 14+|
|Windows 10 UWP|10.0.16299+|
|.NET Core|2.0+|
|ASP.NET Core|2.0+|
|.NET|4.6.1+|

## Setup

It is required that you set an ApplicationId for your application so a folder is created specifically for your app on disk. This can be done with a static string on Barrel before calling ANY method:

```
Barrel.ApplicationId = "your_unique_name_here";
```


### What is Monkey Cache?

The goal of MonkeyCache is to enable developers to easily cache any data for a limited amount of time. It is not MonkeyCache's mission to handle network requests to get or post data, only to cache data easily.

All data for MonkeyCache is stored and retrieved in a Barrel. 

For instance you are making a web request and you get some `json` back from the server. You would want the ability to cache this data incase you go offline, but also you need it to expire after 24 hours.

That may look something like this:

```csharp
async Task<IEnumerable<Monkey>> GetMonkeysAsync()
{
    var url = "http://montemagno.com/monkeys.json";

    //Dev handle online/offline scenario
    if(!CrossConnectivity.Current.IsConnected)
    {
        return Barrel.Current.Get<IEnumerable<Monkey>>(key: url);
    }
    
    //Dev handles checking if cache is expired
    if(!Barrel.Current.IsExpired(key: url))
    {
        return Barrel.Current.Get<IEnumerable<Monkey>>(key: url);
    }


    var client = new HttpClient();
    var json = await client.GetStringAsync(url);
    var monkeys = JsonConvert.DeserializeObject<IEnumerable<Monkey>>(json);

    //Saves the cache and pass it a timespan for expiration
    Barrel.Current.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));

}
```

MonkeyCache will never delete data unless you want to, which is pretty nice incase you are offline for a long period of time. However, there are additional helpers to clean up data:

```csharp
    //removes all data
    Barrel.Current.EmptyAll();

    //param list of keys to flush
    Barrel.Current.Empty(key: url);
```

The above shows how you can integrate MonkeyCache into your existing source code without any modifications to your network code. However, MonkeyCache can help you there too! MonkeyCache also offers helpers when dealing with network calls via HttpCache.

HttpCache balances on top of the Barrel and offers helper methods to pass in a simple url that will handle adding and updating data into the Barrel.

```csharp
Task<IEnumerable<Monkey>> GetMonkeysAsync()
{
    var url = "http://montemagno.com/monkeys.json";

    //Dev handle online/offline scenario
    if(!CrossConnectivity.Current.IsConnected)
        return Barrel.Current.Get<IEnumerable<Monkey>>(key: url);

    return HttpCache.GetAsync<IEnumerable<Monkey>>(key: url, expiration: TimeSpan.FromDays(1), headers: headers);

}
```

Another goal of MonkeyCache is to offer a fast and native experience when storing and retrieving data from the Barrel. MonkeyCache uses a SQLite database to store all data across all platforms. This is super fast and is supported natively on each platform. In addition to the SQLite implementation, there is an implementation based on [LiteDB](http://www.litedb.org/) for data storage. Each have their own NuGet package, but have the same API, namespaces, and class names. This means that they cannot be installed at the same time, but one or the other. 

Regardless of implementation, Cache will always be stored in the default platform specific location:

|Platform|Location|
| ------------------- | :------------------: |
|Xamarin.iOS|NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];|
|Xamarin.Mac|NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];|
|Xamarin.Android|Application.Context.CacheDir.AbsolutePath|
|Windows 10 UWP|Windows.Storage.ApplicationData.Current.LocalFolder.Path|
|.NET Core|Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)|
|ASP.NET Core|Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)|
|.NET|Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)|


