using Microsoft.AspNetCore.HttpOverrides;

namespace MiCash.API.Extensions;

/// <summary>
/// Behind Traefik the TCP peer is the proxy, so without this every client would share its IP
/// (per-IP rate limits, logs). Reads X-Forwarded-For/Proto only when the request comes from a private
/// network (the Docker networks Traefik runs on). In production the API must be reachable only through
/// Traefik (no published port); otherwise a client on a private network could send a fake X-Forwarded-For.
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
        });
    }
}
