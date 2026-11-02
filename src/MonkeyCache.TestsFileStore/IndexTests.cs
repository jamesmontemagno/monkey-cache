using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyCache.FileStore;

namespace MonkeyCache.Tests
{
    [TestClass]
    public class IndexTests
    {
        [TestMethod]
        public void ReadsLegacy32BitEpochIndex()
        {
            var path = Path.Combine(TestEnvironment.Root, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "idx.dat"), "legacy\tetag\t2147483647\n");
            File.WriteAllText(Path.Combine(path, BitConverter.ToString(MD5.HashData(Encoding.Default.GetBytes("legacy")))), "value");
            var barrel = Barrel.Create(path);
            Assert.AreEqual("value", barrel.Get<string>("legacy"));
            Assert.AreEqual("etag", barrel.GetETag("legacy"));
            Assert.AreEqual(new DateTime(2038, 1, 19, 3, 14, 7, DateTimeKind.Utc), barrel.GetExpiration("legacy"));
        }
    }
}
