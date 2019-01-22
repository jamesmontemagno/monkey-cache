using MonkeyCache.SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyCache.Tests
{
    public partial class BarrelTests
    {
		public virtual void SetupBarrel()
		{
			Barrel.ApplicationId = "com.refractored.monkeysql";
			barrel = Barrel.Current;
		}
	}

	public partial class CustomDirBarrelTests
	{
		public override void SetupBarrel()
		{
			var dir = BarrelUtils.GetBasePath("com.refractored.monkeysql.customdir");
			this.barrel = Barrel.Create(dir);
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
