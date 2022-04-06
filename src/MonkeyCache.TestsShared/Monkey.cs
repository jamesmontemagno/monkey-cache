// To parse this JSON data:
//
//    using MonkeyCache.Tests;
//
//    var data = Monkey.FromJson(jsonString);
//
namespace MonkeyCache.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    public partial class Monkey
    {
        public string Details { get; set; }

        public string Image { get; set; }

        public string Location { get; set; }

        public string Name { get; set; }

        public long Population { get; set; }
    }

    public partial class Monkey
    {
        public static Monkey[] FromJson(string json) => JsonSerializer.Deserialize<Monkey[]>(json);
    }

    public static class Serialize
    {
        public static string ToJson(this Monkey[] self) => JsonSerializer.Serialize(self);
    }
}
