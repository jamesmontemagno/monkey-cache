using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MonkeyCache.Tests
{
   
    [TestClass]
    public partial class HttpCacheTests
    {
        IBarrel barrel;
        string url;
        
        [TestInitialize]
        public void Setup()
        {
            SetupBarrel();
            url = "https://raw.githubusercontent.com/jamesmontemagno/app-monkeys/master/MonkeysApp/monkeydata.json";
        }

        [TestMethod]
        public async Task GetCachedTest()
        {
            var result = await HttpCache.Current.GetCachedAsync(barrel, url, TimeSpan.FromSeconds(60), TimeSpan.FromDays(1));

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetCachedForceTest()
        {
            var result = await HttpCache.Current.GetCachedAsync(barrel, url, TimeSpan.FromSeconds(60), TimeSpan.FromDays(1), true);

            Assert.IsNotNull(result);
        }
    }
}
