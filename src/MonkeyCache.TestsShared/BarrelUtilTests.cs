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
				var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				BarrelUtils.SetBaseCachePath(folder);

				Assert.AreEqual(folder, BarrelUtils.basePath);
			}
			finally
			{
				BarrelUtils.basePath = path;
			}

		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void SetCacheDirectoryTwice()
		{			
			BarrelUtils.basePath = string.Empty;
			var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			BarrelUtils.SetBaseCachePath(folder);
			BarrelUtils.SetBaseCachePath(folder);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void SetCacheDirectoryAfterInitialize()
		{
			IBarrel barrel = null;
			var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			SetupBarrel(ref barrel);
			barrel.EmptyAll();

			BarrelUtils.SetBaseCachePath(folder);
		}
	}
}
