using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MonkeyCache
{
	/// <summary>
	/// Http cache utilities!
	/// </summary>
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

		internal HttpClient CreateClient(TimeSpan timeout)
		{
			var h = new HttpClientHandler {
				AllowAutoRedirect = true,
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				MaxAutomaticRedirections = 20,

			};

			var client = new HttpClient(h)
			{
				Timeout = timeout
			};

			return client;
		}

		/// <summary>
		/// Get a cahced item via web request.
		/// </summary>
		/// <param name="barrel">Barrel to use for cache</param>
		/// <param name="url">Url to query</param>
		/// <param name="timeout">Timeout to use</param>
		/// <param name="expireIn">Set the item to expire in</param>
		/// <param name="forceUpdate">Force an update from the server</param>
		/// <param name="throttled">If the request should be throttled</param>
		/// <returns>The cached or new active item.</returns>
		public Task<string> GetCachedAsync(IBarrel barrel, string url, TimeSpan timeout, TimeSpan expireIn, bool forceUpdate = false, bool throttled = true)
		{
			var client = CreateClient(timeout);

			return client.SendCachedAsync(barrel, new HttpRequestMessage(HttpMethod.Get, url), expireIn, forceUpdate, throttled);
		}
	}

	/// <summary>
	/// Http cache extension helpers
	/// </summary>
	public static class HttpCacheExtensions
	{
		static System.Threading.SemaphoreSlim getThrottle = new System.Threading.SemaphoreSlim(4, 4);

		/// <summary>
		/// Send a cached requests
		/// </summary>
		/// <param name="http">Http client ot use</param>
		/// <param name="barrel">Barrel to use for cache</param>
		/// <param name="req">request to send</param>
		/// <param name="expireIn">expire in</param>
		/// <param name="forceUpdate">If we should force the update or not</param>
		/// <param name="throttled">If throttled or not</param>
		/// <returns>The new or cached response.</returns>
		public static async Task<string> SendCachedAsync(this HttpClient http, IBarrel barrel, HttpRequestMessage req, TimeSpan expireIn, bool forceUpdate = false, bool throttled = true)
		{
			var url = req.RequestUri.ToString();

			var contents = barrel.Get<string>(url);
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

				r = await http.SendAsync(req);

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

	/// <summary>
	/// Http request exception
	/// </summary>
	public class HttpCacheRequestException : HttpRequestException
	{
		/// <summary>
		/// Constructor for cache exception
		/// </summary>
		/// <param name="statusCode">The code</param>
		/// <param name="message">Message</param>
		public HttpCacheRequestException (HttpStatusCode statusCode, string message) 
			: base (message)
		{
			StatusCode = statusCode;
		}

		/// <summary>
		/// Status code
		/// </summary>
		public HttpStatusCode StatusCode { get; private set; }
	}
}
