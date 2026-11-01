using MonkeyCache;
using System.Text.Json.Serialization;

namespace MonkeyCache.PlatformBuild;

// Compilation checks native asset selection and all provider APIs; not device execution.
public static class Consumer
{
    public static void Cache(string directory)
    {
        IBarrel[] providers = [
            FileStore.Barrel.Create(Path.Combine(directory, "files")),
            SQLite.Barrel.Create(Path.Combine(directory, "sqlite")),
            LiteDB.Barrel.Create(Path.Combine(directory, "lite"))
        ];
        foreach (var provider in providers)
        {
            provider.Add("value", new Payload { Name = "sample" }, TimeSpan.FromMinutes(1));
            _ = provider.Get<Payload>("value");
            _ = provider.IsExpired("value");
            provider.EmptyExpired();
        }
    }
}

public sealed class Payload
{
    public string Name { get; set; } = "";
}

[JsonSerializable(typeof(Payload))]
internal partial class PayloadJsonContext : JsonSerializerContext;
