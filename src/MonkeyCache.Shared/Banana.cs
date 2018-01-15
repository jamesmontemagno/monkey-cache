
#if SQLITE
using SQLite;
#elif LITEDB
using LiteDB;
#elif REALM
using Realms;
#endif
using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyCache
{
    /// <summary>
    /// Data object for Barrel
    /// </summary>
    class Banana
#if REALM
        : RealmObject
#endif
    {
        /// <summary>
        /// Unique Identifier
        /// </summary>
#if SQLITE || REALM
        [PrimaryKey]
#elif LITEDB
        [BsonId]
#endif
        public string Id { get; set; }


        /// <summary>
        /// Additional ETag to set for Http Caching
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Main Contents.
        /// </summary>
        public string Contents { get; set; }

#if REALM
        /// <summary>
        /// Expiration data of the object
        /// </summary>
        public DateTimeOffset ExpirationDate { get; set; }
#else
        /// <summary>
        /// Expiration data of the object, stored in UTC
        /// </summary>
        public DateTime ExpirationDate { get; set; }
#endif
    }
}
