using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MonkeyCache;
using FS = MonkeyCache.FileStore.Barrel;
using SQL = MonkeyCache.SQLite.Barrel;
using LDB = MonkeyCache.LiteDB.Barrel;

#if MAUI_CONSUMER
MauiConsumerContext.VerifyAssets();
#endif

if (args.Length == 0)
{
#if MAUI_CONSUMER
    var basePath = MauiConsumerContext.GetRoot();
#else
    var basePath = Path.GetTempPath();
#endif
    var root = Path.Combine(basePath, "MonkeyCache.Regression", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    var failures = 0;
    var cases = 0;
    foreach (var provider in new[] { "FileStore", "SQLite", "LiteDB" })
    {
        foreach (var mode in new[] { "missing", "default", "explicit", "isolation", "write", "read" })
        {
            var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            start.ArgumentList.Add(provider);
            start.ArgumentList.Add(mode);
            start.ArgumentList.Add(Path.Combine(root, provider));
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start regression child.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }
            Console.Write(await output);
            Console.Error.Write(await error);
            cases++;
            if (process.ExitCode != 0) failures++;
        }
    }
    Console.WriteLine($"Processes: {cases}; passed: {cases - failures}; failed: {failures}; skipped: 0; data: {root}");
    return failures == 0 ? 0 : 1;
}

try
{
    if (args.Length != 3 || !Path.IsPathFullyQualified(args[2]))
        throw new ArgumentException("Expected provider, mode, absolute isolated root.");
    var (provider, mode, root) = (args[0], args[1], args[2]);
    Console.WriteLine($"{provider}/{mode}: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
    foreach (var assembly in new[] { typeof(BarrelUtils).Assembly, typeof(FS).Assembly, typeof(SQL).Assembly, typeof(LDB).Assembly })
        Console.WriteLine($"{assembly.GetName().Name} {assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
    if (mode == "missing")
    {
        Throws<ArgumentException>(() => _ = Current());
        return 0;
    }
    if (mode == "isolation")
    {
        var first = Create(Path.Combine(root, "first"));
        var second = Create(Path.Combine(root, "second"));
        first.Add("same", "first", TimeSpan.FromDays(1));
        second.Add("same", "second", TimeSpan.FromDays(1));
        Check(first.Get<string>("same") == "first" && second.Get<string>("same") == "second", "independent custom instances");
        return 0;
    }
    if (mode == "default")
    {
        BarrelUtils.SetBaseCachePath(Path.Combine(root, "base"));
        FS.ApplicationId = SQL.ApplicationId = LDB.ApplicationId = "configured";
        var current = Current();
        Check(ReferenceEquals(current, Current()), "Current singleton");
        Throws<InvalidOperationException>(() => BarrelUtils.SetBaseCachePath(root));
        var expected = Path.Combine(root, "base", "configured", provider == "FileStore" ? "MonkeyCacheFS/idx.dat" : "MonkeyCache/Barrel.db");
        Check(File.Exists(expected), "configured initialization path");
        Exercise(current);
        return 0;
    }
    var barrel = Create(Path.Combine(root, mode is "write" or "read" ? "persistent" : mode));
    if (mode == "read")
    {
        Verify(barrel);
        Check(barrel.Get<string>("future") == "future" && !barrel.IsExpired("future"), "post-2038 expiration survives restart");
        Check(barrel.Get<string>("minimum") == "minimum" && barrel.IsExpired("minimum"), "minimum timespan survives restart");
        Check(barrel.Get<string>("maximum") == "maximum" && !barrel.IsExpired("maximum"), "maximum timespan survives restart");
        Check(barrel.GetExpiration("future") > DateTime.UtcNow.AddYears(29), "post-2038 timestamp is not truncated");
        Check(barrel.GetExpiration("minimum") <= DateTime.MinValue.AddSeconds(1), "minimum timestamp is not truncated");
        Check(barrel.GetExpiration("maximum") >= DateTime.MaxValue.AddSeconds(-1), "maximum timestamp is not truncated");
        return 0;
    }
    Exercise(barrel);
    if (mode == "write")
    {
        barrel.Add("future", "future", TimeSpan.FromDays(365 * 30));
        barrel.Add("minimum", "minimum", TimeSpan.MinValue);
        barrel.Add("maximum", "maximum", TimeSpan.MaxValue);
    }
    return 0;

    IBarrel Current() => provider switch
    {
        "FileStore" => FS.Current,
        "SQLite" => SQL.Current,
        "LiteDB" => LDB.Current,
        _ => throw new ArgumentException("Unknown provider")
    };
    IBarrel Create(string path) => provider switch
    {
        "FileStore" => FS.Create(path),
        "SQLite" => SQL.Create(path),
        "LiteDB" => LDB.Create(path),
        _ => throw new ArgumentException("Unknown provider")
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

static void Exercise(IBarrel barrel)
{
    barrel.EmptyExpired();
    barrel.Add("UserAppTheme", "Failure Test", TimeSpan.FromDays(365), eTag: "etag");
    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    barrel.Add("options", new Payload("options", 9), TimeSpan.FromDays(1), options);
    barrel.Add("generated", new Payload("generated", 10), TimeSpan.FromDays(1), RegressionJson.Default.Payload);
    Check(barrel.Get<string>("options").Contains("\"name\""), "options used on write");
    Check(barrel.Get<string>("generated").Contains("\"name\""), "generated metadata used on write");
    barrel.Add("expired", "old", TimeSpan.FromDays(-1));
    Check(barrel.IsExpired("expired") && barrel.Get<string>("expired") == "old", "expired retained by default");
    barrel.AutoExpire = true;
    Check(barrel.Get<string>("expired") == null && barrel.Exists("expired"), "AutoExpire hides without deleting");
    barrel.AutoExpire = false;
    barrel.EmptyExpired();
    Check(!barrel.Exists("expired"), "cleanup removes expired");
    Verify(barrel);
}

static void Verify(IBarrel barrel)
{
    Check(barrel.Get<string>("UserAppTheme") == "Failure Test", "string roundtrip/persistence");
    Check(barrel.GetETag("UserAppTheme") == "etag", "ETag roundtrip/persistence");
    Check(!barrel.IsExpired("UserAppTheme"), "active expiration retained");
    Check(barrel.Get<Payload>("options", new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) == new Payload("options", 9), "options roundtrip/persistence");
    Check(barrel.Get("generated", RegressionJson.Default.Payload) == new Payload("generated", 10), "generated roundtrip/persistence");
    Check(!barrel.Exists("expired"), "expired entry stays removed");
}

static void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"FAIL: {name}");
    Console.WriteLine($"PASS: {name}");
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { Console.WriteLine($"PASS: expected {typeof(T).Name}"); return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}");
}

public sealed record Payload(string Name, int Value);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Payload))]
internal partial class RegressionJson : JsonSerializerContext;
