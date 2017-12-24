using MonkeyCache.Realm;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyCache.Tests
{
	public partial class HttpCacheTests
	{
		void SetupBarrel()
		{
			Barrel.ApplicationId = "com.refractored.monkeylite";
			barrel = Barrel.Current;
		}
	}
}