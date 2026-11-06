using Microsoft.AspNetCore.HttpOverrides;

namespace MiCash.API.Extensions;

/// <summary>
/// Behind a proxy the TCP peer is the proxy, so without this every client would share its IP
/// (per-IP rate limits, logs). Reads X-Forwarded-For/Proto only when the request comes from a private
/// network or loopback. Two trusted senders:
/// - Traefik (mobile app and any direct call): it overwrites X-Forwarded-For with the real client.
/// - micash-web's BFF: it calls the API server to server and sends the visitor's IP (from Traefik's
///   X-Real-Ip). It must call the API on the Docker network (http://api:8080), not through Traefik,
///   or the web server's own IP is what reaches here. Loopback covers local dev (web on the host).
/// In production the API must be reachable only through Traefik and the internal network (no published
/// port); otherwise a client on a private network could send a fake X-Forwarded-For.
/// </summary>
public static class ForwardedHeadersExtension
{
    public static IServiceCollection AddProxyForwardedHeaders ( this IServiceCollection services )
    {
        return services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("127.0.0.0/8"));
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("::1/128"));
        });
    }
}
