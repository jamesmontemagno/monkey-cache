using System;
using System.Threading.Tasks;

namespace MonkeyCache
{
    /// <summary>
    /// A Barrel to throw all your bannanas in.
    /// </summary>
    public class Barrel
    {
        public T Get<T>(string key)
        {
            throw new NotImplementedException();
        }

        public T Add<T>(string key, T data)
        {
            throw new NotImplementedException(); 
        }

        public bool IsExpired(string key)
        {
            throw new NotImplementedException();
        }

        public bool Empty()
        {
            throw new NotImplementedException();
        }

        public bool Empty(params string[] key)
        {
            throw new NotImplementedException();
        }
    }
}
