using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MonkeyCache
{
	public class HttpCache
	{

		static HttpCache instance = null;

		/// <summary>
		/// Gets the instance of the HttpCache
		/// </summary>
		public static HttpCache Current => (instance ?? (instance = new HttpCache()));

		HttpCache()
		{
		}


		HttpClient client;

		internal HttpClient CreateClient(TimeSpan timeout)
		{

			if (client != null)
				return client;

			var h = new HttpClientHandler {
				AllowAutoRedirect = true,
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				MaxAutomaticRedirections = 20,

			};
			var cli = new HttpClient(h);
			cli.Timeout = timeout;
			client = cli;

			return client;
		}

		public Task<string> GetCachedAsync(IBarrel barrel, string url, TimeSpan timeout, TimeSpan expireIn, bool forceUpdate = false, bool throttled = true)
		{
			var client = CreateClient(timeout);

			return client.SendCachedAsync(barrel, new HttpRequestMessage(HttpMethod.Get, url), expireIn, forceUpdate, throttled);
		}
	}

	public static class HttpCacheExtensions
	{
		static System.Threading.SemaphoreSlim getThrottle = new System.Threading.SemaphoreSlim(4, 4);

		public static async Task<string> SendCachedAsync(this HttpClient http, IBarrel barrel, HttpRequestMessage req, TimeSpan expireIn, bool forceUpdate = false, bool throttled = true)
		{
			var url = req.RequestUri.ToString();

			var contents = barrel.Get(url);
			var eTag = barrel.GetETag(url);

			if (!forceUpdate && !string.IsNullOrEmpty(contents) && !barrel.IsExpired(url))
				return contents;

			var etag = eTag ?? null;

			if (throttled)
				await getThrottle.WaitAsync();

			HttpResponseMessage r;
			string c = null;

			try {
				if (!forceUpdate && !string.IsNullOrEmpty(etag) && !string.IsNullOrEmpty(contents)) {
					req.Headers.IfNoneMatch.Clear();
					req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
				}

				r = await http.SendAsync(req).ConfigureAwait(false);

				if (r.StatusCode == HttpStatusCode.NotModified) {
					if (string.IsNullOrEmpty(contents))
						throw new IndexOutOfRangeException($"Cached value missing for HTTP request: {url}");

					return contents;
				}

				c = await r.Content.ReadAsStringAsync();
			} finally {
				if (throttled)
					getThrottle.Release();
			}

			if (r.StatusCode == HttpStatusCode.OK) {
				// Cache it?
				var newEtag = r.Headers.ETag != null ? r.Headers.ETag.Tag : null;
				if (!string.IsNullOrEmpty(newEtag) && newEtag != etag)
					barrel.Add(url, c, expireIn, newEtag);

				return c;
			} else {
				throw new HttpCacheRequestException(r.StatusCode, "HTTP Cache Request Failed");
			}
		}
	}

	public class HttpCacheRequestException : HttpRequestException
	{
		public HttpCacheRequestException (HttpStatusCode statusCode, string message) 
			: base (message)
		{
			StatusCode = statusCode;
		}

		public HttpStatusCode StatusCode { get; private set; }
	}
}
