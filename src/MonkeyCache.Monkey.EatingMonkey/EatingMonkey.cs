using System;
using System.Linq;

namespace MonkeyCache.Monkey.EatingMonkey
{
    public class EatingMonkey : IMonkey
    {
        public void Monkey(IBananaCache cache)
        {
            Random randomBanana = new Random(111_493);
            var keys = cache.Keys.ToArray();
            if(keys.Any())
            {
                var bananaToPick = randomBanana.Next(0, keys.Length - 1);
                var bananaKey = keys[bananaToPick];
                cache.Set<object>(bananaKey, null);
            }
        }
    }
}
