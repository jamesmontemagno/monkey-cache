// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using MonkeyCache.Tests;
//
//    var data = Monkey.FromJson(jsonString);
//
namespace MonkeyCache.Tests
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public partial class Monkey
    {
        [JsonProperty("Details")]
        public string Details { get; set; }

        [JsonProperty("Image")]
        public string Image { get; set; }

        [JsonProperty("Location")]
        public string Location { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Population")]
        public long Population { get; set; }
    }

    public partial class Monkey
    {
        public static Monkey[] FromJson(string json) => JsonConvert.DeserializeObject<Monkey[]>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Monkey[] self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };
    }
}
