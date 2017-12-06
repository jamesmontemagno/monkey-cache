using System.Collections.Generic;

namespace MonkeyCache
{
    public class MonkeyCache
    {
        private readonly List<IMonkey> _availableMonkeys = new List<IMonkey>();
        public IBananaCache BananaCache { get; }

        public MonkeyCache(IBananaCache bananaCache)
        {
            BananaCache = bananaCache;
        }

        public void AddMonkey(IMonkey monkey)
        {
            _availableMonkeys.Add(monkey);
        }

        public void MonkeyAround()
        {
            foreach (var aMonkey in _availableMonkeys)
                aMonkey.Monkey(BananaCache);
        }
    }
}
