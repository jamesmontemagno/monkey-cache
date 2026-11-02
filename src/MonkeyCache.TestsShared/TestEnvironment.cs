using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace MonkeyCache.Tests
{
    [TestClass]
    public class TestEnvironment
    {
        internal static readonly string Root = Path.Combine(Path.GetTempPath(), "MonkeyCache.Tests", Guid.NewGuid().ToString("N"));

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            Directory.CreateDirectory(Root);
            BarrelUtils.SetBaseCachePath(Root);
        }
    }
}
