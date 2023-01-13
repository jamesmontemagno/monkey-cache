using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MonkeyCache.Tests
{
    [TestClass]
	[DoNotParallelize()]
	public partial class BarrelTests
    {
        IEnumerable<Monkey> monkeys;

        string url;
        string json;
        internal IBarrel barrel;

        [TestInitialize]
        public void Setup()
        {
            SetupBarrel();
            url = "http://montemagno.com/monkeys.json";
            json = @"[{""Name"":""Baboon"",""Location"":""Africa & Asia"",""Details"":""Baboons are African and Arabian Old World monkeys belonging to the genus Papio, part of the subfamily Cercopithecinae."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/9\/96\/Portrait_Of_A_Baboon.jpg\/314px-Portrait_Of_A_Baboon.jpg"",""Population"":10000},{""Name"":""Capuchin Monkey"",""Location"":""Central & South America"",""Details"":""The capuchin monkeys are New World monkeys of the subfamily Cebinae. Prior to 2011, the subfamily contained only a single genus, Cebus."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/4\/40\/Capuchin_Costa_Rica.jpg\/200px-Capuchin_Costa_Rica.jpg"",""Population"":23000},{""Name"":""Blue Monkey"",""Location"":""Central and East Africa"",""Details"":""The blue monkey or diademed monkey is a species of Old World monkey native to Central and East Africa, ranging from the upper Congo River basin east to the East African Rift and south to northern Angola and Zambia"",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/8\/83\/BlueMonkey.jpg\/220px-BlueMonkey.jpg"",""Population"":12000},{""Name"":""Squirrel Monkey"",""Location"":""Central & South America"",""Details"":""The squirrel monkeys are the New World monkeys of the genus Saimiri. They are the only genus in the subfamily Saimirinae. The name of the genus Saimiri is of Tupi origin, and was also used as an English name by early researchers."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/2\/20\/Saimiri_sciureus-1_Luc_Viatour.jpg\/220px-Saimiri_sciureus-1_Luc_Viatour.jpg"",""Population"":11000},{""Name"":""Golden Lion Tamarin"",""Location"":""Brazil"",""Details"":""The golden lion tamarin also known as the golden marmoset, is a small New World monkey of the family Callitrichidae."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/8\/87\/Golden_lion_tamarin_portrait3.jpg\/220px-Golden_lion_tamarin_portrait3.jpg"",""Population"":19000},{""Name"":""Howler Monkey"",""Location"":""South America"",""Details"":""Howler monkeys are among the largest of the New World monkeys. Fifteen species are currently recognised. Previously classified in the family Cebidae, they are now placed in the family Atelidae."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/0\/0d\/Alouatta_guariba.jpg\/200px-Alouatta_guariba.jpg"",""Population"":8000},{""Name"":""Japanese Macaque"",""Location"":""Japan"",""Details"":""The Japanese macaque, is a terrestrial Old World monkey species native to Japan. They are also sometimes known as the snow monkey because they live in areas where snow covers the ground for months each"",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/c\/c1\/Macaca_fuscata_fuscata1.jpg\/220px-Macaca_fuscata_fuscata1.jpg"",""Population"":1000},{""Name"":""Mandrill"",""Location"":""Southern Cameroon, Gabon, Equatorial Guinea, and Congo"",""Details"":""The mandrill is a primate of the Old World monkey family, closely related to the baboons and even more closely to the drill. It is found in southern Cameroon, Gabon, Equatorial Guinea, and Congo."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/7\/75\/Mandrill_at_san_francisco_zoo.jpg\/220px-Mandrill_at_san_francisco_zoo.jpg"",""Population"":17000},{""Name"":""Proboscis Monkey"",""Location"":""Borneo"",""Details"":""The proboscis monkey or long-nosed monkey, known as the bekantan in Malay, is a reddish-brown arboreal Old World monkey that is endemic to the south-east Asian island of Borneo."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/e\/e5\/Proboscis_Monkey_in_Borneo.jpg\/250px-Proboscis_Monkey_in_Borneo.jpg"",""Population"":15000},{""Name"":""Sebastian"",""Location"":""Seattle"",""Details"":""This little trouble maker lives in Seattle with James and loves traveling on adventures with James and tweeting @MotzMonkeys. He by far is an Android fanboy and is getting ready for the new Nexus 6P!"",""Image"":""http:\/\/www.refractored.com\/images\/sebastian.jpg"",""Population"":1},{""Name"":""Henry"",""Location"":""Phoenix"",""Details"":""An adorable Monkey who is traveling the world with Heather and live tweets his adventures @MotzMonkeys. His favorite platform is iOS by far and is excited for the new iPhone 6s!"",""Image"":""http:\/\/www.refractored.com\/images\/henry.jpg"",""Population"":1}]";
            monkeys = JsonSerializer.Deserialize<IEnumerable<Monkey>>(json);
        }


		#region Get Tests

		[TestMethod]
        public void GetStringTest()
        {
            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: json, expireIn: TimeSpan.FromDays(1));


            var cached = barrel.Get<string>(url);
            Assert.IsNotNull(cached);
			Assert.AreEqual(cached, json);

        }

        [TestMethod]
        public void GetTest()
        {


            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));


            var cached = barrel.Get<IEnumerable<Monkey>>(url);
            Assert.IsNotNull(cached);
			Assert.AreEqual(cached.Count(), monkeys.Count());
		}

        [TestMethod]
        public void GetTestJsonSerializerOptions()
        {
            JsonSerializerOptions options = new()
            {
                PropertyNamingPolicy =  JsonNamingPolicy.CamelCase
            };
            string serializedString = JsonSerializer.Serialize(monkeys, options);
            StringAssert.Contains(serializedString, "population");

            // Save the camel-case string into the cache
            barrel.Add(key: url, data: serializedString, expireIn: TimeSpan.FromDays(1));

            // Get the value back out of the cache using the same json options
            var cached = barrel.Get<IEnumerable<Monkey>>(url, options);
            Assert.IsNotNull(cached);
            Assert.AreEqual(cached.Count(), monkeys.Count());
        }

        [TestMethod]
        public void GetTestJsonTypeInfo()
        {
            var jsonTypeInfo = JsonContext.Default.IEnumerableMonkey;
            string serializedString = JsonSerializer.Serialize(monkeys, jsonTypeInfo);
            StringAssert.Contains(serializedString, "population");

            // Save the camel-case string into the cache
            barrel.Add(key: url, data: serializedString, expireIn: TimeSpan.FromDays(1));

            // Get the value back out of the cache using the same json type info
            var cached = barrel.Get<IEnumerable<Monkey>>(url, jsonTypeInfo);
            Assert.IsNotNull(cached);
            Assert.AreEqual(cached.Count(), monkeys.Count());
        }

        [TestMethod]
        public void GetETagTest()
        {

            var tag = "etag";
            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: json, expireIn: TimeSpan.FromDays(1), eTag: tag);


            var cached = barrel.GetETag(url);
            Assert.AreEqual(cached, tag);
        }

        [TestMethod]
        public void GetETagNullTest()
        {
            var cached = barrel.GetETag(url);
            Assert.IsNull(cached);
        }

		[TestMethod]
		public void GetKeysTest()
		{
			barrel.EmptyAll();

			var keysToStore = new[] { "One", "Two", "Three" }.OrderBy(x => x).ToArray();
			foreach (var item in keysToStore)
			{
				barrel.Add(key: item, data: item, expireIn: TimeSpan.FromDays(1));
			}

			var test1 = barrel.GetKeys(CacheState.Active | CacheState.Expired);

			var cachedKeys = barrel.GetKeys().OrderBy(x => x).ToArray();
			Assert.IsNotNull(cachedKeys);
			Assert.IsTrue(cachedKeys.Any());
			for (var i = 0; i < cachedKeys.Length; i++)
			{
				Assert.AreEqual(keysToStore[i], cachedKeys[i]);
			}
		}

		[TestMethod]
		public void GetAllActiveKeys_Implied_Test()
		{
			barrel.EmptyAll();

			barrel.Add("One", "one", TimeSpan.FromDays(-1)); // expired
			barrel.Add("Two", "two", TimeSpan.FromDays(1));
			barrel.Add("Three", "three", TimeSpan.FromDays(1));

			var cachedKeys = barrel.GetKeys();
			Assert.IsNotNull(cachedKeys);
			Assert.IsTrue(cachedKeys.Any());
			Assert.AreEqual(2, cachedKeys.Count());
			Assert.IsTrue(cachedKeys.Contains("Two"));
			Assert.IsTrue(cachedKeys.Contains("Three"));
		}

		[TestMethod]
		public void GetAllActiveKeys_CacheState_Active_Test()
		{
			barrel.EmptyAll();

			barrel.Add("One", "one", TimeSpan.FromDays(-1)); // expired
			barrel.Add("Two", "two", TimeSpan.FromDays(1));
			barrel.Add("Three", "three", TimeSpan.FromDays(1));

			var cachedKeys = barrel.GetKeys(CacheState.Active);
			Assert.IsNotNull(cachedKeys);
			Assert.IsTrue(cachedKeys.Any());
			Assert.AreEqual(2, cachedKeys.Count());
			Assert.IsTrue(cachedKeys.Contains("Two"));
			Assert.IsTrue(cachedKeys.Contains("Three"));
			Assert.IsFalse(cachedKeys.Contains("One"));
		}

		[TestMethod]
		public void GetAllActiveKeys_CacheState_Expired_Test()
		{
			barrel.EmptyAll();

			barrel.Add("One", "one", TimeSpan.FromDays(-1)); // expired
			barrel.Add("Two", "two", TimeSpan.FromDays(1));
			barrel.Add("Three", "three", TimeSpan.FromDays(1));

			var cachedKeys = barrel.GetKeys(CacheState.Expired);
			Assert.IsNotNull(cachedKeys);
			Assert.IsTrue(cachedKeys.Any());
			Assert.AreEqual(1, cachedKeys.Count());
			Assert.IsTrue(cachedKeys.Contains("One"));
			Assert.IsFalse(cachedKeys.Contains("Two"));
			Assert.IsFalse(cachedKeys.Contains("Three"));
		}

		[TestMethod]
		public void GetAllActiveKeys_CacheState_Active_And_Expired_Test()
		{
			barrel.EmptyAll();

			barrel.Add("One", "one", TimeSpan.FromDays(-1)); // expired
			barrel.Add("Two", "two", TimeSpan.FromDays(1));
			barrel.Add("Three", "three", TimeSpan.FromDays(1));

			var cachedKeys = barrel.GetKeys(CacheState.Active | CacheState.Expired);
			Assert.IsNotNull(cachedKeys);
			Assert.IsTrue(cachedKeys.Any());
			Assert.AreEqual(3, cachedKeys.Count());
			Assert.IsTrue(cachedKeys.Contains("One"));
			Assert.IsTrue(cachedKeys.Contains("Two"));
			Assert.IsTrue(cachedKeys.Contains("Three"));
		}


		[TestMethod]
		public void GetKeysEmptyTest()
		{
			barrel.EmptyAll();
			var cachedKeys = barrel.GetKeys();

			Assert.IsNotNull(cachedKeys);
			Assert.IsFalse(cachedKeys.Any());
		}


		#endregion

		#region Add Tests
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddStringNullTest() => barrel.Add<string>(key: url, data: null, expireIn: TimeSpan.FromDays(1));

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void AddNullKey() => barrel.Add<string>(key: null, data: json, expireIn: TimeSpan.FromDays(1));

		[TestMethod]
		public void AddMaxTime()
		{


			//Saves the cache and pass it a timespan for expiration
			barrel.Add(key: url, data: json, expireIn: TimeSpan.MaxValue);


			var cached = barrel.Get<string>(url);
			Assert.IsNotNull(cached);

		}

		[TestMethod]
		public void AddMinTime()
		{


			//Saves the cache and pass it a timespan for expiration
			barrel.Add(key: url, data: json, expireIn: TimeSpan.MinValue);


			var cached = barrel.Get<string>(url);
			Assert.IsNotNull(cached);

		}

		[TestMethod]
        public void AddStringTest()
        {


            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: json, expireIn: TimeSpan.FromDays(1));


            var cached = barrel.Get<string>(url);
            Assert.IsNotNull(cached);

        }

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddNullTest() => barrel.Add<Monkey>(key: url, data: null, expireIn: TimeSpan.FromDays(1));

		[TestMethod]
        public void AddTest()
        {


            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));


            var cached = barrel.Get<IEnumerable<Monkey>>(url);
            Assert.IsNotNull(cached);

        }

        [TestMethod]
        public void AddTestJsonSerializerOptions()
        {
            JsonSerializerOptions options = new()
            {
                PropertyNamingPolicy =  JsonNamingPolicy.CamelCase
            };

            // Save the value into the cache using the options
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1), options);

            // Get the string value out of the cache to verify it was serialized using the supplied options
            var serializedString = barrel.Get<string>(url);
            StringAssert.Contains(serializedString, "population");
        }

        [TestMethod]
        public void AddTestJsonTypeInfo()
        {
            var jsonTypeInfo = JsonContext.Default.IEnumerableMonkey;

            // Save the value into the cache using the JsonTypeInfo
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1), jsonTypeInfo);

            // Get the string value out of the cache to verify it was serialized using the supplied options
            var serializedString = barrel.Get<string>(url);
            StringAssert.Contains(serializedString, "population");
        }

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddTestNull() => barrel.Add<IEnumerable<Monkey>>(key: url, data: null, expireIn: TimeSpan.FromDays(1));

		#endregion

		#region Expiration Tests

		[TestMethod]
		public void IsExpiredNullTest() => Assert.IsTrue(barrel.IsExpired(url));


		[TestMethod]
        public void IsExpiredTest()
        {


            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(-1));


            var cached = barrel.Get<IEnumerable<Monkey>>(url);
            Assert.IsNotNull(cached);
            Assert.IsTrue(barrel.IsExpired(url));

        }

		[TestMethod]
		public void GetDate()
		{
			//Saves the cache and pass it a timespan for expiration
			barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));


			var cached = barrel.Get<IEnumerable<Monkey>>(url);
			
			var date = barrel.GetExpiration(url);
			Assert.IsNotNull(date);
			Assert.IsTrue(date <= DateTime.UtcNow.Add(TimeSpan.FromDays(1)));
			Assert.IsTrue(date >= DateTime.UtcNow);

		}

		[TestMethod]
        public void IsNotExpiredTest()
        {


            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));


            var cached = barrel.Get<IEnumerable<Monkey>>(url);
            Assert.IsNotNull(cached);
            Assert.IsFalse(barrel.IsExpired(url));

        }

        #endregion

        #region Empty Tests

        [TestMethod]
        public void EmptyTest()
        {


            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));


            var cached = barrel.Get<IEnumerable<Monkey>>(url);
            Assert.IsNotNull(cached);

            barrel.Empty(url);

            cached = barrel.Get<IEnumerable<Monkey>>(url);

            Assert.IsNull(cached);
        }

        [TestMethod]
        public void EmptyAllTest()
        {


            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));


            var cached = barrel.Get<IEnumerable<Monkey>>(url);
            Assert.IsNotNull(cached);

            barrel.EmptyAll();

            cached = barrel.Get<IEnumerable<Monkey>>(url);

            Assert.IsNull(cached);
        }

        [TestMethod]
        public void EmptyExpiredTest()
        {

            var url2 = "url2";

            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));
            barrel.Add(key: url2, data: monkeys, expireIn: TimeSpan.FromDays(-1));



            Assert.IsTrue(barrel.Exists(url));
            Assert.IsTrue(barrel.Exists(url2));

            barrel.EmptyExpired();

            Assert.IsTrue(barrel.Exists(url));
            Assert.IsFalse(barrel.Exists(url2));
        }

        #endregion


        #region Exists Tests
        [TestMethod]
        public void ExistsTest()
        {


            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));


            Assert.IsTrue(barrel.Exists(url));
        }

        [TestMethod]
        public void DoesNotExistsTest()
        {


            //Saves the cache and pass it a timespan for expiration
            barrel.Add(key: url, data: monkeys, expireIn: TimeSpan.FromDays(1));

            barrel.EmptyAll();

            Assert.IsFalse(barrel.Exists(url));
        }

		#endregion

		#region Performance Tests

#if DEBUG

		//[TestMethod]
		//public void PerformanceTests1() => PerformanceTestRunner(1, true, 1);

		//[TestMethod]
		//public void PerformanceTestsJson1() => PerformanceTestRunner(1, true, 1, true);

		//[TestMethod]
		//public void PerformanceTestsJson10() => PerformanceTestRunner(1, true, 10, true);

		//[TestMethod]
		//public void PerformanceTestsJson100() => PerformanceTestRunner(1, true, 100, true);

		//[TestMethod]
		//public void PerformanceTestsJson1000() => PerformanceTestRunner(1, true, 1000, true);

		//[TestMethod]
		//public void PerformanceTestsMultiThreadedJson() => PerformanceTestRunner(4, false, 1000, true);

		//[TestMethod]
		//public void PerformanceTestsMultiThreadedWithDuplicatesJson() => PerformanceTestRunner(4, true, 1000, true);


		//[TestMethod]
		//public void PerformanceTests() => PerformanceTestRunner(1, true, 1000);

		//[TestMethod]
		//public void PerformanceTestsMultiThreaded() => PerformanceTestRunner(4, false, 1000);

		//[TestMethod]
		//public void PerformanceTestsMultiThreadedWithDuplicates() => PerformanceTestRunner(4, true, 1000);


		void PerformanceTestRunner (int threads, bool allowDuplicateKeys, int keysPerThread, bool useJson = false)
        {
            var tasks = new List<Task>();

            var mainStopwatch = new Stopwatch();
            mainStopwatch.Start();

            for (var i = 0; i < threads; i++) {
                var i2 = i;

                var task = Task.Factory.StartNew(() => {
                    var tId = i2;

                    var keyModifier = allowDuplicateKeys ? string.Empty : tId.ToString();

                    var keys = Enumerable.Range(0, keysPerThread).Select(x => $"key-{keyModifier}-{x}").ToArray();

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    // Add a lot of items
                    foreach (var key in keys)
                    {
                        if(useJson)
                            barrel.Add(key: key, data: json, expireIn: TimeSpan.FromDays(1));
                        else
                            barrel.Add(key: key, data: monkeys, expireIn: TimeSpan.FromDays(1));
                    }
                    stopwatch.Stop();
                    Debug.WriteLine($"Add ({tId}) took {stopwatch.ElapsedMilliseconds} ms");
                    stopwatch.Restart();

                    foreach (var key in keys)
					{
						if (useJson)
							barrel.Get<string>(key);
						else
							barrel.Get<IEnumerable<Monkey>>(key);
                    }

                    stopwatch.Stop();
                    Debug.WriteLine($"Gets ({tId}) took {stopwatch.ElapsedMilliseconds} ms");
                    stopwatch.Restart();

                    foreach (var key in keys) {
                        var content = barrel.GetETag(key);
                    }

                    stopwatch.Stop();
                    Debug.WriteLine($"Get ({tId}) eTags took {stopwatch.ElapsedMilliseconds} ms");
                    stopwatch.Restart();

                    // Delete all
                    barrel.Empty(keys);

                    stopwatch.Stop();
                    Debug.WriteLine($"Empty ({tId}) took {stopwatch.ElapsedMilliseconds} ms");

                    Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 0);
                });

                task.ContinueWith(t => {
                    if (t.Exception?.InnerException != null)
                        Debug.WriteLine(t.Exception.InnerException);

                    Assert.IsNull(t.Exception);
                }, TaskContinuationOptions.OnlyOnFaulted);
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            mainStopwatch.Stop();
            Debug.WriteLine($"Entire Test took {mainStopwatch.ElapsedMilliseconds} ms");
        }
#endif
		#endregion

		[TestCleanup]
		public void Teardown() => barrel?.EmptyAll();
	}

	[TestClass]
	[DoNotParallelize()]
	public partial class CustomDirBarrelTests : BarrelTests
	{
	}

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(IEnumerable<Monkey>))]
    public partial class JsonContext : JsonSerializerContext
    {
    }
}
