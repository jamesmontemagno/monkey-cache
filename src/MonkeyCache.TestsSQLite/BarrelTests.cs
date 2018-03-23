using MonkeyCache.SQLite;
using MonkeyCache.TestsShared;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyCache.Tests
{
    public partial class BarrelTests
    {
		void SetupBarrel()
		{
			Barrel.ApplicationId = "com.refractored.monkeysql";
			barrel = Barrel.Current;
		}

		void SetupLid() => Barrel.Lid = new TestLid();

		bool LidWasUsed() => (Barrel.Lid as TestLid).WasUsed;
	}
}
