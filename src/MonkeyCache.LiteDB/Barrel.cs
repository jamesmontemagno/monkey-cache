using System;
using System.IO;
using LiteDB;
using Newtonsoft.Json;

namespace MonkeyCache.LiteDB
{
	/// <summary>
	/// Persistant Key/Value data store for any data object.
	/// Allows for saving data along with expiration dates and ETags.
	/// </summary>
	public class Barrel : IBarrel
	{
		public static string ApplicationId { get; set; } = string.Empty;
		public static string EncryptionKey { get; set; } = string.Empty;

		static readonly Lazy<string> baseCacheDir = new Lazy<string>(() =>
		{
			return Path.Combine(Utils.GetBasePath(ApplicationId), "MonkeyCache");
		});

		readonly LiteDatabase db;


		static Barrel instance = null;
		static LiteCollection<Banana> col;

		/// <summary>
		/// Gets the instance of the Barrel
		/// </summary>
		public static IBarrel Current => (instance ?? (instance = new Barrel()));

		readonly JsonSerializerSettings jsonSettings;
		Barrel()
		{
			var directory = baseCacheDir.Value;
			var path = Path.Combine(directory, "Barrel.db");
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			if (!string.IsNullOrWhiteSpace(EncryptionKey))
				path = $"Filename={path}; Password={EncryptionKey}";

			db = new LiteDatabase(path);
			col = db.GetCollection<Banana>();

			jsonSettings = new JsonSerializerSettings
			{
				ObjectCreationHandling = ObjectCreationHandling.Replace,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				TypeNameHandling = TypeNameHandling.All,
			};


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

			var ent = col.FindById(key);

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

			var ent = col.FindById(key);

			if (ent == null)
				return true;

			return DateTime.UtcNow > ent.ExpirationDate.ToUniversalTime();
		}

		#endregion

		#region Get Methods

		/// <summary>
		/// Gets the data entry for the specified key.
		/// </summary>
		/// <param name="key">Unique identifier for the entry to get</param>
		/// <param name="jsonSerializationSettings">Custom json serialization settings to use</param>
		/// <returns>The data object that was stored if found, else default(T)</returns>
		public T Get<T>(string key, JsonSerializerSettings jsonSerializationSettings = null)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			var result = default(T);

			var ent = col.FindById(key);

			if (ent == null)
				return result;

			if (Utils.IsString(result))
			{
				object final = ent.Contents;
				return (T)final;
			}

			return JsonConvert.DeserializeObject<T>(ent.Contents, jsonSerializationSettings ?? jsonSettings);
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

			var ent = col.FindById(key);

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
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			var ent = col.FindById(key);

			if (ent == null)
				return null;

			return ent.ExpirationDate;
		}

		#endregion

		#region Add Methods

		/// <summary>
		/// Adds a string netry to the barrel
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key">Unique identifier for the entry</param>
		/// <param name="data">Data string to store</param>
		/// <param name="expireIn">Time from UtcNow to expire entry in</param>
		/// <param name="eTag">Optional eTag information</param>
		void Add(string key, string data, TimeSpan expireIn, string eTag = null)
		{
			if (data == null)
				return;

			var ent = new Banana
			{
				Id = key,
				ExpirationDate = Utils.GetExpiration(expireIn),
				ETag = eTag,
				Contents = data
			};

			col.Upsert(ent);
		}

		/// <summary>
		/// Adds an entry to the barrel
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key">Unique identifier for the entry</param>
		/// <param name="data">Data object to store</param>
		/// <param name="expireIn">Time from UtcNow to expire entry in</param>
		/// <param name="eTag">Optional eTag information</param>
		/// <param name="jsonSerializationSettings">Custom json serialization settings to use</param>
		public void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null, JsonSerializerSettings jsonSerializationSettings = null)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key can not be null or empty.", nameof(key));

			if (data == null)
				throw new ArgumentNullException("Data can not be null.", nameof(data));

			var dataJson = string.Empty;

			if (Utils.IsString(data))
			{
				dataJson = data as string;
			}
			else
			{
				dataJson = JsonConvert.SerializeObject(data, jsonSerializationSettings ?? jsonSettings);
			}

			Add(key, dataJson, expireIn, eTag);
		}

		#endregion

		#region Empty Methods
		/// <summary>
		/// Empties all expired entries that are in the Barrel.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		public void EmptyExpired() => col.Delete(b => b.ExpirationDate.ToUniversalTime() < DateTime.UtcNow);

		/// <summary>
		/// Empties all expired entries that are in the Barrel.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		public void EmptyAll() => col.Delete(Query.All());

		/// <summary>
		/// Empties all specified entries regardless if they are expired.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		/// <param name="key">keys to empty</param>
		public void Empty(params string[] key)
		{
			foreach (var k in key)
			{
				if (string.IsNullOrWhiteSpace(k))
					continue;

				col.Delete(k);
			}
		}
		#endregion
	}
}