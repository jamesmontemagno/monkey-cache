# 🐒Cache
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

## Platform Support

Monkey Cache is a .NET Standard 2.0 library, but has some platform specific tweaks for storing data in the correct Cache directory.

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

First, select an implementation of **Monkey Cache** that you would like (LiteDB, SQLite, or FileStore). Install the specific NuGet for that implementation, which will also install the base **MonkeyCache** library. Installing **MonkeyCache** without an implementation will only give you the high level interfaces. 

It is required that you set an ApplicationId for your application so a folder is created specifically for your app on disk. This can be done with a static string on Barrel before calling ANY method:

```csharp
Barrel.ApplicationId = "your_unique_name_here";
```

### LiteDB Encryption
LiteDB offers [built in encryption support](https://github.com/mbdavid/LiteDB/wiki/Connection-String), which can be enabled with a static string on Barrel before calling ANY method. You must choose this up front before saving any data.

```csharp
Barrel.EncryptionKey = "SomeKey";
```

### What is Monkey Cache?

The goal of Monkey Cache is to enable developers to easily cache any data for a limited amount of time. It is not Monkey Cache's mission to handle network requests to get or post data, only to cache data easily.

All data for Monkey Cache is stored and retrieved in a Barrel. 

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

Ideally, you can make these calls extremely generic and just pass in a string:

```csharp
public async Task<T> GetAsync<T>(string url, int days = 7, bool forceRefresh = false)
{
    var json = string.Empty;

    if (!CrossConnectivity.Current.IsConnected)
        json = Barrel.Current.Get<string>(url);

    if (!forceRefresh && !Barrel.Current.IsExpired(url))
        json = Barrel.Current.Get<string>(url);

    try
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            json = await client.GetStringAsync(url);
            Barrel.Current.Add(url, json, TimeSpan.FromDays(days));
        }
        return JsonConvert.DeserializeObject<T>(json);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unable to get information from server {ex}");
        //probably re-throw here :)
    }

    return default(T);
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

Cache will always be stored in the default platform specific location:

|Platform|Location|
| ------------------- | :------------------: |
|Xamarin.iOS|NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];|
|Xamarin.Mac|NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];|
|Xamarin.Android|Application.Context.CacheDir.AbsolutePath|
|Windows 10 UWP|Windows.Storage.ApplicationData.Current.LocalFolder.Path|
|.NET Core|Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)|
|ASP.NET Core|Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)|
|.NET|Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)|


#### Persisting Data Longer
Since the default is to use the Cache directories the platform can clean this up at any time.  If you want to change the base path of where the data is stored you can call the following static method:

```csharp
BarrelUtils.SetBaseCachePath("Path");
```

You MUST call this before initializing or accessing anything in the Barrel, and it can only ever be called once else it will throw an `InvalidOperationException`.


### FAQ

Have questions? Open up an issue. Here are a few:

#### How does Monkey Cache differ from the Settings Plugin?
Great question. I would also first say to read through this: https://github.com/jamesmontemagno/SettingsPlugin#settings-plugin-or-xamarinforms-appproperties as it will compare it to app.properties.

So with the Settings Plugin it is storing properties to the users local preferences/settings api of each platform. This is great for simple data (bool, int, and small strings). Each platform has different limitations when it comes to these different types of data and strings especially should never house a large amount of data. In fact if you try to store a super huge string on Android you could easily get an exception.

Monkey Cache enables you to easily store any type of data or just a simple string that you can easily serialize and deserialize back and forth. The key here is that you can set an expiration data associated with that data. So you can say this data should be used for the next few days. A key here is the ETag that is extra data that can be used to help with http caching and is used in the http caching library.


#### Isn't this just Akavache?
Akavache offers up a great and super fast asnchronous, pesistent key-value store that is based on SQLite and Reactive Extensions. I love me some Akavache and works great for applications, but wasn't exactly what I was looking for in a data caching library. Akavache offers up a lot of different features and really cool Reactive type of programming, but Monkey Cache focuses in on trying to create a drop dead simple API with a focus on data expiration. My goal was also to minimize dependencies on the NuGet package, which is why Monkey Cache offers a SQLite, LiteDB, or a simple FileStore implementation for use.

#### How about the link settings?

You may need to --linkskip=SQLite-net or other libraries.


#### Where Can I Learn More?
Listen to our podcast [Merge Conflict: Episode 76](http://www.mergeconflict.fm/76) for an overview of Monkey Cache and it's creation.

### License
Under MIT (see license file)

### Want To Support This Project?
All I have ever asked is to be active by submitting bugs, features, and sending those pull requests down! Want to go further? Make sure to subscribe to my weekly development podcast [Merge Conflict](http://mergeconflict.fm), where I talk all about awesome Xamarin goodies and you can optionally support the show by becoming a [supporter on Patreon](https://www.patreon.com/mergeconflictfm).


