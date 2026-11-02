using MonkeyCache;
using MonkeyCache.Samples;

if (args.Length != 1 || args[0] is not ("filestore" or "sqlite" or "litedb"))
{
    Console.Error.WriteLine("Usage: MonkeyCache.Console <filestore|sqlite|litedb>");
    return 2;
}

// Configure before the first provider Current access, including indirect DI/static access.
var path = Path.Combine(Path.GetTempPath(), "MonkeyCache.Examples", Guid.NewGuid().ToString("N"));
BarrelUtils.SetBaseCachePath(path);
const string applicationId = "portable-example";
IBarrel cache;
switch (args[0])
{
    case "filestore":
        MonkeyCache.FileStore.Barrel.ApplicationId = applicationId;
        cache = MonkeyCache.FileStore.Barrel.Current;
        break;
    case "sqlite":
        MonkeyCache.SQLite.Barrel.ApplicationId = applicationId;
        cache = MonkeyCache.SQLite.Barrel.Current;
        break;
    default:
        MonkeyCache.LiteDB.Barrel.ApplicationId = applicationId;
        cache = MonkeyCache.LiteDB.Barrel.Current;
        break;
}

Console.WriteLine($"Provider: {args[0]}; runtime: {Environment.Version}; example cache base: {path}");
foreach (var result in CacheWalkthrough.Run(cache))
    Console.WriteLine(result);
Console.WriteLine("Completed 8 cache checks. The isolated example directory is retained for inspection.");
return 0;
