# monkey-cache
Just monkeying around with all that data to cache it!

The goal of MonkeyCache is to enable developers to easily cache any data for a limited amount of time. It is not MonkeyCache's mission to handle network requests to get or post data, only to cache data easily.

All data for MonkeyCache is stored and retrieved in a Barrel. 

For instance you are making a web request and you get some `json` back from the server. You would want the ability to cache this data incase you go offline, but also you need it to expire after 24 hours.

That may look something like this:

```csharp
async Task<Monkey> GetMonkeysAsync()
{
    var url = "http://montemagno.com/monkeys.json";

    //Dev handle online/offline scenario
    if(!CrossConnectivity.Current.IsConnected)
        return await Barrel.GetAsync<IEnumerable<Monkey>>(key: url);

    //Dev handles checking if cache is expired
    if(!Barrel.IsExpired(key: url))
    {
        return await Barrel.GetAsync<IEnumerable<Monkey>>(key: url);
    }


    var client = new HttpClient();
    var json = await client.GetStringAsync(url);
    var monkeys = JsonConvert.DeserializeObject<IEnumerable<Monkey>>(json);

    //Saves the cache and pass it a timespan for expiration
    await Barrel.AddAsync(key: url, data: monkeys, expiration: TimeSpan.FromDays(1);)

}
```

MonkeyCache will never delete data unless you want to, which is pretty nice incase you are offline for long period of time. However, there should be additional helpers to clean up data:

```csharp
    //removes all data
    await Barrel.EmptyExpiredAsync();

    //param list of keys to flush
    await Barrel.EmptyExpiredAsync(key: url);
```

Another goal of MonkeyCache is to offer a fast and native experience when storing and retrieving data from the Barrel.