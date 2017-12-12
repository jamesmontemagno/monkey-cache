using System;
using System.IO;
using System.Linq;
using SQLite;
using Newtonsoft.Json;
using System.Reflection;

namespace MonkeyCache
{
    class Banana
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
        static readonly Lazy<string> baseCacheDir = new Lazy<string>(() =>
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (AppDomain.CurrentDomain.GetAssemblies().Any(x => x.GetName().Name == "Xamarin.iOS"))
                path = Path.GetFullPath(Path.Combine(path, "..", "Library", "Caches"));

            return Path.Combine(path, "MonkeyCache");
        });

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
        
        public T Get<T>(string key)
        {
            Banana ent;
            lock(dblock)
            {
                ent = db.Query<Banana>($"SELECT Contents FROM Banana WHERE Url = ?", key).FirstOrDefault();
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
                ent = db.Query<Banana>($"SELECT Contents FROM Banana WHERE Url = ?", key).FirstOrDefault();
            }

            if (ent == null)
                return null;

            return ent.Contents;
        }

        public string GetETag(string key)
        {
            Banana ent;
            lock (dblock)
            {
                ent = db.Query<Banana>($"SELECT ETag FROM Banana WHERE Url = ?", key).FirstOrDefault();
            }

            if (ent == null)
                return null;

            return ent.ETag;
        }

        public void Add(string key, string data, TimeSpan expireIn, string eTag = null)
        {
            if (data == null)
                return;


            var ent = new Banana
            {
                Url = key,
                ExpirationDate = DateTime.UtcNow.Add(expireIn),
                ETag = eTag,
                Contents = data
            };
            lock (dblock)
            {
                db.InsertOrReplace(ent);
            }
        }

        public void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null)
        {
            if (data == null)
                return;
            
            var ent = new Banana
            {
                Url = key,
                ExpirationDate = DateTime.UtcNow.Add(expireIn),
                ETag = eTag,
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

        public bool EmptyExpired()
        {
            lock (dblock)
            {
                var entries = db.Query<Banana>($"SELECT * FROM Banana WHERE ExpirationDate < ?", DateTime.UtcNow.Ticks);
                db.RunInTransaction(() =>
                {
                    foreach (var k in entries)
                        db.Delete<Banana>(k.Url);
                });
            }

            return true;
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
                db.RunInTransaction(() =>
                {
                    foreach (var k in key)
                        db.Delete<Banana>(primaryKey: k);
                });
            }

            return true;
        }
    }
}
