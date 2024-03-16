using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;

namespace MonkeyCache.FileStore
{
	public class Barrel : IBarrel
	{
		ReaderWriterLockSlim indexLocker;
		Lazy<string> baseDirectory;
		HashAlgorithm hashAlgorithm;
		object locker = new object();

		/// <summary>
		/// FileStore Barrel constructor
		/// </summary>
		/// <param name="cacheDirectory">Optionally specify directory where cache will live</param>
		/// <param name="hash">Optionally specify hash algorithm</param>
		Barrel(string cacheDirectory = null, HashAlgorithm hash = null)
		{
			baseDirectory = new Lazy<string>(() =>
			{
				return string.IsNullOrEmpty(cacheDirectory) ?
					Path.Combine(BarrelUtils.GetBasePath(ApplicationId), "MonkeyCacheFS")
					: cacheDirectory;
			});

			hashAlgorithm = hash;

			indexLocker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			index = new Dictionary<string, Tuple<string, DateTime>>();

			LoadIndex();
			WriteIndex();
		}

		public static string ApplicationId { get; set; } = string.Empty;

		public bool AutoExpire { get ; set; }

		static readonly Lazy<Barrel> instance = new Lazy<Barrel>(() => new Barrel());

		/// <summary>
		/// Gets the instance of the Barrel
		/// </summary>
		public static IBarrel Current => instance.Value;

		/// <summary>
		/// FileStore Barrel
		/// </summary>
		/// <param name="cacheDirectory">Optionally specify directory where cache will live</param>
		/// <param name="hash">Optionally specify hash algorithm</param>
		public static IBarrel Create(string cacheDirectory, HashAlgorithm hash = null) =>
			new Barrel(cacheDirectory, hash);

		void Add(string key, string data, TimeSpan expireIn, string eTag)
		{
			indexLocker.EnterWriteLock();

			try
			{
				var hash = Hash(key);
				var path = Path.Combine(baseDirectory.Value, hash);

				if (!Directory.Exists(baseDirectory.Value))
					Directory.CreateDirectory(baseDirectory.Value);

				File.WriteAllText(path, data);

				index[key] = new Tuple<string, DateTime>(eTag ?? string.Empty, BarrelUtils.GetExpiration(expireIn));

				WriteIndex();
			}
			finally
			{
				indexLocker.ExitWriteLock();
			}
		}

		void Add<T>(
			string key,
			T data,
			TimeSpan expireIn,
			string eTag,
			Func<T, string> serializer)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			if (data == null)
				throw new ArgumentNullException("Data can not be null.", nameof(data));

			string dataJson;
			if (BarrelUtils.IsString(data))
			{
				dataJson = data as string;
			}
			else
			{
				dataJson = serializer(data);
			}

			Add(key, dataJson, expireIn, eTag);
		}

		/// <inheritdoc/>
		[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo, or make sure all of the required types are preserved.")]
		public void Add<T>(string key, T data, TimeSpan expireIn, JsonSerializerOptions options = null, string eTag = null) =>
			Add(key, data, expireIn, eTag, data => JsonSerialize(data, options));

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Workaround https://github.com/dotnet/linker/issues/2001")]
		static string JsonSerialize<T>(T data, JsonSerializerOptions options) =>
			JsonSerializer.Serialize(data, options);

		/// <inheritdoc/>
		public void Add<T>(string key, T data, TimeSpan expireIn, JsonTypeInfo<T> jsonTypeInfo, string eTag = null) =>
			Add(key, data, expireIn, eTag, data => JsonSerializer.Serialize(data, jsonTypeInfo));

		/// <summary>
		/// Empties all specified entries regardless if they are expired.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		/// <param name="key">keys to empty</param>
		public void Empty(params string[] key)
		{
			indexLocker.EnterWriteLock();

			try
			{
				foreach (var k in key)
				{
					if (string.IsNullOrWhiteSpace(k))
						continue;

					var file = Path.Combine(baseDirectory.Value, Hash(k));
					if (File.Exists(file))
						File.Delete(file);

					index.Remove(k);
				}

				WriteIndex();
			}
			finally
			{
				indexLocker.ExitWriteLock();
			}
		}

		/// <summary>
		/// Empties all expired entries that are in the Barrel.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		public void EmptyAll()
		{
			indexLocker.EnterWriteLock();

			try
			{
				foreach (var item in index)
				{
					var hash = Hash(item.Key);
					var file = Path.Combine(baseDirectory.Value, hash);
					if (File.Exists(file))
						File.Delete(file);
				}

				index.Clear();

				WriteIndex();
			}
			finally
			{
				indexLocker.ExitWriteLock();
			}
		}

		/// <summary>
		/// Empties all expired entries that are in the Barrel.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		public void EmptyExpired()
		{
			indexLocker.EnterWriteLock();

			try
			{
				var expired = index.Where(k => k.Value.Item2 < DateTime.UtcNow);

				var toRem = new List<string>();

				foreach (var item in expired)
				{
					var hash = Hash(item.Key);
					var file = Path.Combine(baseDirectory.Value, hash);
					if (File.Exists(file))
						File.Delete(file);
					toRem.Add(item.Key);
				}

				foreach (var key in toRem)
					index.Remove(key);

				WriteIndex();
			}
			finally
			{
				indexLocker.ExitWriteLock();
			}
		}

		/// <summary>
		/// Checks to see if the key exists in the Barrel.
		/// </summary>
		/// <param name="key">Unique identifier for the entry to check</param>
		/// <returns>If the key exists</returns>
		public bool Exists(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			var exists = false;

			indexLocker.EnterReadLock();

			try
			{
				exists = index.ContainsKey(key);
			}
			finally
			{
				indexLocker.ExitReadLock();
			}

			return exists;
		}

		/// <summary>
		/// Gets all the keys that are saved in the cache
		/// </summary>
		/// <returns>The IEnumerable of keys</returns>
		public IEnumerable<string> GetKeys(CacheState state = CacheState.Active)
		{
			indexLocker.EnterReadLock();

			try
			{
				if (index != null)
				{
					var bananas = new List<KeyValuePair<string, Tuple<string, DateTime>>>();

					if (state.HasFlag(CacheState.Active))
					{
						bananas = index
							.Where(x => x.Value.Item2 >= DateTime.UtcNow)
							.ToList();
					}

					if (state.HasFlag(CacheState.Expired))
					{
						bananas.AddRange(index.Where(x => x.Value.Item2 < DateTime.UtcNow));
					}

					return bananas.Select(x => x.Key);
				}

				return new string[0];
			}
			catch (Exception)
			{
				return new string[0];
			}
			finally
			{
				indexLocker.ExitReadLock();
			}
		}

		T Get<T>(string key, Func<FileStream, T> deserialize)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			var result = default(T);

			indexLocker.EnterReadLock();

			try
			{
				var hash = Hash(key);
				var path = Path.Combine(baseDirectory.Value, hash);

				if (index.ContainsKey(key) && File.Exists(path) && (!AutoExpire || (AutoExpire && !IsExpired(key))))
				{
					if (BarrelUtils.IsString(result))
					{
						return (T)(object)File.ReadAllText(path);
					}

					using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
					result = deserialize(fileStream);
				}
			}
			finally
			{
				indexLocker.ExitReadLock();
			}

			return result;
		}

		/// <inheritdoc/>
		[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo, or make sure all of the required types are preserved.")]
		public T Get<T>(string key, JsonSerializerOptions options = null) =>
			Get(key, fileStream => JsonDeserialize<T>(fileStream, options));

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Workaround https://github.com/dotnet/linker/issues/2001")]
		static T JsonDeserialize<T>(FileStream fileStream, JsonSerializerOptions options) =>
			JsonSerializer.Deserialize<T>(fileStream, options);

		/// <inheritdoc/>
		public T Get<T>(string key, JsonTypeInfo<T> jsonTypeInfo) => Get(key, fileStream =>
			JsonSerializer.Deserialize(fileStream, jsonTypeInfo));

		/// <summary>
		/// Gets the DateTime that the item will expire for the specified key.
		/// </summary>
		/// <param name="key">Unique identifier for entry to get</param>
		/// <returns>The expiration date if the key is found, else null</returns>
		public DateTime? GetExpiration(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			DateTime? date = null;

			indexLocker.EnterReadLock();

			try
			{
				if (index.ContainsKey(key))
					date = index[key]?.Item2;
			}
			finally
			{
				indexLocker.ExitReadLock();
			}

			return date;
		}

		/// <summary>
		/// Gets the ETag for the specified key.
		/// </summary>
		/// <param name="key">Unique identifier for entry to get</param>
		/// <returns>The ETag if the key is found, else null</returns>
		public string GetETag(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			string etag = null;

			indexLocker.EnterReadLock();

			try
			{
				if (index.ContainsKey(key))
					etag = index[key]?.Item1;
			}
			finally
			{
				indexLocker.ExitReadLock();
			}

			return etag;
		}

		/// <summary>
		/// Checks to see if the entry for the key is expired.
		/// </summary>
		/// <param name="key">Key to check</param>
		/// <returns>If the expiration data has been met</returns>
		public bool IsExpired(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			var expired = true;

			indexLocker.EnterReadLock();

			try
			{
				if (index.ContainsKey(key))
					expired = index[key].Item2 < DateTime.UtcNow;
			}
			finally
			{
				indexLocker.ExitReadLock();
			}

			return expired;
		}

		Dictionary<string, Tuple<string, DateTime>> index;

		const string INDEX_FILENAME = "idx.dat";

		string indexFile;

		void WriteIndex()
		{
			if (string.IsNullOrEmpty(indexFile))
				indexFile = Path.Combine(baseDirectory.Value, INDEX_FILENAME);
			if (!Directory.Exists(baseDirectory.Value))
				Directory.CreateDirectory(baseDirectory.Value);

			using (var f = File.Open(indexFile, FileMode.Create))
			using (var sw = new StreamWriter(f))
			{
				foreach (var kvp in index)
				{
					var dtEpoch = DateTimeToEpochSeconds(kvp.Value.Item2);
					sw.WriteLine($"{kvp.Key}\t{kvp.Value.Item1}\t{dtEpoch.ToString()}");
				}
			}
		}

		void LoadIndex()
		{
			if (string.IsNullOrEmpty(indexFile))
				indexFile = Path.Combine(baseDirectory.Value, INDEX_FILENAME);

			if (!File.Exists(indexFile))
				return;

			index.Clear();

			using (var f = File.OpenRead(indexFile))
			using (var sw = new StreamReader(f))
			{
				string line = null;
				while ((line = sw.ReadLine()) != null)
				{
					var parts = line.Split('\t');
					if (parts.Length == 3)
					{
						var key = parts[0];
						var etag = parts[1];
						var dt = parts[2];

						int secondsSinceEpoch;
						if (!string.IsNullOrEmpty(key) && int.TryParse(dt, out secondsSinceEpoch) && !index.ContainsKey(key))
							index.Add(key, new Tuple<string, DateTime>(etag, EpochSecondsToDateTime(secondsSinceEpoch)));
					}
				}
			}
		}

		string Hash(string input)
		{
			byte[] data;
			if(hashAlgorithm is null)
			{
				data = MD5.HashData(Encoding.Default.GetBytes(input));
			}
			else
			{
				lock(locker)
				{
					data = hashAlgorithm.ComputeHash(Encoding.Default.GetBytes(input));
				}
			}
			return BitConverter.ToString(data);
		}

		static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		static int DateTimeToEpochSeconds(DateTime date)
		{
			var diff = date - epoch;
			return (int)diff.TotalSeconds;
		}

		static DateTime EpochSecondsToDateTime(int seconds) => epoch + TimeSpan.FromSeconds(seconds);
	}
}
