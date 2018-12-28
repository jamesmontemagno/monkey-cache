using System;

namespace MonkeyCache
{
	[Flags]
	public enum CacheState
	{
		None = 0,
		Expired = 1,
		Active = 2
	}
}
