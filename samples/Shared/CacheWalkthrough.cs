using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MonkeyCache;

namespace MonkeyCache.Samples;

public static class CacheWalkthrough
{
    public static IReadOnlyList<string> Run(IBarrel cache)
    {
        var results = new List<string>();
        var key = $"example/{Guid.NewGuid():N}";
        var expiredKey = key + "/expired";
        var message = new CacheMessage("Hello from Monkey Cache");
        var metadata = ExampleJsonContext.Default.CacheMessage;

        cache.AutoExpire = true;
        cache.Add(key, message, TimeSpan.FromMinutes(5), metadata, eTag: "example-v1");
        Check(cache.Get(key, metadata) == message, "Read the cached message");
        Check(!cache.IsExpired(key), "Fresh entry is not expired");
        Check(cache.GetETag(key) == "example-v1", "Read its ETag");

        // A past expiration makes the example deterministic without a timer or sleep.
        cache.Add(expiredKey, message, TimeSpan.FromSeconds(-1), metadata);
        Check(cache.IsExpired(expiredKey), "Recognize an expired entry");
        Check(cache.Get(expiredKey, metadata) is null, "AutoExpire hides expired data");
        cache.EmptyExpired();
        Check(!cache.Exists(expiredKey), "Remove expired entries");
        Check(cache.Exists(key), "Keep the fresh entry during expiration cleanup");
        cache.Empty(key);
        Check(!cache.Exists(key), "Explicitly remove the example entry");
        return results;

        void Check(bool condition, string description)
        {
            if (!condition)
                throw new InvalidOperationException($"Cache example failed: {description}");
            results.Add($"PASS: {description}");
        }
    }
}

public sealed record CacheMessage(string Text);

[JsonSerializable(typeof(CacheMessage))]
internal partial class ExampleJsonContext : JsonSerializerContext
{
}
