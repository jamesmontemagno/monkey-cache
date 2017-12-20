using System;
using MonkeyCache.SQLite;

namespace MonkeyCache.Tests
{
	public partial class HttpCacheTests
	{
		void SetupBarrel()
		{
			Barrel.ApplicationId = "com.refractored.monkeyfile";
			barrel = Barrel.Current;
		}
	}
}
