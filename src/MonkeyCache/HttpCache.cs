using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SQLite;

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

        internal HttpClient CreateClient(TimeSpan timeout)
        {
            var h = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                MaxAutomaticRedirections = 20,

            };
            var cli = new HttpClient(h);
            cli.Timeout = timeout;
            return cli;
        }

        static System.Threading.SemaphoreSlim getThrottle = new System.Threading.SemaphoreSlim(4, 4);

        public async Task<string> GetCachedAsync(string url, TimeSpan timeout, TimeSpan expireIn, bool forceUpdate = false)
        {
            var contents = Barrel.Current.Get(url);
            var eTag = Barrel.Current.GetETag(url);

            if (!forceUpdate && !string.IsNullOrEmpty(contents) && !Barrel.Current.IsExpired(url))
                return contents;

            var etag = eTag ?? null;

            await getThrottle.WaitAsync();

            HttpResponseMessage r;
            string c;

            try
            {

                //Console.WriteLine("GetCachedAsync " + url + " etag: " + etag);

                var client = CreateClient(timeout);
                if (!forceUpdate && !string.IsNullOrEmpty(etag) && !string.IsNullOrEmpty(contents))
                {
                    client.DefaultRequestHeaders.IfNoneMatch.Clear();
                    client.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
                }

                r = await client.GetAsync(url);

                //Console.WriteLine("GotCachedAsync " + r.StatusCode);

                if (r.StatusCode == HttpStatusCode.NotModified)
                {
                    if (string.IsNullOrEmpty(contents))
                        throw new Exception("Cached value missing");
                    
                    return contents;
                }

                if (r.StatusCode == HttpStatusCode.OK)
                {

                    c = await r.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(c) && c[0] == 0xFEFF)
                    {
                        c = c.Substring(1);
                    }
                }
                else
                {
                    c = "";
                }
            }
            finally
            {
                getThrottle.Release();
            }

            if (r.StatusCode == HttpStatusCode.OK)
            {
                //
                // Cache it?
                //
                var newEtag = r.Headers.ETag != null ? r.Headers.ETag.Tag : null;
                if (!string.IsNullOrEmpty(newEtag) && newEtag != etag)
                {
                    //Console.WriteLine("CACHING " + url + " etag: " + etag);
                    Barrel.Current.Add(url, c, expireIn, newEtag);
                }

                return c;
            }
            else
            {
                throw new Exception("Bad web response: " + r.StatusCode);
            }
        }
    }
}
