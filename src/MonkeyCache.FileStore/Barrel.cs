using System;

namespace MonkeyCache.FileStore
{
    public class Barrel : IBarrel
    {
		public static string ApplicationId { get; set; } = string.Empty;

		static Barrel instance = null;

		/// <summary>
		/// Gets the instance of the Barrel
		/// </summary>
		public static IBarrel Current => (instance ?? (instance = new Barrel()));

		public void Add(string key, string data, TimeSpan expireIn, string eTag = null)
        {
            throw new NotImplementedException();
        }

        public void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null)
        {
            throw new NotImplementedException();
        }

        public void Empty(params string[] key)
        {
            throw new NotImplementedException();
        }

        public void EmptyAll()
        {
            throw new NotImplementedException();
        }

        public void EmptyExpired()
        {
            throw new NotImplementedException();
        }

        public bool Exists(string key)
        {
            throw new NotImplementedException();
        }

        public string Get(string key)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string key)
        {
            throw new NotImplementedException();
        }

        public string GetETag(string key)
        {
            throw new NotImplementedException();
        }

        public bool IsExpired(string key)
        {
            throw new NotImplementedException();
        }
    }
}
