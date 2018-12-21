using System;

namespace MonkeyCache
{
	[Flags]
	public enum CacheState
	{
		Expired = 1,
		Active = 2
	}
}
