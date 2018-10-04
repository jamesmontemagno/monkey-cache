using Newtonsoft.Json;
using System;

namespace MonkeyCache
{
	public interface IBarrel
	{
		void Add(string key, string data, TimeSpan expireIn, string eTag = null);
		void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null, JsonSerializerSettings jsonSerializationSettings = null);
		void Empty(params string[] key);
		void EmptyAll();
		void EmptyExpired();
		bool Exists(string key);
		string Get(string key);
		T Get<T>(string key, JsonSerializerSettings jsonSettings = null);
		string GetETag(string key);
		bool IsExpired(string key);

		DateTime? GetExpiration(string key);
	}
}