using System;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using Newtonsoft.Json;

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

        public T Get<T>(string key)
        {
            Banana ent;
            lock(dblock)
            {
                ent = db.Find<Banana>(key);
            }

            return JsonConvert.DeserializeObject<T>(ent.Contents, jsonSettings);
        }

        public void Add<T>(string key, T data, TimeSpan expireIn)
        {
            if (data == null)
                return;


            var ent = new Banana
            {
                Url = key,
                ExpirationDate = DateTime.UtcNow.Add(expireIn),
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

        public bool Empty()
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
                    db.Delete(key);
                db.Commit();
            }

            return true;
        }
    }
}
