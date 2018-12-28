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
[assembly: InternalsVisibleTo("MonkeyCache.TestsFileStore")]
[assembly: InternalsVisibleTo("MonkeyCache.TestsSQLite")]
[assembly: InternalsVisibleTo("MonkeyCache.TestsLiteDB")]
namespace MonkeyCache
{
	/// <summary>
	/// Barrel Utils
	/// </summary>
	public static class BarrelUtils
	{
		internal static string basePath;

		/// <summary>
		/// Sets the base path to use. This can only be set once and before using the Barrel
		/// </summary>
		public static void SetBaseCachePath(string path)
		{
			if (!string.IsNullOrWhiteSpace(basePath))
				throw new InvalidOperationException("You can only set the base cache path once before using the Barrel.");

			basePath = path;
		}

		internal static bool IsString<T>(T item)
		{
			var typeOf = typeof(T);
			if (typeOf.IsGenericType && typeOf.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				typeOf = Nullable.GetUnderlyingType(typeOf);
			}
			var typeCode = Type.GetTypeCode(typeOf);
			return typeCode == TypeCode.String;
		}

		internal static string GetBasePath(string applicationId)
		{
			if (string.IsNullOrWhiteSpace(applicationId))
				throw new ArgumentException("You must set a ApplicationId for MonkeyCache by using Barrel.ApplicationId.");

			if (applicationId.IndexOfAny(Path.GetInvalidPathChars()) != -1)
				throw new ArgumentException("ApplicationId has invalid characters");
			
			if (string.IsNullOrWhiteSpace(basePath))
			{
				// Gets full path based on device type.
	#if __IOS__ || __MACOS__
				basePath = NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];
	#elif __ANDROID__
				basePath = Application.Context.CacheDir.AbsolutePath;
	#elif __UWP__
				basePath = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
	#else
				basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
	#endif
			}

			return Path.Combine(basePath, applicationId);
		}

		/// <summary>
		/// Gets the expiration from a timespan
		/// </summary>
		/// <param name="timeSpan"></param>
		/// <returns></returns>
		internal static DateTime GetExpiration(TimeSpan timeSpan)
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
