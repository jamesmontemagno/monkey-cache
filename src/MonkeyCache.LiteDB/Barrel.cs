using System;
using System.IO;
using System.Linq;
using LiteDB;
using Newtonsoft.Json;
using System.Reflection;
#if __IOS__ || __MACOS__
using Foundation;
#elif __ANDROID__
using Android.App;
#endif

namespace MonkeyCache
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

        JsonSerializerSettings jsonSettings;
        Barrel()
        {
            var directory = baseCacheDir.Value;
            string path = Path.Combine(directory, "Barrel.db");
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
            var ent = col.FindById(key);
          
            if (ent == null)
                return true;

            return DateTime.UtcNow > ent.ExpirationDate;
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
            var ent = col.FindById(key);

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
            var ent = col.FindById(key);

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
            var ent = col.FindById(key);

            if (ent == null)
                return null;

            return ent.ETag;
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
        public void Add(string key, string data, TimeSpan expireIn, string eTag = null)
        {
            if (data == null)
                return;

            var ent = new Banana
            {
                Id = key,
                ExpirationDate = DateTime.UtcNow.Add(expireIn),
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
        public void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null)
        {
            if (data == null)
                return;

            Add(key, JsonConvert.SerializeObject(data, jsonSettings), expireIn, eTag);
        }

        #endregion

        #region Empty Methods
        /// <summary>
        /// Empties all expired entries that are in the Barrel.
        /// Throws an exception if any deletions fail and rolls back changes.
        /// </summary>
        public void EmptyExpired()
        {
            col.Delete(b => b.ExpirationDate < DateTime.UtcNow);
        }

        /// <summary>
        /// Empties all expired entries that are in the Barrel.
        /// Throws an exception if any deletions fail and rolls back changes.
        /// </summary>
        public void EmptyAll()
        {
            col.Delete(Query.All());
            
        }

        /// <summary>
        /// Empties all specified entries regardless if they are expired.
        /// Throws an exception if any deletions fail and rolls back changes.
        /// </summary>
        /// <param name="key">keys to empty</param>
        public void Empty(params string[] key)
        {
            foreach (var k in key)
                col.Delete(k);
        }
        #endregion
    }
}