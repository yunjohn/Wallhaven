using System.Net;
using System.Security.Authentication;

namespace Wallhaven.Core;

public static class NetworkClient
{
    public static HttpClient Create()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(15),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AllowAutoRedirect = false
        };
        // TLS 1.2 is supported on both Windows 10 and 11; keep certificate validation enabled.
        handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls12;
        return new HttpClient(new ReadRetryHandler(handler)) { Timeout = TimeSpan.FromSeconds(60) };
    }
    private sealed class ReadRetryHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            for (int attempt = 0; ; attempt++)
            {
                // Every request made by this application is an idempotent GET.
                using var copy = new HttpRequestMessage(request.Method, request.RequestUri);
                foreach (var header in request.Headers) copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
                try { return await base.SendAsync(copy, ct); }
                catch (HttpRequestException) when (request.Method == HttpMethod.Get && attempt < 2)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * (attempt + 1)), ct);
                }
            }
        }
    }
}
