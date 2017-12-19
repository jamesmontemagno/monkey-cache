using MonkeyCache.SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyCache.Tests
{
    public partial class BarrelTests
    {
		void SetupBarrel()
		{
			Barrel.ApplicationId = "com.refractored.monkeycacheldb";
			barrel = Barrel.Current;
		}
	}
}
