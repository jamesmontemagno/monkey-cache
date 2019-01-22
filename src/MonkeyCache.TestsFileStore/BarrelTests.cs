using MonkeyCache.FileStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyCache.Tests
{
	public partial class BarrelTests
	{
		public virtual void SetupBarrel()
		{
			Barrel.ApplicationId = "com.refractored.monkeyfile";
			barrel = Barrel.Current;
		}
	}
	public partial class CustomDirBarrelTests
	{
		public override void SetupBarrel()
		{
			var dir = BarrelUtils.GetBasePath("com.refractored.monkeyfile.customdir");
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
