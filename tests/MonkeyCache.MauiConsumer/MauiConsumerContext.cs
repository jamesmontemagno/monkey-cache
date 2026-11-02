using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Maui.Storage;
using MonkeyCache;

internal static class MauiConsumerContext
{
    public static void VerifyAssets()
    {
        var core = typeof(BarrelUtils).Assembly;
        var essentials = typeof(FileSystem).Assembly;
        Require(core.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName == ".NETCoreApp,Version=v8.0",
            "Consumer must use the preserved net8 library contract.");
        Require(essentials.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName == ".NETCoreApp,Version=v10.0",
            "Consumer must use the net10 MAUI asset.");
#if WINDOWS
        Require(OperatingSystem.IsWindows(), "Windows consumer requires Windows for execution.");
        foreach (var assembly in new[] { core, essentials })
            Require(assembly.GetCustomAttribute<TargetPlatformAttribute>()?.PlatformName.StartsWith("Windows", StringComparison.OrdinalIgnoreCase) == true,
                $"{assembly.GetName().Name} must resolve its Windows asset.");
#else
        foreach (var assembly in new[] { core, essentials })
            Require(assembly.GetCustomAttribute<TargetPlatformAttribute>() == null,
                $"{assembly.GetName().Name} must resolve its portable asset.");
#endif
        foreach (var assembly in new[] { core, essentials })
            Console.WriteLine($"ASSET: {assembly.GetName().Name} {assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}; platform: {assembly.GetCustomAttribute<TargetPlatformAttribute>()?.PlatformName ?? "portable"}");

        if (UseNativePath())
        {
#if WINDOWS
            var expected = Environment.GetEnvironmentVariable("MONKEYCACHE_EXPECT_PACKAGE_IDENTITY");
            Require(expected is "present" or "absent", "Native mode requires explicit expected package identity: present or absent.");
            uint length = 0;
            var result = GetCurrentPackageFullName(ref length, null);
            Require(result is 0 or 122 or 15700, $"Package identity query failed: {result}.");
            var packaged = result != 15700;
            Require(packaged == (expected == "present"), "Actual package identity differs from the requested native row.");
            Console.WriteLine($"IDENTITY: {(packaged ? "present" : "absent")}");
#else
            throw new PlatformNotSupportedException("This native path harness currently requires the Windows-target build.");
#endif
        }
    }

    public static string GetRoot()
    {
        if (!UseNativePath())
            return Path.GetTempPath();
        var path = FileSystem.CacheDirectory;
        Require(Path.IsPathFullyQualified(path), "MAUI cache path must be absolute.");
        Console.WriteLine($"NATIVE MAUI CACHE: {path}");
        return path;
    }

    private static bool UseNativePath() => Environment.GetEnvironmentVariable("MONKEYCACHE_MAUI_NATIVE") switch
    {
        null or "0" => false,
        "1" => true,
        _ => throw new ArgumentException("MONKEYCACHE_MAUI_NATIVE must be unset, 0 or 1.")
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

#if WINDOWS
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, StringBuilder? packageFullName);
#endif
}
