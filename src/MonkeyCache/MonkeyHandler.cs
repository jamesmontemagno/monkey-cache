using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyCache
{
    public class MonkeyHandler : DelegatingHandler
    {
        public MonkeyHandler(HttpMessageHandler handler) : base(handler) 
        {

        }

        public static class RequestProperties
        {
            public const string ForceUpdate = "forceUpdate";

            public const string ExpireIn = "expireIn";
        }

        static SemaphoreSlim getThrottle = new SemaphoreSlim(4, 4);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri.ToString();
            var forceUpdate = (request.Properties[RequestProperties.ForceUpdate] as bool?) ?? false;
            var expireIn = (request.Properties[RequestProperties.ExpireIn] as TimeSpan?) ?? TimeSpan.MaxValue;

            var contents = Barrel.Current.Get(url);
            var eTag = Barrel.Current.GetETag(url);

            if (!forceUpdate && !string.IsNullOrEmpty(contents) && !Barrel.Current.IsExpired(url))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(contents),
                };
            }
                

            var etag = eTag ?? null;

            await getThrottle.WaitAsync();

            HttpResponseMessage r;
            string c;

            try
            {
                //Console.WriteLine("GetCachedAsync " + url + " etag: " + etag);

                if (!forceUpdate && !string.IsNullOrEmpty(etag) && !string.IsNullOrEmpty(contents))
                {
                    request.Headers.IfNoneMatch.Clear();
                    request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
                }

                r = await base.SendAsync(request, cancellationToken);

                //Console.WriteLine("GotCachedAsync " + r.StatusCode);

                if (r.StatusCode == HttpStatusCode.NotModified)
                {
                    if (string.IsNullOrEmpty(contents))
                        throw new Exception("Cached value missing");

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(contents),
                    };
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

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(c),
                };
            }
            else
            {
                throw new Exception("Bad web response: " + r.StatusCode);
            }
        }
    }
}
