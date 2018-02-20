using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace MonkeyCache.FileStore
{
	public class Barrel : IBarrel
	{
		ReaderWriterLockSlim indexLocker;

		JsonSerializerSettings jsonSettings;

		Barrel()
		{
			indexLocker = new ReaderWriterLockSlim();

			jsonSettings = new JsonSerializerSettings
			{
				ObjectCreationHandling = ObjectCreationHandling.Replace,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				TypeNameHandling = TypeNameHandling.All,
			};

			index = new Dictionary<string, Tuple<string, DateTime>>();

			LoadIndex();
			WriteIndex();
		}

		public static string ApplicationId { get; set; } = string.Empty;

		public static ILid Lid { get; set; }

		static Barrel instance = null;

		/// <summary>
		/// Gets the instance of the Barrel
		/// </summary>
		public static IBarrel Current => (instance ?? (instance = new Barrel()));

		public void Add(string key, string data, TimeSpan expireIn, string eTag = null)
		{
			if (data == null)
				return;

			indexLocker.EnterWriteLock();

			try
			{
				var hash = Hash(key);
				var path = Path.Combine(baseDirectory.Value, hash);

				var contents = (Lid != null) ? Lid.AddingToBarrel(data) : data;

				if (contents == null)
					return;

				File.WriteAllText(path, contents);

				index[key] = new Tuple<string, DateTime>(eTag ?? string.Empty, DateTime.UtcNow.Add(expireIn));

				WriteIndex();
			}
			finally
			{
				indexLocker.ExitWriteLock();
			}
		}

		public void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null)
		{
			if (data == null)
				return;

			var dataJson = JsonConvert.SerializeObject(data, jsonSettings);

			Add(key, dataJson, expireIn, eTag);
		}

		public void Empty(params string[] key)
		{
			indexLocker.EnterWriteLock();

			try
			{
				foreach (var k in key)
				{
					File.Delete(Path.Combine(baseDirectory.Value, Hash(k)));
					index.Remove(k);
				}

				WriteIndex();
			}
			finally
			{
				indexLocker.ExitWriteLock();
			}
		}

		public void EmptyAll()
		{
			indexLocker.EnterWriteLock();

			try
			{
				foreach (var item in index)
				{
					var hash = Hash(item.Key);
					File.Delete(Path.Combine(baseDirectory.Value, hash));
				}

				index.Clear();

				WriteIndex();
			}
			finally
			{
				indexLocker.ExitWriteLock();
			}
		}

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
					File.Delete(Path.Combine(baseDirectory.Value, hash));
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

		public bool Exists(string key)
		{
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

		public string Get(string key)
		{
			string result = null;

			indexLocker.EnterReadLock();

			try
			{
				var hash = Hash(key);
				var path = Path.Combine(baseDirectory.Value, hash);

				if (index.ContainsKey(key) && File.Exists(path))
				{
					result = File.ReadAllText(path);
					if (Lid != null)
						result = Lid.GettingFromBarrel(result);
				}
			}
			finally
			{
				indexLocker.ExitReadLock();
			}

			return result;
		}

		public T Get<T>(string key)
		{
			var result = default(T);
			var resultJson = Get(key);

			if (resultJson != null)
			{
				result = JsonConvert.DeserializeObject<T>(resultJson, jsonSettings);
			}

			return result;
		}

		public string GetETag(string key)
		{
			if (key == null)
				return null;

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

		public bool IsExpired(string key)
		{
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

		Lazy<string> baseDirectory = new Lazy<string>(() =>
		{
			return Path.Combine(Utils.GetBasePath(ApplicationId), "MonkeyCacheFS");
		});

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
			using (var sw = new StreamWriter(f)) {
				foreach (var kvp in index) {
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

		static string Hash(string input)
		{
			var md5Hasher = MD5.Create();
			var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
			return BitConverter.ToString(data);
		}


		static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		static int DateTimeToEpochSeconds (DateTime date)
		{
			var diff = date - epoch;
			return (int)diff.TotalSeconds;
		}

		static DateTime EpochSecondsToDateTime (int seconds)
		{
			return epoch + TimeSpan.FromSeconds(seconds);
		}
	}
}
