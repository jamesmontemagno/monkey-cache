using System;
using System.Collections.Generic;

namespace MonkeyCache.InMemory
{
    public class BananaCache : IBananaCache
    {
        private readonly IDictionary<string, object> _bananaCache = new Dictionary<string, object>();

        public IEnumerable<string> Keys => _bananaCache.Keys;

        public T Get<T>(string key)
        {
            if (_bananaCache.TryGetValue(key, out object data))
                return (T)data;

            throw new ArgumentOutOfRangeException(nameof(key));
        }

        public void Set<T>(string key, T data)
        {
            if (!_bananaCache.ContainsKey(key))
                _bananaCache.Add(key, (object)null);

            _bananaCache[key] = data;
        }
    }
}
