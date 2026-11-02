using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using MonkeyCache.FileStore;

namespace MonkeyCache.Samples.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Run once, before constructing services/pages that might access Barrel.Current.
        BarrelUtils.SetBaseCachePath(FileSystem.Current.CacheDirectory);
        Barrel.ApplicationId = "MonkeyCache.Maui.Example";

        return MauiApp.CreateBuilder().UseMauiApp<CacheApp>().Build();
    }
}
