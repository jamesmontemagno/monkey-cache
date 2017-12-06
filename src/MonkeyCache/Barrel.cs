using System;
using System.Threading.Tasks;

namespace MonkeyCache
{
    /// <summary>
    /// A Barrel to throw all your bannanas in.
    /// </summary>
    public class Barrel
    {
        public async Task<T> GetAsync<T>(string key)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> AddAsync<T>(string key, T data)
        {
            throw new NotImplementedException(); 
        }

        public bool IsExpired(string key)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> EmptyAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> EmptyAsync(params string[] key)
        {
            throw new NotImplementedException();
        }
    }
}
