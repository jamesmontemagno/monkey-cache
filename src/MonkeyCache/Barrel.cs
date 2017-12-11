using System;
using System.IO;
using SQLite;
using Newtonsoft.Json;

namespace MonkeyCache
{
    internal class Banana
    {
        [PrimaryKey]
        public string Url { get; set; }

        public string ETag { get; set; }
        public string Contents { get; set; }
        public DateTime ExpirationDate { get; set; }
    }

    /// <summary>
    /// A Barrel to throw all your bannanas in.
    /// </summary>
    public class Barrel
    {
        static readonly Lazy<string> baseCacheDir = new Lazy<string>(() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MonkeyCache"));

        readonly SQLiteConnection db;
        readonly object dblock = new object();


        static Barrel instance = null;

        /// <summary>
        /// Gets the instance of the Barrel
        /// </summary>
        public static Barrel Current => (instance ?? (instance = new Barrel()));

        JsonSerializerSettings jsonSettings;
        Barrel()
        {
            string path = Path.Combine(baseCacheDir.Value, "Barrel.sqlite");
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(baseCacheDir.Value);
            }

            db = new SQLiteConnection(path);
            db.CreateTable<Banana>();

            jsonSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            };
        }

        public bool Exists(string key)
        {
            Banana ent;
            lock (dblock)
            {
                ent = db.Find<Banana>(key);
            }

            return ent != null;
        }


        internal Banana GetBanana(string key)
        {
            Banana ent;
            lock (dblock)
            {
                ent = db.Find<Banana>(key);
            }

            return ent;
        }

        public T Get<T>(string key)
        {
            Banana ent;
            lock(dblock)
            {
                ent = db.Find<Banana>(key);
            }

            if (ent == null)
                return default(T);

            return JsonConvert.DeserializeObject<T>(ent.Contents, jsonSettings);
        }

        public string Get(string key)
        {
            Banana ent;
            lock (dblock)
            {
                ent = db.Find<Banana>(key);
            }

            if (ent == null)
                return null;

            return ent.Contents;
        }

        public void Add(string key, string data, TimeSpan expireIn, string etag = null)
        {
            if (data == null)
                return;


            var ent = new Banana
            {
                Url = key,
                ExpirationDate = DateTime.UtcNow.Add(expireIn),
                ETag = etag ?? string.Empty,
                Contents = data
            };
            lock (dblock)
            {
                db.InsertOrReplace(ent);
            }
        }

        public void Add<T>(string key, T data, TimeSpan expireIn, string etag = null)
        {
            if (data == null)
                return;


            var ent = new Banana
            {
                Url = key,
                ExpirationDate = DateTime.UtcNow.Add(expireIn),
                ETag = etag ?? string.Empty,
                Contents = JsonConvert.SerializeObject(data, jsonSettings)
            };
            lock (dblock)
            {
                db.InsertOrReplace(ent);
            }
        }

        public bool IsExpired(string key)
        {
            Banana ent;
            lock (dblock)
            {
                ent = db.Find<Banana>(key);
            }

            if (ent == null)
                return false;

            return DateTime.UtcNow > ent.ExpirationDate;
        }

        public bool EmptyAll()
        {
            lock(dblock)
            {
                db.DeleteAll<Banana>();
            }

            return true;
        }

        public bool Empty(params string[] key)
        {
            lock (dblock)
            {
                db.BeginTransaction();
                foreach (var k in key)
                    db.Delete<Banana>(primaryKey: k);
                db.Commit();
            }

            return true;
        }
    }
}
