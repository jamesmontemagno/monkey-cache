using System;

namespace MonkeyCache
{
	/// <summary>
	/// Current state of the item in the cache.
	/// </summary>
	[Flags]
	public enum CacheState
	{
		/// <summary>
		/// An unknown state for the cache item
		/// </summary>
		None = 0,
		/// <summary>
		/// Expired cache item
		/// </summary>
		Expired = 1,
		/// <summary>
		/// Active non-expired cache item
		/// </summary>
		Active = 2
	}
}
