using System.Collections.Generic;

namespace MonkeyCache
{
    public interface IBananaCache
    {
        T Get<T>(string key);
        void Set<T>(string key, T banana);
        IEnumerable<string> Keys { get; }
    }
}
