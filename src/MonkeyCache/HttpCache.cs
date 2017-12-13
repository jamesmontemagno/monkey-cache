using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace MonkeyCache
{
    public class HttpCache
    {

        static Lazy<HttpCache> instance = new Lazy<HttpCache>(() => new HttpCache());

        /// <summary>
        /// Gets the instance of the HttpCache
        /// </summary>
        public static HttpCache Current => instance.Value;

        HttpCache()
        {
        }

        internal HttpClient CreateClient(TimeSpan timeout)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                MaxAutomaticRedirections = 20,

            };

            var monkeyHandler = new MonkeyHandler(handler);

            var cli = new HttpClient(monkeyHandler);
            cli.Timeout = timeout;
            return cli;
        }

        public async Task<string> GetCachedAsync(string url, TimeSpan timeout, TimeSpan expireIn, bool forceUpdate = false)
        {
            var client = CreateClient(timeout);

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
            };

            request.Properties.Add(MonkeyHandler.RequestProperties.ForceUpdate, forceUpdate);
            request.Properties.Add(MonkeyHandler.RequestProperties.ExpireIn, expireIn);

            var result = await client.SendAsync(request);
            return await result.Content.ReadAsStringAsync();
        }
    }
}
