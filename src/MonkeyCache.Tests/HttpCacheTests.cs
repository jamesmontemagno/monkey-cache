using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace MonkeyCache.Tests
{
   
    [TestClass]
    public class HttpCacheTests
    {
        IEnumerable<Monkey> monkeys;
        Barrel barrel;
        string url;
        string json;
        [TestInitialize]
        public void Setup()
        {
            Barrel.UniqueId = "com.refractored.monkeycache";
            url = "http://montemagno.com/monkeys.json";
            barrel = Barrel.Current;

            json = @"[{""Name"":""Baboon"",""Location"":""Africa & Asia"",""Details"":""Baboons are African and Arabian Old World monkeys belonging to the genus Papio, part of the subfamily Cercopithecinae."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/9\/96\/Portrait_Of_A_Baboon.jpg\/314px-Portrait_Of_A_Baboon.jpg"",""Population"":10000},{""Name"":""Capuchin Monkey"",""Location"":""Central & South America"",""Details"":""The capuchin monkeys are New World monkeys of the subfamily Cebinae. Prior to 2011, the subfamily contained only a single genus, Cebus."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/4\/40\/Capuchin_Costa_Rica.jpg\/200px-Capuchin_Costa_Rica.jpg"",""Population"":23000},{""Name"":""Blue Monkey"",""Location"":""Central and East Africa"",""Details"":""The blue monkey or diademed monkey is a species of Old World monkey native to Central and East Africa, ranging from the upper Congo River basin east to the East African Rift and south to northern Angola and Zambia"",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/8\/83\/BlueMonkey.jpg\/220px-BlueMonkey.jpg"",""Population"":12000},{""Name"":""Squirrel Monkey"",""Location"":""Central & South America"",""Details"":""The squirrel monkeys are the New World monkeys of the genus Saimiri. They are the only genus in the subfamily Saimirinae. The name of the genus Saimiri is of Tupi origin, and was also used as an English name by early researchers."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/2\/20\/Saimiri_sciureus-1_Luc_Viatour.jpg\/220px-Saimiri_sciureus-1_Luc_Viatour.jpg"",""Population"":11000},{""Name"":""Golden Lion Tamarin"",""Location"":""Brazil"",""Details"":""The golden lion tamarin also known as the golden marmoset, is a small New World monkey of the family Callitrichidae."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/8\/87\/Golden_lion_tamarin_portrait3.jpg\/220px-Golden_lion_tamarin_portrait3.jpg"",""Population"":19000},{""Name"":""Howler Monkey"",""Location"":""South America"",""Details"":""Howler monkeys are among the largest of the New World monkeys. Fifteen species are currently recognised. Previously classified in the family Cebidae, they are now placed in the family Atelidae."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/0\/0d\/Alouatta_guariba.jpg\/200px-Alouatta_guariba.jpg"",""Population"":8000},{""Name"":""Japanese Macaque"",""Location"":""Japan"",""Details"":""The Japanese macaque, is a terrestrial Old World monkey species native to Japan. They are also sometimes known as the snow monkey because they live in areas where snow covers the ground for months each"",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/c\/c1\/Macaca_fuscata_fuscata1.jpg\/220px-Macaca_fuscata_fuscata1.jpg"",""Population"":1000},{""Name"":""Mandrill"",""Location"":""Southern Cameroon, Gabon, Equatorial Guinea, and Congo"",""Details"":""The mandrill is a primate of the Old World monkey family, closely related to the baboons and even more closely to the drill. It is found in southern Cameroon, Gabon, Equatorial Guinea, and Congo."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/7\/75\/Mandrill_at_san_francisco_zoo.jpg\/220px-Mandrill_at_san_francisco_zoo.jpg"",""Population"":17000},{""Name"":""Proboscis Monkey"",""Location"":""Borneo"",""Details"":""The proboscis monkey or long-nosed monkey, known as the bekantan in Malay, is a reddish-brown arboreal Old World monkey that is endemic to the south-east Asian island of Borneo."",""Image"":""http:\/\/upload.wikimedia.org\/wikipedia\/commons\/thumb\/e\/e5\/Proboscis_Monkey_in_Borneo.jpg\/250px-Proboscis_Monkey_in_Borneo.jpg"",""Population"":15000},{""Name"":""Sebastian"",""Location"":""Seattle"",""Details"":""This little trouble maker lives in Seattle with James and loves traveling on adventures with James and tweeting @MotzMonkeys. He by far is an Android fanboy and is getting ready for the new Nexus 6P!"",""Image"":""http:\/\/www.refractored.com\/images\/sebastian.jpg"",""Population"":1},{""Name"":""Henry"",""Location"":""Phoenix"",""Details"":""An adorable Monkey who is traveling the world with Heather and live tweets his adventures @MotzMonkeys. His favorite platform is iOS by far and is excited for the new iPhone 6s!"",""Image"":""http:\/\/www.refractored.com\/images\/henry.jpg"",""Population"":1}]";
            monkeys = JsonConvert.DeserializeObject<IEnumerable<Monkey>>(json);
        }

        [TestMethod]
        public async Task GetCachedTest()
        {
            var result = await HttpCache.Current.GetCachedAsync(url, TimeSpan.FromSeconds(60), TimeSpan.FromDays(1));

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetCachedForceTest()
        {
            var result = await HttpCache.Current.GetCachedAsync(url, TimeSpan.FromSeconds(60), TimeSpan.FromDays(1), true);

            Assert.IsNotNull(result);
        }


    }
}
