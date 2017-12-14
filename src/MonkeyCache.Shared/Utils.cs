using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if __IOS__ || __MACOS__
using Foundation;
#elif __ANDROID__
using Android.App;
#endif


namespace MonkeyCache
{
    static class Utils
    {
        public static string GetBasePath(string uniqueId)
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
                throw new ArgumentException("You must set a UniqueId for MonkeyCache by using Barrel.UniqueId.");

            var path = string.Empty;
            ///Gets full path based on device type.
#if __IOS__ || __MACOS__
            path = NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];
#elif __ANDROID__
            path = Application.Context.CacheDir.AbsolutePath;
#elif __UWP__
            path = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#else
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#endif
            return Path.Combine(path, uniqueId);
        }
    }
}
