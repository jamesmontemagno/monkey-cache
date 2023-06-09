using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SQLite;
using System.Collections.Generic;

namespace MonkeyCache.SQLite
{
	/// <summary>
	/// Persistent Key/Value data store for any data object.
	/// Allows for saving data along with expiration dates and ETags.
	/// </summary>
	public class Barrel : IBarrel
	{
		public static string ApplicationId { get; set; } = string.Empty;

		static readonly Lazy<string> baseCacheDir = new Lazy<string>(() =>
		{
			return Path.Combine(BarrelUtils.GetBasePath(ApplicationId), "MonkeyCache");
		});

		readonly SQLiteConnection db;
		readonly object dblock = new object();

		public bool AutoExpire { get; set; }

		static Barrel instance = null;

		/// <summary>
		/// Gets the instance of the Barrel
		/// </summary>
		public static IBarrel Current => (instance ?? (instance = new Barrel()));

		public static IBarrel Create(string cacheDirectory)
			=> new Barrel(cacheDirectory);

		Barrel(string cacheDirectory = null)
		{
			var directory = string.IsNullOrEmpty(cacheDirectory) ? baseCacheDir.Value : cacheDirectory;
			var path = Path.Combine(directory, "Barrel.db");
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			db = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
			db.CreateTable<Banana>();
		}

		#region Exist and Expiration Methods
		/// <summary>
		/// Checks to see if the key exists in the Barrel.
		/// </summary>
		/// <param name="key">Unique identifier for the entry to check</param>
		/// <returns>If the key exists</returns>
		public bool Exists(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			Banana ent;
			lock (dblock)
			{
				ent = db.Find<Banana>(key);
			}

			return ent != null;
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
			
			Banana ent;
			lock (dblock)
			{
				ent = db.Find<Banana>(key);
			}

			if (ent == null)
				return true;

			return DateTime.UtcNow > ent.ExpirationDate;
		}

		#endregion

		#region Get Methods
		/// <summary>
		/// Gets all the keys that are saved in the cache
		/// </summary>
		/// <returns>The IEnumerable of keys</returns>
		public IEnumerable<string> GetKeys(CacheState state = CacheState.Active)
		{
			IEnumerable<Banana> allBananas;
			lock (dblock)
			{
				allBananas = db.Query<Banana>($"SELECT Id FROM {nameof(Banana)}");
			}

			if (allBananas != null)
			{
				var bananas = new List<Banana>();

				if (state.HasFlag(CacheState.Active))
				{
					bananas = allBananas
						.Where(x => GetExpiration(x.Id) >= DateTime.UtcNow)
						.ToList();
				}

				if (state.HasFlag(CacheState.Expired))
				{
					bananas.AddRange(allBananas.Where(x => GetExpiration(x.Id) < DateTime.UtcNow));
				}

				return bananas.Select(x => x.Id);
			}

			return new string[0];
		}

		T Get<T>(string key, Func<string, T> deserialize)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			Banana ent;
			lock (dblock)
			{
				ent = db.Query<Banana>($"SELECT {nameof(ent.Contents)} FROM {nameof(Banana)} WHERE {nameof(ent.Id)} = ?", key).FirstOrDefault();
			}

			var result = default(T);

			if (ent == null || (AutoExpire && IsExpired(key)))
				return result;

			if (BarrelUtils.IsString(result))
			{
				object final = ent.Contents;
				return (T)final;
			}

			return deserialize(ent.Contents);
		}

		/// <inheritdoc/>
		[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo, or make sure all of the required types are preserved.")]
		public T Get<T>(string key, JsonSerializerOptions options = null) =>
			Get(key, contents => JsonDeserialize<T>(contents, options));

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Workaround https://github.com/dotnet/linker/issues/2001")]
		static T JsonDeserialize<T>(string contents, JsonSerializerOptions options) =>
			JsonSerializer.Deserialize<T>(contents, options);

		/// <inheritdoc/>
		public T Get<T>(string key, JsonTypeInfo<T> jsonTypeInfo) =>
			Get(key, contents => JsonSerializer.Deserialize(contents, jsonTypeInfo));

		/// <summary>
		/// Gets the ETag for the specified key.
		/// </summary>
		/// <param name="key">Unique identifier for entry to get</param>
		/// <returns>The ETag if the key is found, else null</returns>
		public string GetETag(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			Banana ent;
			lock (dblock)
			{
				ent = db.Query<Banana>($"SELECT {nameof(ent.ETag)} FROM {nameof(Banana)} WHERE {nameof(ent.Id)} = ?", key).FirstOrDefault();
			}

			if (ent == null)
				return null;

			return ent.ETag;
		}

		/// <summary>
		/// Gets the DateTime that the item will expire for the specified key.
		/// </summary>
		/// <param name="key">Unique identifier for entry to get</param>
		/// <returns>The expiration date if the key is found, else null</returns>
		public DateTime? GetExpiration(string key)
		{
			Banana ent;
			lock (dblock)
			{
				ent = db.Query<Banana>($"SELECT {nameof(ent.ExpirationDate)} FROM {nameof(Banana)} WHERE {nameof(ent.Id)} = ?", key).FirstOrDefault();
			}

			if (ent == null)
				return null;

			return ent.ExpirationDate;
		}

		#endregion

		#region Add Methods

		void Add(string key, string data, TimeSpan expireIn, string eTag)
		{
			var ent = new Banana
			{
				Id = key,
				ExpirationDate = BarrelUtils.GetExpiration(expireIn),
				ETag = eTag,
				Contents = data
			};

			lock (dblock)
			{
				db.InsertOrReplace(ent);
			}
		}

		void Add<T>(string key, T data, TimeSpan expireIn, string eTag, Func<T, string> serializer)
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
		static string JsonSerialize<T>(T data, JsonSerializerOptions options) => JsonSerializer.Serialize(data, options);

		/// <inheritdoc/>
		public void Add<T>(string key, T data, TimeSpan expireIn, JsonTypeInfo<T> jsonTypeInfo, string eTag = null) =>
			Add(key, data, expireIn, eTag, data => JsonSerializer.Serialize(data, jsonTypeInfo));


		#endregion

		#region Empty Methods
		/// <summary>
		/// Empties all expired entries that are in the Barrel.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		public void EmptyExpired()
		{
			lock (dblock)
			{
				var entries = db.Query<Banana>($"SELECT * FROM Banana WHERE ExpirationDate < ?", DateTime.UtcNow.Ticks);
				db.RunInTransaction(() =>
				{
					foreach (var k in entries)
						db.Delete<Banana>(k.Id);
				});
			}

		}

		/// <summary>
		/// Empties all expired entries that are in the Barrel.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		public void EmptyAll()
		{
			lock (dblock)
			{
				db.DeleteAll<Banana>();
			}

		}

		/// <summary>
		/// Empties all specified entries regardless if they are expired.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		/// <param name="key">keys to empty</param>
		public void Empty(params string[] key)
		{
			lock (dblock)
			{
				db.RunInTransaction(() =>
				{
					foreach (var k in key)
					{
						if (string.IsNullOrWhiteSpace(k))
							continue;

						db.Delete<Banana>(primaryKey: k);
					}
				});
			}
		}

		#endregion
	}
}