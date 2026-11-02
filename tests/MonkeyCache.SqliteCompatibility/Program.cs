using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MonkeyCache;
using MonkeyCache.SQLite;
using SQLite;

if (args.Length != 2 || args[0] is not ("seed" or "read" or "update" or "updated" or "clear" or "empty"))
    throw new ArgumentException("Expected: seed|read|update|updated|clear|empty <isolated-directory>");

var mode = args[0];
var directory = Path.GetFullPath(args[1]);
var database = Path.Combine(directory, "Barrel.db");
if (mode == "seed" ? Directory.Exists(directory) : !File.Exists(database))
    throw new InvalidOperationException("Seed requires a new directory; other modes require an existing database.");

// Open through the provider first: the harness must not supply or mask its initializer.
IBarrel barrel = Barrel.Create(directory);
using var connection = new SQLiteConnection(database, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
Console.WriteLine($"ORM={typeof(SQLiteConnection).Assembly.FullName}");
Console.WriteLine($"Raw={typeof(SQLitePCL.raw).Assembly.FullName}");
Console.WriteLine($"Engine={connection.ExecuteScalar<string>("SELECT sqlite_version()")}");
Console.WriteLine($"SourceId={connection.ExecuteScalar<string>("SELECT sqlite_source_id()")}");
using var process = Process.GetCurrentProcess();
var modules = OperatingSystem.IsMacOS() ? MacImages.Paths() : process.Modules.Cast<ProcessModule>().Select(module => module.FileName);
var native = modules
    .Where(path => Path.GetFileName(path).Contains("e_sqlite3", StringComparison.OrdinalIgnoreCase))
    .Distinct().ToArray();
Check(native.Length == 1, "one e_sqlite3 native module loaded");
Console.WriteLine($"Native={native[0]}");
Check(connection.ExecuteScalar<string>("PRAGMA integrity_check") == "ok", "database integrity");
Check(connection.ExecuteScalar<int>("PRAGMA user_version") == 0, "unchanged user_version");
Console.WriteLine($"Schema={connection.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name='Banana'")}");
var schema = connection.Query<SchemaColumn>("PRAGMA table_info('Banana')");
Check(schema.Select(column => $"{column.Name}:{column.Type}:{column.PrimaryKey}").SequenceEqual(
    ["Id:varchar:1", "ETag:varchar:0", "Contents:varchar:0", "ExpirationDate:bigint:0"]), "unchanged Banana columns/types/key");
var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

switch (mode)
{
    case "seed":
        barrel.Add("string", "original", TimeSpan.FromDays(1), eTag: "\"original-etag\"");
        barrel.Add("options", new Payload("options", 9), TimeSpan.FromDays(1), options);
        barrel.Add("generated", new Payload("generated", 10), TimeSpan.FromDays(1), CompatibilityJson.Default.Payload);
        barrel.Add("expired", "expired-but-retained", TimeSpan.MinValue);
        barrel.Add("future", "post-2038", TimeSpan.MaxValue, eTag: "future-etag");
        barrel.Add("remove", "remove-me", TimeSpan.FromDays(1));
        Verify(false);
        break;
    case "read":
        Verify(false);
        break;
    case "update":
        Verify(false);
        barrel.Add("string", "updated", TimeSpan.FromDays(2), eTag: "\"updated-etag\"");
        barrel.Add("options", new Payload("updated-options", 19), TimeSpan.FromDays(1), options);
        barrel.Add("generated", new Payload("updated-generated", 20), TimeSpan.FromDays(1), CompatibilityJson.Default.Payload);
        barrel.EmptyExpired();
        barrel.Empty("remove");
        Verify(true);
        break;
    case "updated":
        Verify(true);
        break;
    case "clear":
        Verify(true);
        barrel.EmptyAll();
        Check(!barrel.GetKeys(CacheState.Active | CacheState.Expired).Any(), "EmptyAll removes every entry");
        break;
    case "empty":
        Check(!barrel.GetKeys(CacheState.Active | CacheState.Expired).Any(), "EmptyAll persists across graph/reopen");
        Check(!barrel.Exists("string") && barrel.GetETag("string") == null && barrel.GetExpiration("string") == null,
            "removed values and metadata absent");
        break;
}
Check(connection.ExecuteScalar<string>("PRAGMA integrity_check") == "ok", "final database integrity");
Console.WriteLine($"COMPLETE {mode}");

void Verify(bool updated)
{
    Check(barrel.Get<string>("string") == (updated ? "updated" : "original"), "string persistence/update");
    Check(barrel.GetETag("string") == (updated ? "\"updated-etag\"" : "\"original-etag\""), "ETag persistence/update");
    Check(!barrel.IsExpired("string"), "active expiration");
    Check(barrel.GetExpiration("string") > DateTime.UtcNow.AddHours(updated ? 47 : 23),
        "expiration duration persistence/update");
    Check(barrel.Get<Payload>("options", options) == (updated ? new Payload("updated-options", 19) : new Payload("options", 9)),
        "custom JSON options persistence/update");
    Check(barrel.Get("generated", CompatibilityJson.Default.Payload) == (updated ? new Payload("updated-generated", 20) : new Payload("generated", 10)),
        "generated JSON persistence/update");
    Check(barrel.Get<string>("options").Contains("\"name\"") && barrel.Get<string>("generated").Contains("\"name\""),
        "serialized naming policy unchanged");
    Check(barrel.Get<string>("future") == "post-2038" && barrel.GetExpiration("future")?.Ticks == DateTime.MaxValue.Ticks,
        "maximum expiration ticks preserved");
    Check(connection.ExecuteScalar<long>("SELECT ExpirationDate FROM Banana WHERE Id='future'") == DateTime.MaxValue.Ticks,
        "on-disk INTEGER DateTime ticks unchanged");
    Check(!barrel.IsExpired("future") && barrel.GetETag("future") == "future-etag", "future metadata retained");
    Check(barrel.Exists("remove") == !updated, "explicit removal state");
    Check(barrel.Exists("expired") == !updated, "expired cleanup state");
    if (!updated)
    {
        Check(barrel.IsExpired("expired") && barrel.GetExpiration("expired")?.Ticks == DateTime.MinValue.Ticks,
            "minimum expiration retained");
        Check(barrel.Get<string>("expired") == "expired-but-retained", "expired readable by default");
        barrel.AutoExpire = true;
        Check(barrel.Get<string>("expired") == null && barrel.Exists("expired"), "AutoExpire hides without deleting");
        barrel.AutoExpire = false;
    }
    Check(barrel.GetKeys(CacheState.Active | CacheState.Expired).Count() == (updated ? 4 : 6), "expected row count");
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException($"FAIL: {message}");
    Console.WriteLine($"PASS: {message}");
}

public sealed class SchemaColumn
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    [Column("pk")]
    public int PrimaryKey { get; set; }
}

public sealed record Payload(string Name, int Value);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Payload))]
internal partial class CompatibilityJson : JsonSerializerContext;

internal static class MacImages
{
    // Process.Modules on macOS reports only the executable, not its loaded libraries.
    internal static IEnumerable<string> Paths()
    {
        var count = ImageCount();
        for (uint index = 0; index < count; index++)
            yield return Marshal.PtrToStringUTF8(ImageName(index))
                ?? throw new InvalidOperationException("dyld returned a missing image name.");
    }

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "_dyld_image_count")]
    private static extern uint ImageCount();

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "_dyld_get_image_name")]
    private static extern IntPtr ImageName(uint index);
}
