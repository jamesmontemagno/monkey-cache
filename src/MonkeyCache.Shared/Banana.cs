
#if SQLITE
using SQLite;
#elif LITEDB
using LiteDB;
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
	{
		/// <summary>
		/// Unique Identifier
		/// </summary>
#if SQLITE
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

		/// <summary>
		/// Expiration data of the object, stored in UTC
		/// </summary>
		public DateTime ExpirationDate { get; set; }
	}
}
