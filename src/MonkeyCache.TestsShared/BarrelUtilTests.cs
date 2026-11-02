using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace MonkeyCache.Tests
{
	[TestClass]
	[DoNotParallelize()]
	public partial class BarrelUtilTests
	{
		[TestMethod]
		public void SetBaseCacheDirectory()
		{
			var path = BarrelUtils.basePath;
			try
			{
				
				BarrelUtils.basePath = string.Empty;
				var folder = TestEnvironment.Root;
				BarrelUtils.SetBaseCachePath(folder);

				Assert.AreEqual(folder, BarrelUtils.basePath);
			}
			finally
			{
				BarrelUtils.basePath = path;
			}

		}

		[TestMethod]
		public void SetCacheDirectoryTwice()
		{			
			var path = BarrelUtils.basePath;
			try
			{
				BarrelUtils.basePath = string.Empty;
				BarrelUtils.SetBaseCachePath(TestEnvironment.Root);
				Assert.ThrowsExactly<InvalidOperationException>(() => BarrelUtils.SetBaseCachePath(TestEnvironment.Root));
			}
			finally
			{
				BarrelUtils.basePath = path;
			}
		}

		[TestMethod]
		public void SetCacheDirectoryAfterInitialize()
		{
			IBarrel barrel = null;
			var folder = TestEnvironment.Root;
			SetupBarrel(ref barrel);
			barrel.EmptyAll();

			Assert.ThrowsExactly<InvalidOperationException>(() => BarrelUtils.SetBaseCachePath(folder));
		}
	}
}
