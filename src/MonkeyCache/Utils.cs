using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if __IOS__ || __MACOS__
using Foundation;
#elif __ANDROID__
using Android.App;
#endif

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MonkeyCache.SQLite")]
[assembly: InternalsVisibleTo("MonkeyCache.LiteDB")]
[assembly: InternalsVisibleTo("MonkeyCache.FileStore")]
namespace MonkeyCache
{
	internal static class Utils
	{
		public static string GetBasePath(string applicationId)
		{
			if (string.IsNullOrWhiteSpace(applicationId))
				throw new ArgumentException("You must set a ApplicationId for MonkeyCache by using Barrel.ApplicationId.");

			if (applicationId.IndexOfAny(Path.GetInvalidPathChars()) != -1)
				throw new ArgumentException("ApplicationId has invalid characters");

			var path = string.Empty;
			///Gets full path based on device type.
#if __IOS__ || __MACOS__
            path = NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];
#elif __ANDROID__
            path = Application.Context.CacheDir.AbsolutePath;
#elif __UWP__
            path = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
#else
			path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#endif
			return Path.Combine(path, applicationId);
		}

		public static DateTime GetExpiration(TimeSpan timeSpan)
		{
			try
			{
				return DateTime.UtcNow.Add(timeSpan);
			}
			catch
			{
				if (timeSpan.Milliseconds < 0)
					return DateTime.MinValue;

				return DateTime.MaxValue;
			}
		}
	}
}
