using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MonkeyCache.Tests
{
    [TestClass]
    public partial class HttpCacheTests
    {
        IBarrel barrel;

        [TestInitialize]
        public void Setup() => SetupBarrel();

        [TestMethod]
        public Task GetCachedTest() => CheckResponse(false);

        [TestMethod]
        public Task GetCachedForceTest() => CheckResponse(true);

        async Task CheckResponse(bool force)
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/monkeys";
            const string body = "[{\"Name\":\"Baboon\"}]";
            var server = Serve();
            try
            {
                if (force)
                    barrel.Add(url, "old", TimeSpan.FromDays(1));
                var result = await HttpCache.Current.GetCachedAsync(barrel, url, TimeSpan.FromSeconds(10), TimeSpan.FromDays(1), force);
                Assert.AreEqual(body, result);
                await server.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.AreEqual(body, barrel.Get<string>(url));
                Assert.AreEqual("\"test\"", barrel.GetETag(url));
                listener.Stop();
                Assert.AreEqual(body, await HttpCache.Current.GetCachedAsync(barrel, url, TimeSpan.FromSeconds(10), TimeSpan.FromDays(1)));
            }
            finally
            {
                listener.Stop();
                barrel.Empty(url);
            }

            async Task Serve()
            {
                using var client = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(10));
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)))) { }
                var response = Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\nETag: \"test\"\r\nConnection: close\r\n\r\n{body}");
                await stream.WriteAsync(response);
            }
        }
    }
}
