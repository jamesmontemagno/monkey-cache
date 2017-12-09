# monkey-cache
Just monkeying around with all that data to cache it!

**Build Status**: ![](https://jamesmontemagno.visualstudio.com/_apis/public/build/definitions/00ee1525-d4f2-42b3-ab63-16f5d8b8aba0/4/badge)

**NuGet**: [![NuGet](https://img.shields.io/nuget/v/MonkeyCache.svg?label=NuGet)](https://www.nuget.org/packages/MonkeyCache/)

**Development NuGet source**: http://myget.org/F/monkey-cache

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
        return Barrel.Get<IEnumerable<Monkey>>(key: url);

    //Dev handles checking if cache is expired
    if(!Barrel.IsExpired(key: url))
    {
        return Barrel.Get<IEnumerable<Monkey>>(key: url);
    }


    var client = new HttpClient();
    var json = await client.GetStringAsync(url);
    var monkeys = JsonConvert.DeserializeObject<IEnumerable<Monkey>>(json);

    //Saves the cache and pass it a timespan for expiration
    Barrel.Add(key: url, data: monkeys, expiration: TimeSpan.FromDays(1));

}
```

MonkeyCache will never delete data unless you want to, which is pretty nice incase you are offline for long period of time. However, there should be additional helpers to clean up data:

```csharp
    //removes all data
    Barrel.Empty();

    //param list of keys to flush
    Barrel.Empty(key: url);
```

The above shows how you can integrate MonkeyCache into your existing source code without any modifications to your network code. However, MonkeyCache can help you there too! MonkeyCache also offers helpers when dealing with network calls via HttpCache.

HttpCache balances on top of the Barrel and offers helper methods to pass in a simple url that will handle adding and updating data into the Barrel.

```csharp
Task<IEnumerable<Monkey>> GetMonkeysAsync()
{
    var url = "http://montemagno.com/monkeys.json";

    //Dev handle online/offline scenario
    if(!CrossConnectivity.Current.IsConnected)
        return Barrel.Get<IEnumerable<Monkey>>(key: url);

    return HttpCache.GetAsync<IEnumerable<Monkey>>(key: url, expiration: TimeSpan.FromDays(1), headers: headers);

}
```


Another goal of MonkeyCache is to offer a fast and native experience when storing and retrieving data from the Barrel.
