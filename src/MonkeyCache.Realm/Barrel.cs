﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Realms;

namespace MonkeyCache.Realm
{
	public class Barrel : IBarrel
	{
		public static string ApplicationId { get; set; } = string.Empty;
		public static string EncryptionKey { get; set; } = string.Empty;

		/// <summary>
		/// Gets the instance of the Barrel
		/// </summary>
		public static IBarrel Current => (instance ?? (instance = new Barrel()));

		static readonly Lazy<string> baseCacheDir = new Lazy<string>(() =>
		{
			return Path.Combine(Utils.GetBasePath(ApplicationId), "MonkeyCache");
		});

		readonly RealmConfiguration configuration;

		Realms.Realm realm
		{
			get => Realms.Realm.GetInstance(configuration);
		}

		JsonSerializerSettings jsonSettings;

		static Barrel instance = null;

		public Barrel()
		{
			var directory = baseCacheDir.Value;
			var path = Path.Combine(directory, "Barrel.realm");
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			configuration = new RealmConfiguration(path);

			jsonSettings = new JsonSerializerSettings
			{
				ObjectCreationHandling = ObjectCreationHandling.Replace,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				TypeNameHandling = TypeNameHandling.All,
			};
		}

		#region Add Methods

		/// <summary>
		/// Adds a string netry to the barrel
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key">Unique identifier for the entry</param>
		/// <param name="data">Data string to store</param>
		/// <param name="expireIn">Time from UtcNow to expire entry in</param>
		/// <param name="eTag">Optional eTag information</param>
		public void Add(string key, string data, TimeSpan expireIn, string eTag = null)
		{
			if (data == null)
				return;

			realm.Write(() =>
			{
				var found = realm.Find<Banana>(key);

				if(found != null)
				{
					found.Id = key;
					found.ExpirationDate = DateTimeOffset.Now.Add(expireIn);
					found.ETag = eTag;
					found.Contents = data;
				}
				else
				{
					var ent = new Banana
					{
						Id = key,
						ExpirationDate = DateTimeOffset.Now.Add(expireIn),
						ETag = eTag,
						Contents = data
					};

					realm.Add(ent);
				}
			});
		}

		/// <summary>
		/// Adds an entry to the barrel
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key">Unique identifier for the entry</param>
		/// <param name="data">Data object to store</param>
		/// <param name="expireIn">Time from UtcNow to expire entry in</param>
		/// <param name="eTag">Optional eTag information</param>
		public void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null)
		{
			if (data == null)
				return;

			Add(key, JsonConvert.SerializeObject(data, jsonSettings), expireIn, eTag);
		}

		#endregion

		#region Exist and Expiration Methods
		/// <summary>
		/// Checks to see if the key exists in the Barrel.
		/// </summary>
		/// <param name="key">Unique identifier for the entry to check</param>
		/// <returns>If the key exists</returns>
		public bool Exists(string key)
		{
			var ent = realm.Find<Banana>(key);

			return ent != null;
		}

		/// <summary>
		/// Checks to see if the entry for the key is expired.
		/// </summary>
		/// <param name="key">Key to check</param>
		/// <returns>If the expiration data has been met</returns>
		public bool IsExpired(string key)
		{
			var ent = realm.Find<Banana>(key);

			if (ent == null)
				return true;

			return DateTimeOffset.Now > ent.ExpirationDate;
		}

		#endregion

		#region Get Methods

		/// <summary>
		/// Gets the data entry for the specified key.
		/// </summary>
		/// <param name="key">Unique identifier for the entry to get</param>
		/// <returns>The data object that was stored if found, else default(T)</returns>
		public T Get<T>(string key)
		{
			var ent = realm.Find<Banana>(key);

			if (ent == null)
				return default(T);

			return JsonConvert.DeserializeObject<T>(ent.Contents, jsonSettings);
		}

		/// <summary>
		/// Gets the string entry for the specified key.
		/// </summary>
		/// <param name="key">Unique identifier for the entry to get</param>
		/// <returns>The string that was stored if found, else null</returns>
		public string Get(string key)
		{
			var ent = realm.Find<Banana>(key);

			if (ent == null)
				return null;

			return ent.Contents;
		}

		/// <summary>
		/// Gets the ETag for the specified key.
		/// </summary>
		/// <param name="key">Unique identifier for entry to get</param>
		/// <returns>The ETag if the key is found, else null</returns>
		public string GetETag(string key)
		{
			var ent = realm.Find<Banana>(key);

			if (ent == null)
				return null;

			return ent.ETag;
		}

		#endregion

		#region Empty Methods
		/// <summary>
		/// Empties all expired entries that are in the Barrel.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		public void EmptyExpired()
		{
			var now = DateTimeOffset.Now;
			realm.Write(() => {
				realm.RemoveRange(realm.All<Banana>().Where(b => b.ExpirationDate < now));
			});
		}

		/// <summary>
		/// Empties all expired entries that are in the Barrel.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		public void EmptyAll()
		{
			realm.Write(() => realm.RemoveAll<Banana>());
		}

		/// <summary>
		/// Empties all specified entries regardless if they are expired.
		/// Throws an exception if any deletions fail and rolls back changes.
		/// </summary>
		/// <param name="key">keys to empty</param>
		public void Empty(params string[] key)
		{
			realm.Write(() => {
				foreach (var item in key)
				{
					var foundItem = realm.Find<Banana>(item);
					if (foundItem != null)
						realm.Remove(foundItem);
				}
			});
		}
		#endregion
	}
}
