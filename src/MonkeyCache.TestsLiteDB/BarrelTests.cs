using MonkeyCache.LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyCache.Tests
{
    public partial class BarrelTests
    {
		void SetupBarrel()
		{
			Barrel.ApplicationId = "com.refractored.monkeylite";
			barrel = Barrel.Current;
		}
	}
	public partial class BarrelUtilTests
	{
		void SetupBarrel(ref IBarrel barrel)
		{
			Barrel.ApplicationId = "com.refractored.monkeyfile";
			barrel = Barrel.Current;
		}
	}
}
