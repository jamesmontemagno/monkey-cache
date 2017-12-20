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

            jsonSettings = new JsonSerializerSettings {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            };

            index = new Dictionary<string, Tuple<string, DateTime>>();

            LoadIndex();
        }

        public static string ApplicationId { get; set; } = string.Empty;

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

            var hash = Hash(key);
            var path = Path.Combine(baseDirectory.Value, hash);

            File.WriteAllText(path, data);

            index[key] = new Tuple<string, DateTime>(eTag ?? string.Empty, DateTime.UtcNow.Add(expireIn));

            WriteIndex();

            indexLocker.ExitWriteLock();
        }

        public void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null)
        {
            var dataJson = JsonConvert.SerializeObject(data, jsonSettings);

            Add(key, dataJson, expireIn, eTag);
        }

        public void Empty(params string[] key)
        {
            indexLocker.EnterWriteLock();

            foreach (var k in key) {
                File.Delete(Path.Combine(baseDirectory.Value, Hash(k)));
                index.Remove(k);
            }

            WriteIndex();

            indexLocker.ExitWriteLock();
        }

        public void EmptyAll()
        {
            indexLocker.EnterWriteLock();

            foreach (var item in index) {
                var hash = Hash(item.Key);
                File.Delete(Path.Combine(baseDirectory.Value, hash));
            }

            index.Clear();

            WriteIndex();

            indexLocker.ExitWriteLock();
        }

        public void EmptyExpired()
        {
            indexLocker.EnterWriteLock();

            var expired = index.Where(k => k.Value.Item2 < DateTime.UtcNow);

            var toRem = new List<string>();

            foreach (var item in expired) {
                var hash = Hash(item.Key);
                File.Delete(Path.Combine(baseDirectory.Value, hash));
                toRem.Add(item.Key);
            }

            foreach (var key in toRem)
                index.Remove(key);

            WriteIndex();

            indexLocker.ExitWriteLock();
        }

        public bool Exists(string key)
        {
            var exists = false;

            indexLocker.EnterReadLock();

            exists = index.ContainsKey(key);

            indexLocker.ExitReadLock();

            return exists;
        }

        public string Get(string key)
        {
            string result = null;

            indexLocker.EnterReadLock();

            var hash = Hash(key);
            var path = Path.Combine(baseDirectory.Value, hash);

            if (index.ContainsKey(key) && File.Exists(path))
                result = File.ReadAllText(path);

            indexLocker.ExitReadLock();

            return result;
        }

        public T Get<T>(string key)
        {
            T result = default(T);

            indexLocker.EnterReadLock();

            var hash = Hash(key);
            var path = Path.Combine(baseDirectory.Value, hash);

            if (index.ContainsKey(key) && File.Exists(path)) {
                var contents = File.ReadAllText(path);
                result = JsonConvert.DeserializeObject<T>(contents, jsonSettings);
            }

            indexLocker.ExitReadLock();

            return result;
        }

        public string GetETag(string key)
        {
            if (key == null)
                return null;
            
            string etag = null;

            indexLocker.EnterReadLock();

            if (index.ContainsKey(key))
                etag = index[key]?.Item1;

            indexLocker.ExitReadLock();

            return etag;
        }

        public bool IsExpired(string key)
        {
            var expired = false;

            indexLocker.EnterReadLock();

            if (index.ContainsKey(key))
                expired = index[key].Item2 < DateTime.UtcNow;

            indexLocker.ExitReadLock();

            return expired;
        }

        Lazy<string> baseDirectory = new Lazy<string>(() => {
            return Path.Combine(Utils.GetBasePath(ApplicationId), "MonkeyCacheFS");
        });

        Dictionary<string, Tuple<string, DateTime>> index;

        const string INDEX_FILENAME = "index.dat";

        string indexFile;

        void WriteIndex()
        {
            if (string.IsNullOrEmpty(indexFile))
                indexFile = Path.Combine(baseDirectory.Value, INDEX_FILENAME);

            if (!Directory.Exists(baseDirectory.Value))
                Directory.CreateDirectory(baseDirectory.Value);
            
            using (var f = File.Open(indexFile, FileMode.Create))
            using (var sw = new StreamWriter(f)) {
                foreach (var kvp in index)
                    sw.WriteLine($"{kvp.Key}\t{kvp.Value.Item1}\t{kvp.Value.Item2.ToString("o")}");
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
            using (var sw = new StreamReader(f)) {
                string line = null;
                while ((line = sw.ReadLine()) != null) {
                    var parts = line.Split('\t');
                    if (parts.Length == 3) {
                        var key = parts[0];
                        var etag = parts[1];
                        var dt = parts[2];

                        DateTime date;
                        if (!string.IsNullOrEmpty(key) && DateTime.TryParse(dt, out date) && !index.ContainsKey(key))
                            index.Add(key, new Tuple<string, DateTime>(etag, date));
                    }
                }
            }
        }

        static string Hash(string input)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            return BitConverter.ToString(data);
        }
    }
}
